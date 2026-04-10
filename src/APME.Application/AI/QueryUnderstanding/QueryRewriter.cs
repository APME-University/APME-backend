using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace APME.AI.QueryUnderstanding;

/// <summary>
/// Query rewriter that improves ambiguous or unclear queries for better search results.
/// Uses strategy-based rewriting with LLM assistance.
/// </summary>
public class QueryRewriter : IQueryRewriter, ITransientDependency
{
    private readonly ILlmProvider _llmProvider;
    private readonly QueryUnderstandingOptions _options;
    private readonly ILogger<QueryRewriter> _logger;

    // Ambiguous terms that often need clarification
    private static readonly HashSet<string> AmbiguousTerms = new(StringComparer.OrdinalIgnoreCase)
    {
        "thing", "things", "stuff", "item", "items", "product", "products",
        "good", "nice", "cool", "awesome", "great", "something", "anything",
        "one", "it", "that", "this", "these", "those", "some"
    };

    // Rewrite strategy prompts
    private static readonly Dictionary<string, string> StrategyPrompts = new()
    {
        {
            "product_specification_focus",
            @"Rewrite this query to focus on product specifications and detailed features. The query should ask for specific technical details, features, and capabilities."
        },
        {
            "category_navigation_optimization",
            @"Rewrite this query to be more specific about the product category. Include category-specific terms and navigation keywords."
        },
        {
            "purchase_intent_clarification",
            @"Rewrite this query to clarify purchase intent. Include terms related to buying, pricing, availability, or transaction."
        },
        {
            "problem_description_enhancement",
            @"Rewrite this query to better describe the support issue or problem. Include specific details about what's not working or what help is needed."
        },
        {
            "comparison_criteria_expansion",
            @"Rewrite this query to clearly specify what aspects should be compared. Include comparison criteria like price, features, quality, etc."
        },
        {
            "intent_disambiguation",
            @"Rewrite this ambiguous query to be more specific about what the user is looking for. Clarify the product type, category, or action intended."
        },
        {
            "entity_extraction_fallback",
            @"Rewrite this query to include more specific product-related terms like category, brand, or attributes that would help identify what the user wants."
        },
        {
            "general_query_optimization",
            @"Optimize this search query for better product search results. Make it more specific and searchable while keeping the original intent."
        }
    };

    public QueryRewriter(
        ILlmProvider llmProvider,
        IOptions<QueryUnderstandingOptions> options,
        ILogger<QueryRewriter> logger)
    {
        _llmProvider = llmProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<QueryRewriteResult> RewriteQueryAsync(
        string query,
        IntentClassificationResult intentResult,
        List<EntityExtractionResult> entities,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || !_options.EnableQueryRewriting)
        {
            return new QueryRewriteResult
            {
                OriginalQuery = query,
                RewrittenQuery = query,
                WasRewritten = false,
                Reason = "Rewriting disabled or empty query",
                Confidence = 1.0f
            };
        }

        var stopwatch = Stopwatch.StartNew();

        // Step 1: Determine if rewriting is needed
        var (shouldRewrite, reason) = ShouldRewriteQuery(query, intentResult, entities);

        if (!shouldRewrite)
        {
            stopwatch.Stop();
            return new QueryRewriteResult
            {
                OriginalQuery = query,
                RewrittenQuery = query,
                WasRewritten = false,
                Reason = reason,
                Confidence = 1.0f,
                ProcessingTime = stopwatch.Elapsed
            };
        }

        // Step 2: Determine rewrite strategy
        var strategy = DetermineRewriteStrategy(intentResult, entities, query);

        // Step 3: Try rule-based rewriting first
        var ruleBasedResult = TryRuleBasedRewrite(query, strategy, entities);
        if (ruleBasedResult != null)
        {
            stopwatch.Stop();
            ruleBasedResult.ProcessingTime = stopwatch.Elapsed;
            
            _logger.LogDebug(
                "Rule-based query rewrite applied. Strategy: {Strategy}, Original: '{Original}', Rewritten: '{Rewritten}'",
                strategy, query, ruleBasedResult.RewrittenQuery);
            
            return ruleBasedResult;
        }

        // Step 4: LLM-based rewriting
        if (_options.EnableLlmFallback)
        {
            try
            {
                var llmResult = await RewriteWithLlmAsync(query, strategy, intentResult, entities, cancellationToken);
                stopwatch.Stop();
                llmResult.ProcessingTime = stopwatch.Elapsed;

                _logger.LogDebug(
                    "LLM query rewrite applied. Strategy: {Strategy}, Original: '{Original}', Rewritten: '{Rewritten}'",
                    strategy, query, llmResult.RewrittenQuery);

                return llmResult;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LLM query rewriting failed for query: {Query}", query);
            }
        }

        // Fallback: return original query
        stopwatch.Stop();
        return new QueryRewriteResult
        {
            OriginalQuery = query,
            RewrittenQuery = query,
            WasRewritten = false,
            Reason = "Rewriting failed, using original query",
            Strategy = strategy,
            Confidence = 0.5f,
            ProcessingTime = stopwatch.Elapsed
        };
    }

    private (bool ShouldRewrite, string Reason) ShouldRewriteQuery(
        string query,
        IntentClassificationResult intentResult,
        List<EntityExtractionResult> entities)
    {
        // Don't rewrite greetings, farewells, or human handoff requests
        if (intentResult.PrimaryIntent is IntentType.Greeting or IntentType.Farewell or IntentType.HumanHandoff)
        {
            return (false, "Intent does not require rewriting");
        }

        // Rewrite if intent confidence is low
        if (intentResult.PrimaryConfidence < _options.RewriteConfidenceThreshold)
        {
            return (true, "Low intent confidence");
        }

        // Rewrite if no high-confidence entities found
        var hasHighConfidenceEntities = entities.Any(e => e.Confidence >= 0.8f);
        if (!hasHighConfidenceEntities && intentResult.PrimaryIntent != IntentType.Support)
        {
            return (true, "No high-confidence entities detected");
        }

        // Rewrite if query is too short
        var wordCount = query.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        if (wordCount < _options.MinQueryWordsForNoRewrite)
        {
            return (true, "Query too short");
        }

        // Rewrite if query contains ambiguous terms
        var words = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var hasAmbiguousTerms = words.Any(w => AmbiguousTerms.Contains(w));
        if (hasAmbiguousTerms)
        {
            return (true, "Contains ambiguous terms");
        }

        // Rewrite if intent is ambiguous (close confidence scores)
        if (intentResult.SecondaryConfidence.HasValue &&
            intentResult.PrimaryConfidence - intentResult.SecondaryConfidence.Value < 0.2f)
        {
            return (true, "Ambiguous intent");
        }

        return (false, "Query is clear enough");
    }

    private string DetermineRewriteStrategy(
        IntentClassificationResult intentResult,
        List<EntityExtractionResult> entities,
        string query)
    {
        // Strategy based on primary intent
        switch (intentResult.PrimaryIntent)
        {
            case IntentType.Informational:
                var hasProductEntity = entities.Any(e =>
                    e.EntityType is EntityTypes.ProductId or EntityTypes.Brand or EntityTypes.Category);
                return hasProductEntity ? "product_specification_focus" : "general_query_optimization";

            case IntentType.Navigational:
                return "category_navigation_optimization";

            case IntentType.Transactional:
                return "purchase_intent_clarification";

            case IntentType.Support:
                return "problem_description_enhancement";

            case IntentType.Comparison:
                return "comparison_criteria_expansion";

            case IntentType.Discovery:
                return "category_navigation_optimization";

            case IntentType.Account:
                return "general_query_optimization";
        }

        // Fallback strategies
        if (!entities.Any())
        {
            return "entity_extraction_fallback";
        }

        if (intentResult.PrimaryConfidence < 0.5f)
        {
            return "intent_disambiguation";
        }

        return "general_query_optimization";
    }

    private QueryRewriteResult? TryRuleBasedRewrite(
        string query,
        string strategy,
        List<EntityExtractionResult> entities)
    {
        var normalizedQuery = query.Trim();
        string? rewrittenQuery = null;

        switch (strategy)
        {
            case "category_navigation_optimization":
                // Add "products" or "items" if missing product-related term
                if (!normalizedQuery.Contains("product", StringComparison.OrdinalIgnoreCase) &&
                    !normalizedQuery.Contains("item", StringComparison.OrdinalIgnoreCase))
                {
                    var category = entities.FirstOrDefault(e => e.EntityType == EntityTypes.Category);
                    if (category != null)
                    {
                        rewrittenQuery = $"{category.Value} products {normalizedQuery.Replace(category.Value, "", StringComparison.OrdinalIgnoreCase).Trim()}".Trim();
                    }
                    else
                    {
                        rewrittenQuery = $"{normalizedQuery} products".Trim();
                    }
                }
                break;

            case "purchase_intent_clarification":
                // Add "buy" or "purchase" if transactional but not explicit
                if (!normalizedQuery.Contains("buy", StringComparison.OrdinalIgnoreCase) &&
                    !normalizedQuery.Contains("purchase", StringComparison.OrdinalIgnoreCase) &&
                    !normalizedQuery.Contains("order", StringComparison.OrdinalIgnoreCase))
                {
                    rewrittenQuery = $"buy {normalizedQuery}";
                }
                break;

            case "product_specification_focus":
                // Add "specifications" or "features" if asking about product details
                var brandEntity = entities.FirstOrDefault(e => e.EntityType == EntityTypes.Brand);
                var categoryEntity = entities.FirstOrDefault(e => e.EntityType == EntityTypes.Category);
                
                if (brandEntity != null || categoryEntity != null)
                {
                    var productPart = brandEntity?.Value ?? categoryEntity?.Value ?? "";
                    if (!normalizedQuery.Contains("spec", StringComparison.OrdinalIgnoreCase) &&
                        !normalizedQuery.Contains("feature", StringComparison.OrdinalIgnoreCase) &&
                        !normalizedQuery.Contains("detail", StringComparison.OrdinalIgnoreCase))
                    {
                        rewrittenQuery = $"{productPart} specifications features {normalizedQuery.Replace(productPart, "", StringComparison.OrdinalIgnoreCase).Trim()}".Trim();
                    }
                }
                break;

            case "intent_disambiguation":
            case "entity_extraction_fallback":
                // Try to add context from existing entities
                var significantEntities = entities.Where(e => e.Confidence >= 0.7f).ToList();
                if (significantEntities.Any())
                {
                    var entityValues = string.Join(" ", significantEntities.Select(e => e.Value).Distinct());
                    if (!normalizedQuery.Contains(entityValues, StringComparison.OrdinalIgnoreCase))
                    {
                        rewrittenQuery = $"{entityValues} {normalizedQuery}".Trim();
                    }
                }
                break;
        }

        if (rewrittenQuery != null && !string.Equals(rewrittenQuery, query, StringComparison.OrdinalIgnoreCase))
        {
            // Clean up double spaces
            rewrittenQuery = string.Join(" ", rewrittenQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries));

            return new QueryRewriteResult
            {
                OriginalQuery = query,
                RewrittenQuery = rewrittenQuery,
                WasRewritten = true,
                Reason = $"Applied {strategy} rule-based rewrite",
                Strategy = strategy,
                Confidence = 0.75f
            };
        }

        return null;
    }

    private async Task<QueryRewriteResult> RewriteWithLlmAsync(
        string query,
        string strategy,
        IntentClassificationResult intentResult,
        List<EntityExtractionResult> entities,
        CancellationToken cancellationToken)
    {
        var strategyDescription = StrategyPrompts.GetValueOrDefault(strategy, StrategyPrompts["general_query_optimization"]);
        var entityContext = entities.Count > 0
            ? string.Join(", ", entities.Select(e => $"{e.EntityType}: {e.Value}"))
            : "none detected";

        var prompt = $@"You are an expert query rewriter for e-commerce search optimization.

ORIGINAL QUERY: ""{query}""
DETECTED INTENT: {intentResult.PrimaryIntent} (confidence: {intentResult.PrimaryConfidence:F2})
DETECTED ENTITIES: {entityContext}

REWRITE STRATEGY: {strategyDescription}

REQUIREMENTS:
- Keep the core intent of the original query
- Make the query more specific and searchable
- Add relevant product-related terms if helpful
- Keep the rewritten query concise (2-8 words)
- Output ONLY the rewritten query, nothing else

REWRITTEN QUERY:";

        var request = new GenerationRequest
        {
            Messages = new List<GenerationMessage>
            {
                new GenerationMessage { Role = "user", Content = prompt }
            },
            Temperature = 0.2f,
            MaxTokens = 100,
            Stream = false
        };

        var response = await _llmProvider.GenerateResponseAsync(request, cancellationToken);
        var rewrittenQuery = CleanLlmResponse(response.Content);

        // Validate the rewritten query
        if (string.IsNullOrWhiteSpace(rewrittenQuery) ||
            rewrittenQuery.Length > query.Length * 3 ||
            rewrittenQuery.Length < 2)
        {
            return new QueryRewriteResult
            {
                OriginalQuery = query,
                RewrittenQuery = query,
                WasRewritten = false,
                Reason = "LLM rewrite was invalid",
                Strategy = strategy,
                Confidence = 0.3f
            };
        }

        return new QueryRewriteResult
        {
            OriginalQuery = query,
            RewrittenQuery = rewrittenQuery,
            WasRewritten = true,
            Reason = $"Applied {strategy} LLM rewrite",
            Strategy = strategy,
            Confidence = CalculateRewriteConfidence(query, rewrittenQuery, intentResult, entities)
        };
    }

    private string CleanLlmResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return string.Empty;

        // Remove common LLM response artifacts
        var cleaned = response
            .Trim()
            .Trim('"', '\'')
            .Trim();

        // Take only the first line if multiple lines
        var lines = cleaned.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length > 0)
        {
            cleaned = lines[0].Trim();
        }

        // Remove any "Rewritten query:" prefix
        var prefixes = new[] { "rewritten query:", "result:", "query:", "output:" };
        foreach (var prefix in prefixes)
        {
            if (cleaned.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned.Substring(prefix.Length).Trim();
            }
        }

        return cleaned.Trim('"', '\'').Trim();
    }

    private float CalculateRewriteConfidence(
        string originalQuery,
        string rewrittenQuery,
        IntentClassificationResult intentResult,
        List<EntityExtractionResult> entities)
    {
        var confidence = 0.5f;

        // Higher confidence if we have entities that are preserved
        if (entities.Any(e => e.Confidence >= 0.8f &&
            rewrittenQuery.Contains(e.Value, StringComparison.OrdinalIgnoreCase)))
        {
            confidence += 0.2f;
        }

        // Higher confidence if intent was clear
        if (intentResult.PrimaryConfidence >= 0.7f)
        {
            confidence += 0.15f;
        }

        // Higher confidence if query became more specific (but not too long)
        var originalWords = originalQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var rewrittenWords = rewrittenQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        
        if (rewrittenWords > originalWords && rewrittenWords <= originalWords + 3)
        {
            confidence += 0.1f;
        }

        // Lower confidence if query changed drastically
        if (!rewrittenQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Any(w => originalQuery.Contains(w, StringComparison.OrdinalIgnoreCase)))
        {
            confidence -= 0.2f;
        }

        return Math.Clamp(confidence, 0.3f, 0.95f);
    }
}
