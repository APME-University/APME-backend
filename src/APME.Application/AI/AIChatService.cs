using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using APME.Products;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OllamaSharp;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;

namespace APME.AI;

/// <summary>
/// Implementation of AI chat service using RAG (Retrieval-Augmented Generation).
/// Uses semantic search to find relevant products and Ollama for response generation.
/// SRS Reference: AI Chatbot RAG Architecture - Chat Service
/// </summary>
public class AIChatService : IAIChatService, ITransientDependency
{
    private readonly ISemanticSearchService _semanticSearchService;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly OllamaApiClient _ollamaClient;
    private readonly AIOptions _options;
    private readonly IDataFilter _dataFilter;
    private readonly ILogger<AIChatService> _logger;

    private const string SystemPrompt = @"You are a helpful e-commerce assistant for an online shopping platform. 
Your role is to help customers find products, answer questions about products, and provide shopping recommendations.

When answering questions:
1. Use the provided product context to give accurate, specific information
2. If asked about products not in the context, politely say you don't have information about those specific products
3. Be helpful, friendly, and concise
4. If you recommend products, explain why they might be good choices
5. Always provide accurate prices and availability information from the context
6. Do not make up product information that is not provided in the context

Current product context is provided below. Use this information to answer customer questions.";

    public AIChatService(
        ISemanticSearchService semanticSearchService,
        IRepository<Product, Guid> productRepository,
        IOptions<AIOptions> options,
        IDataFilter dataFilter,
        ILogger<AIChatService> logger)
    {
        _semanticSearchService = semanticSearchService;
        _productRepository = productRepository;
        _options = options.Value;
        _dataFilter = dataFilter;
        _logger = logger;

        // Initialize Ollama client for generation
        var uri = new Uri(_options.OllamaBaseUrl);
        _ollamaClient = new OllamaApiClient(uri);
        _ollamaClient.SelectedModel = _options.GenerationModel;
    }

    /// <inheritdoc />
    public async Task<ChatResponse> ChatAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Processing chat request: '{Message}' (session={SessionId})",
            TruncateForLog(request.Message), request.SessionId);

        try
        {
            // Step 1: Retrieve relevant products using semantic search
            var contextProducts = await _semanticSearchService.SearchAsync(
                request.Message,
                request.ContextProductCount,
                request.TenantId,
                request.ShopId,
                cancellationToken);

            _logger.LogDebug(
                "Found {Count} relevant products for context",
                contextProducts.Count);

            // Step 2: Fetch authoritative product data from relational DB
            var enrichedProducts = await EnrichProductDataAsync(contextProducts, cancellationToken);

            // Step 3: Build prompt with context
            var prompt = BuildPrompt(request, enrichedProducts);

            // Step 4: Generate response using Ollama
            var response = await GenerateResponseAsync(prompt, request.ConversationHistory, cancellationToken);

            stopwatch.Stop();

            var chatResponse = new ChatResponse
            {
                Response = response,
                ContextProducts = enrichedProducts,
                GenerationTimeMs = stopwatch.ElapsedMilliseconds,
                SessionId = request.SessionId ?? Guid.NewGuid().ToString()
            };

            _logger.LogInformation(
                "Chat response generated in {TimeMs}ms, {ProductCount} products in context",
                stopwatch.ElapsedMilliseconds, enrichedProducts.Count);

            return chatResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat request failed for message: {Message}", TruncateForLog(request.Message));
            throw;
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> ChatStreamAsync(
        ChatRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing streaming chat request: '{Message}'",
            TruncateForLog(request.Message));

        // Step 1: Retrieve relevant products
        var contextProducts = await _semanticSearchService.SearchAsync(
            request.Message,
            request.ContextProductCount,
            request.TenantId,
            request.ShopId,
            cancellationToken);

        // Step 2: Enrich with authoritative data
        var enrichedProducts = await EnrichProductDataAsync(contextProducts, cancellationToken);

        // Step 3: Build prompt
        var prompt = BuildPrompt(request, enrichedProducts);

        // Step 4: Build messages for Ollama
        var messages = BuildChatMessages(prompt, request.ConversationHistory);

        // Step 5: Stream response
        await foreach (var token in _ollamaClient.ChatAsync(
            new OllamaSharp.Models.Chat.ChatRequest
            {
                Model = _options.GenerationModel,
                Messages = messages,
                Options = new OllamaSharp.Models.RequestOptions
                {
                    Temperature = _options.GenerationTemperature,
                    NumPredict = _options.MaxGenerationTokens
                },
                Stream = true
            }, cancellationToken))
        {
            if (!string.IsNullOrEmpty(token?.Message?.Content))
            {
                yield return token.Message.Content;
            }
        }
    }

    /// <summary>
    /// Enriches search results with authoritative product data from the relational database.
    /// </summary>
    private async Task<List<ProductSearchResult>> EnrichProductDataAsync(
        List<ProductSearchResult> searchResults,
        CancellationToken cancellationToken)
    {
        if (searchResults.Count == 0)
        {
            return searchResults;
        }

        var productIds = searchResults.Select(r => r.ProductId).ToList();

        // Fetch products from relational DB (disable tenant filter for cross-tenant access)
        using (_dataFilter.Disable<IMultiTenant>())
        {
            var products = await _productRepository.GetListAsync(
                p => productIds.Contains(p.Id),
                cancellationToken: cancellationToken);

            var productLookup = products.ToDictionary(p => p.Id);

            // Enrich search results with latest product data
            foreach (var result in searchResults)
            {
                if (productLookup.TryGetValue(result.ProductId, out var product))
                {
                    result.ProductName = product.Name;
                    result.Price = product.Price;
                    result.IsInStock = product.IsInStock();
                    result.IsOnSale = product.IsOnSale();
                    result.SKU = product.SKU;
                }
            }
        }

        return searchResults;
    }

    /// <summary>
    /// Builds the prompt with product context.
    /// </summary>
    private string BuildPrompt(ChatRequest request, List<ProductSearchResult> contextProducts)
    {
        var sb = new StringBuilder();
        sb.AppendLine(SystemPrompt);
        sb.AppendLine();

        if (contextProducts.Count > 0)
        {
            sb.AppendLine("=== PRODUCT CONTEXT ===");
            sb.AppendLine();

            foreach (var product in contextProducts)
            {
                sb.AppendLine($"Product: {product.ProductName}");
                sb.AppendLine($"- Price: ${product.Price:F2}");
                sb.AppendLine($"- In Stock: {(product.IsInStock ? "Yes" : "No")}");
                sb.AppendLine($"- On Sale: {(product.IsOnSale ? "Yes" : "No")}");
                
                if (!string.IsNullOrWhiteSpace(product.CategoryName))
                {
                    sb.AppendLine($"- Category: {product.CategoryName}");
                }
                
                if (!string.IsNullOrWhiteSpace(product.ShopName))
                {
                    sb.AppendLine($"- Shop: {product.ShopName}");
                }
                
                if (!string.IsNullOrWhiteSpace(product.MatchedSnippet))
                {
                    sb.AppendLine($"- Details: {product.MatchedSnippet}");
                }
                
                sb.AppendLine();
            }

            sb.AppendLine("=== END PRODUCT CONTEXT ===");
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("No specific products found matching the query. Provide general assistance.");
            sb.AppendLine();
        }

        sb.AppendLine($"Customer Question: {request.Message}");

        return sb.ToString();
    }

    /// <summary>
    /// Generates a response using Ollama.
    /// </summary>
    private async Task<string> GenerateResponseAsync(
        string prompt,
        List<ChatMessage>? conversationHistory,
        CancellationToken cancellationToken)
    {
        var messages = BuildChatMessages(prompt, conversationHistory);

        var response = new StringBuilder();

        await foreach (var token in _ollamaClient.ChatAsync(
            new OllamaSharp.Models.Chat.ChatRequest
            {
                Model = _options.GenerationModel,
                Messages = messages,
                Options = new OllamaSharp.Models.RequestOptions
                {
                    Temperature = _options.GenerationTemperature,
                    NumPredict = _options.MaxGenerationTokens
                },
                Stream = true
            }, cancellationToken))
        {
            if (!string.IsNullOrEmpty(token?.Message?.Content))
            {
                response.Append(token.Message.Content);
            }
        }

        return response.ToString();
    }

    /// <summary>
    /// Builds chat messages for Ollama including conversation history.
    /// </summary>
    private List<OllamaSharp.Models.Chat.Message> BuildChatMessages(
        string currentPrompt,
        List<ChatMessage>? conversationHistory)
    {
        var messages = new List<OllamaSharp.Models.Chat.Message>();

        // Add conversation history if provided
        if (conversationHistory != null)
        {
            foreach (var msg in conversationHistory.TakeLast(10)) // Limit history
            {
                messages.Add(new OllamaSharp.Models.Chat.Message
                {
                    Role = msg.Role switch
                    {
                        "user" => OllamaSharp.Models.Chat.ChatRole.User,
                        "assistant" => OllamaSharp.Models.Chat.ChatRole.Assistant,
                        "system" => OllamaSharp.Models.Chat.ChatRole.System,
                        _ => OllamaSharp.Models.Chat.ChatRole.User
                    },
                    Content = msg.Content
                });
            }
        }

        // Add current prompt as user message
        messages.Add(new OllamaSharp.Models.Chat.Message
        {
            Role = OllamaSharp.Models.Chat.ChatRole.User,
            Content = currentPrompt
        });

        return messages;
    }

    /// <summary>
    /// Truncates text for logging purposes.
    /// </summary>
    private string TruncateForLog(string text, int maxLength = 100)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
        {
            return text ?? string.Empty;
        }
        return text.Substring(0, maxLength) + "...";
    }
}








