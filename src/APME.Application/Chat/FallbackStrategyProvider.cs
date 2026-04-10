using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using APME.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace APME.Chat;

/// <summary>
/// Interface for fallback strategy provider.
/// </summary>
public interface IFallbackStrategyProvider
{
    /// <summary>
    /// Generates a response with fallback strategies.
    /// </summary>
    Task<FallbackResponse> GenerateWithFallbackAsync(
        string query,
        ChatContext context,
        QueryAnalysis analysis,
        Func<string, Task>? onToken = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Response from fallback strategy provider.
/// </summary>
public class FallbackResponse
{
    public string Response { get; set; } = string.Empty;
    public float Confidence { get; set; }
    public string StrategyUsed { get; set; } = string.Empty;
    public bool IsRuleBased { get; set; }
    public bool RequiresHumanHandoff { get; set; }
    public List<ProductSearchResult> ContextProducts { get; set; } = new();
}

/// <summary>
/// Provides multi-tier fallback strategies for chat responses.
/// Ensures graceful degradation when primary RAG/LLM fails.
/// </summary>
public class FallbackStrategyProvider : IFallbackStrategyProvider, ITransientDependency
{
    private readonly ISemanticSearchService _semanticSearchService;
    private readonly ILlmProvider _llmProvider;
    private readonly ChatOptions _chatOptions;
    private readonly AIOptions _aiOptions;
    private readonly ILogger<FallbackStrategyProvider> _logger;

    private const string SystemPrompt = @"You are a helpful e-commerce assistant for an online shopping platform.
Your role is to help customers find products, answer questions about products, and provide shopping recommendations.

When answering questions:
1. Use the provided product context to give accurate, specific information
2. If asked about products not in the context, politely say you don't have information about those specific products
3. Be helpful, friendly, and concise
4. If you recommend products, explain why they might be good choices
5. Always provide accurate prices and availability information from the context
6. Do not make up product information that is not provided in the context";

    public FallbackStrategyProvider(
        ISemanticSearchService semanticSearchService,
        ILlmProvider llmProvider,
        IOptions<ChatOptions> chatOptions,
        IOptions<AIOptions> aiOptions,
        ILogger<FallbackStrategyProvider> logger)
    {
        _semanticSearchService = semanticSearchService;
        _llmProvider = llmProvider;
        _chatOptions = chatOptions.Value;
        _aiOptions = aiOptions.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<FallbackResponse> GenerateWithFallbackAsync(
        string query,
        ChatContext context,
        QueryAnalysis analysis,
        Func<string, Task>? onToken = null,
        CancellationToken cancellationToken = default)
    {
        var ruleBasedResponse = TryRuleBasedResponse(query, analysis);
        if (ruleBasedResponse != null && ruleBasedResponse.Confidence >= 0.85f)
        {
            _logger.LogDebug("Using rule-based response for intent: {Intent}", analysis.Intent);
            
            if (onToken != null)
            {
                await onToken(ruleBasedResponse.Response);
            }
            
            return ruleBasedResponse;
        }

        if (analysis.ShouldUseRag)
        {
            var primaryResponse = await TryPrimaryRagAsync(
                query, context, analysis, onToken, cancellationToken);
            
            if (primaryResponse != null && 
                primaryResponse.Confidence >= _chatOptions.PrimaryConfidenceThreshold)
            {
                return primaryResponse;
            }

            var expandedResponse = await TryExpandedSearchAsync(
                query, context, analysis, onToken, cancellationToken);
            
            if (expandedResponse != null && 
                expandedResponse.Confidence >= _chatOptions.ExpandedConfidenceThreshold)
            {
                return expandedResponse;
            }
        }

        if (ruleBasedResponse != null)
        {
            _logger.LogDebug("Using rule-based fallback response");
            
            if (onToken != null)
            {
                await onToken(ruleBasedResponse.Response);
            }
            
            return ruleBasedResponse;
        }

        var finalResponse = GetFinalFallbackResponse(query, analysis);
        
        if (onToken != null)
        {
            await onToken(finalResponse.Response);
        }
        
        return finalResponse;
    }

    private FallbackResponse? TryRuleBasedResponse(string query, QueryAnalysis analysis)
    {
        var normalizedQuery = query.ToLowerInvariant();

        return analysis.Intent switch
        {
            Intent.Greeting => new FallbackResponse
            {
                Response = "Hello! I'm here to help you find products and answer any questions you have. What are you looking for today?",
                Confidence = 0.95f,
                StrategyUsed = "RuleBased_Greeting",
                IsRuleBased = true
            },
            
            Intent.Farewell => new FallbackResponse
            {
                Response = "Thank you for shopping with us! If you have any more questions, feel free to come back anytime. Have a great day!",
                Confidence = 0.95f,
                StrategyUsed = "RuleBased_Farewell",
                IsRuleBased = true
            },
            
            Intent.HumanHandoff => new FallbackResponse
            {
                Response = "I understand you'd like to speak with a human representative. Our customer support team is available Monday-Friday, 9 AM - 6 PM. You can also reach them at support@example.com or call (555) 123-4567.",
                Confidence = 0.9f,
                StrategyUsed = "RuleBased_HumanHandoff",
                IsRuleBased = true,
                RequiresHumanHandoff = true
            },
            
            Intent.Support when normalizedQuery.Contains("refund") => new FallbackResponse
            {
                Response = "For refund requests, please visit your order history in your account or contact our support team. Refunds are typically processed within 5-7 business days after we receive the returned item.",
                Confidence = 0.85f,
                StrategyUsed = "RuleBased_Support_Refund",
                IsRuleBased = true
            },
            
            Intent.Support when normalizedQuery.Contains("return") => new FallbackResponse
            {
                Response = "Our return policy allows returns within 30 days of purchase. Items must be unused and in original packaging. You can initiate a return from your order history page.",
                Confidence = 0.85f,
                StrategyUsed = "RuleBased_Support_Return",
                IsRuleBased = true
            },
            
            Intent.OrderManagement when normalizedQuery.Contains("track") => new FallbackResponse
            {
                Response = "You can track your order by visiting the 'My Orders' section in your account. There you'll find tracking numbers and delivery status for all your recent orders.",
                Confidence = 0.85f,
                StrategyUsed = "RuleBased_Order_Tracking",
                IsRuleBased = true
            },
            
            _ => null
        };
    }

    private async Task<FallbackResponse?> TryPrimaryRagAsync(
        string query,
        ChatContext context,
        QueryAnalysis analysis,
        Func<string, Task>? onToken,
        CancellationToken cancellationToken)
    {
        try
        {
            var products = await _semanticSearchService.SearchAsync(
                query,
                _chatOptions.ContextProductCount,
                tenantId: null,
                shopId: null,
                cancellationToken);

            if (products.Count == 0)
            {
                return null;
            }

            var prompt = BuildPromptWithProducts(query, context, products);
            var responseBuilder = new StringBuilder();

            var request = new GenerationRequest
            {
                Messages = BuildMessages(prompt, context),
                Temperature = _aiOptions.GenerationTemperature,
                MaxTokens = _aiOptions.MaxGenerationTokens,
                Stream = true
            };

            await foreach (var token in _llmProvider.GenerateStreamingResponseAsync(request, cancellationToken))
            {
                responseBuilder.Append(token);
                if (onToken != null)
                {
                    await onToken(token);
                }
            }

            var confidence = CalculateConfidence(products, responseBuilder.Length);

            return new FallbackResponse
            {
                Response = responseBuilder.ToString(),
                Confidence = confidence,
                StrategyUsed = "PrimaryRAG",
                IsRuleBased = false,
                ContextProducts = products
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Primary RAG strategy failed for query: {Query}", query);
            return null;
        }
    }

    private async Task<FallbackResponse?> TryExpandedSearchAsync(
        string query,
        ChatContext context,
        QueryAnalysis analysis,
        Func<string, Task>? onToken,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Trying expanded search for query: {Query}", query);

            var products = await _semanticSearchService.SearchAsync(
                query,
                _chatOptions.ContextProductCount * 2,
                tenantId: null,
                shopId: null,
                cancellationToken);

            if (products.Count == 0)
            {
                return null;
            }

            var prompt = BuildPromptWithProducts(query, context, products);
            var responseBuilder = new StringBuilder();

            var request = new GenerationRequest
            {
                Messages = BuildMessages(prompt, context),
                Temperature = _aiOptions.GenerationTemperature + 0.1f,
                MaxTokens = _aiOptions.MaxGenerationTokens,
                Stream = true
            };

            await foreach (var token in _llmProvider.GenerateStreamingResponseAsync(request, cancellationToken))
            {
                responseBuilder.Append(token);
                if (onToken != null)
                {
                    await onToken(token);
                }
            }

            var confidence = CalculateConfidence(products, responseBuilder.Length) * 0.9f;

            return new FallbackResponse
            {
                Response = responseBuilder.ToString(),
                Confidence = confidence,
                StrategyUsed = "ExpandedSearch",
                IsRuleBased = false,
                ContextProducts = products
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Expanded search strategy failed for query: {Query}", query);
            return null;
        }
    }

    private FallbackResponse GetFinalFallbackResponse(string query, QueryAnalysis analysis)
    {
        var message = analysis.Intent switch
        {
            Intent.ProductDiscovery => 
                "I couldn't find specific products matching your search. Could you try different keywords or browse our categories? I'm here to help you find what you're looking for!",
            
            Intent.PriceInquiry => 
                "I don't have current pricing information for that specific item. You can check our website for the most up-to-date prices, or tell me more about what you're looking for.",
            
            Intent.AvailabilityInquiry => 
                "I don't have availability information for that item at the moment. Please check our website for current stock status, or let me know if you'd like to see similar products.",
            
            _ => 
                "I'm not sure I understood your question completely. I can help you find products, check prices, or answer questions about our services. Could you please rephrase your question?"
        };

        return new FallbackResponse
        {
            Response = message,
            Confidence = 0.3f,
            StrategyUsed = "FinalFallback",
            IsRuleBased = true
        };
    }

    private string BuildPromptWithProducts(string query, ChatContext context, List<ProductSearchResult> products)
    {
        var sb = new StringBuilder();
        sb.AppendLine(SystemPrompt);
        sb.AppendLine();

        if (products.Count > 0)
        {
            sb.AppendLine("=== PRODUCT CONTEXT ===");
            sb.AppendLine();

            foreach (var product in products)
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

        sb.AppendLine($"Customer Question: {query}");

        return sb.ToString();
    }

    private List<GenerationMessage> BuildMessages(string prompt, ChatContext context)
    {
        var messages = new List<GenerationMessage>();

        foreach (var msg in context.Messages.TakeLast(10))
        {
            messages.Add(new GenerationMessage
            {
                Role = msg.Role == ChatMessageRole.User ? "user" : "assistant",
                Content = msg.Content,
                Timestamp = msg.CreatedAt
            });
        }

        messages.Add(new GenerationMessage
        {
            Role = "user",
            Content = prompt,
            Timestamp = DateTime.UtcNow
        });

        return messages;
    }

    private float CalculateConfidence(List<ProductSearchResult> products, int responseLength)
    {
        var avgRelevance = products.Count > 0 
            ? (float)products.Average(p => p.RelevanceScore) 
            : 0f;

        var productCountFactor = Math.Min(products.Count / 5f, 1f);
        var responseLengthFactor = Math.Min(responseLength / 200f, 1f);

        return (avgRelevance * 0.5f) + (productCountFactor * 0.3f) + (responseLengthFactor * 0.2f);
    }
}
