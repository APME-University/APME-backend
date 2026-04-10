using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using APME.BlobStorage;
using APME.Products;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;

namespace APME.AI;

/// <summary>
/// Implementation of AI chat service using RAG (Retrieval-Augmented Generation).
/// Uses semantic search to find relevant products and ILlmProvider for response generation.
/// SRS Reference: AI Chatbot RAG Architecture - Chat Service
/// </summary>
public class AIChatService : IAIChatService, ITransientDependency
{
    private readonly ISemanticSearchService _semanticSearchService;
    private readonly ILlmProvider _llmProvider;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IImageUrlProvider _imageUrlProvider;
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
        ILlmProvider llmProvider,
        IRepository<Product, Guid> productRepository,
        IImageUrlProvider imageUrlProvider,
        IOptions<AIOptions> options,
        IDataFilter dataFilter,
        ILogger<AIChatService> logger)
    {
        _semanticSearchService = semanticSearchService;
        _llmProvider = llmProvider;
        _productRepository = productRepository;
        _imageUrlProvider = imageUrlProvider;
        _options = options.Value;
        _dataFilter = dataFilter;
        _logger = logger;
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
            var contextProducts = await _semanticSearchService.SearchAsync(
                request.Message,
                request.ContextProductCount,
                request.TenantId,
                request.ShopId,
                cancellationToken);

            _logger.LogDebug(
                "Found {Count} relevant products for context",
                contextProducts.Count);

            var enrichedProducts = await EnrichProductDataAsync(contextProducts, cancellationToken);
            var prompt = BuildPrompt(request, enrichedProducts);
            var generationRequest = BuildGenerationRequest(prompt, request.ConversationHistory);

            var generationResponse = await _llmProvider.GenerateResponseAsync(
                generationRequest,
                cancellationToken);

            stopwatch.Stop();

            var chatResponse = new ChatResponse
            {
                Response = generationResponse.Content,
                ContextProducts = enrichedProducts,
                GenerationTimeMs = stopwatch.ElapsedMilliseconds,
                SessionId = request.SessionId ?? Guid.NewGuid().ToString(),
                TokenUsage = generationResponse.TokenUsage != null 
                    ? new TokenUsage
                    {
                        PromptTokens = generationResponse.TokenUsage.PromptTokens,
                        CompletionTokens = generationResponse.TokenUsage.CompletionTokens
                    }
                    : null
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
        List<ProductSearchResult>? preSearchedProducts = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing streaming chat request: '{Message}'",
            TruncateForLog(request.Message));

        List<ProductSearchResult> contextProducts;
        
        // Use pre-searched products if provided, otherwise search again
        if (preSearchedProducts != null && preSearchedProducts.Count > 0)
        {
            _logger.LogDebug("Using {Count} pre-searched products for context", preSearchedProducts.Count);
            contextProducts = preSearchedProducts;
        }
        else
        {
            contextProducts = await _semanticSearchService.SearchAsync(
                request.Message,
                request.ContextProductCount,
                request.TenantId,
                request.ShopId,
                cancellationToken);
        }

        // Note: Products should already be enriched if passed from orchestrator
        // But we enrich again here to ensure they have all required fields
        var enrichedProducts = await EnrichProductDataAsync(contextProducts, cancellationToken);
        var prompt = BuildPrompt(request, enrichedProducts);
        var generationRequest = BuildGenerationRequest(prompt, request.ConversationHistory);

        await foreach (var token in _llmProvider.GenerateStreamingResponseAsync(
            generationRequest,
            cancellationToken))
        {
            yield return token;
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

        using (_dataFilter.Disable<IMultiTenant>())
        {
            var products = await _productRepository.GetListAsync(
                p => productIds.Contains(p.Id),
                cancellationToken: cancellationToken);

            var productLookup = products.ToDictionary(p => p.Id);

            foreach (var result in searchResults)
            {
                if (productLookup.TryGetValue(result.ProductId, out var product))
                {
                    result.ProductName = product.Name;
                    result.Price = product.Price;
                    result.IsInStock = product.IsInStock();
                    result.IsOnSale = product.IsOnSale();
                    result.SKU = product.SKU;
                    result.Slug = product.Slug;
                    result.ImageUrl = _imageUrlProvider.GetFullImageUrl(product.PrimaryImageUrl);
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
    /// Builds a GenerationRequest from conversation history and prompt.
    /// </summary>
    private GenerationRequest BuildGenerationRequest(
        string currentPrompt,
        List<ChatMessage>? conversationHistory)
    {
        var messages = new List<GenerationMessage>();

        if (conversationHistory != null)
        {
            foreach (var msg in conversationHistory.TakeLast(10))
            {
                messages.Add(new GenerationMessage
                {
                    Role = msg.Role,
                    Content = msg.Content,
                    Timestamp = msg.Timestamp
                });
            }
        }

        messages.Add(new GenerationMessage
        {
            Role = "user",
            Content = currentPrompt,
            Timestamp = DateTime.UtcNow
        });

        return new GenerationRequest
        {
            Messages = messages,
            Temperature = _options.GenerationTemperature,
            MaxTokens = _options.MaxGenerationTokens,
            Stream = true
        };
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
