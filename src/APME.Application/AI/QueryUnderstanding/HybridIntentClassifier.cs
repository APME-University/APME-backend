using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace APME.AI.QueryUnderstanding;

/// <summary>
/// Hybrid intent classifier using rule-based fast path with LLM fallback.
/// </summary>
public class HybridIntentClassifier : IIntentClassifier, ITransientDependency
{
    private readonly ILlmProvider _llmProvider;
    private readonly QueryUnderstandingOptions _options;
    private readonly ILogger<HybridIntentClassifier> _logger;

    private static readonly Dictionary<string, IntentType> GreetingPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        { "hello", IntentType.Greeting },
        { "hi ", IntentType.Greeting },
        { "hi!", IntentType.Greeting },
        { "hey ", IntentType.Greeting },
        { "hey!", IntentType.Greeting },
        { "good morning", IntentType.Greeting },
        { "good afternoon", IntentType.Greeting },
        { "good evening", IntentType.Greeting },
        { "greetings", IntentType.Greeting },
        { "howdy", IntentType.Greeting },
        { "what's up", IntentType.Greeting },
        { "whats up", IntentType.Greeting }
    };

    private static readonly Dictionary<string, IntentType> FarewellPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        { "bye", IntentType.Farewell },
        { "goodbye", IntentType.Farewell },
        { "see you", IntentType.Farewell },
        { "take care", IntentType.Farewell },
        { "have a good", IntentType.Farewell },
        { "thanks", IntentType.Farewell },
        { "thank you", IntentType.Farewell },
        { "cheers", IntentType.Farewell },
        { "later", IntentType.Farewell },
        { "cya", IntentType.Farewell }
    };

    private static readonly string[] InformationalPatterns =
    {
        "what is", "how does", "tell me about", "describe", "explain",
        "features of", "specifications", "specs", "details", "information",
        "can you tell me", "what are the"
    };

    private static readonly string[] NavigationalPatterns =
    {
        "find", "search", "show me", "where can i find", "locate", "browse",
        "look for", "looking for", "want to see", "show", "display"
    };

    private static readonly string[] TransactionalPatterns =
    {
        "buy", "purchase", "order", "checkout", "add to cart", "cart",
        "price", "cost", "how much", "shipping", "delivery", "payment"
    };

    private static readonly string[] SupportPatterns =
    {
        "help", "support", "issue", "problem", "broken", "not working",
        "complaint", "refund", "return", "exchange", "warranty", "trouble"
    };

    private static readonly string[] ComparisonPatterns =
    {
        "compare", "versus", " vs ", "difference between", "better",
        "which is better", "which one", "or ", "alternative"
    };

    private static readonly string[] DiscoveryPatterns =
    {
        "recommend", "suggest", "suggestion", "best", "top rated",
        "popular", "trending", "new arrival", "what should i",
        "any recommendations"
    };

    private static readonly string[] AccountPatterns =
    {
        "my order", "my orders", "order history", "account", "profile",
        "my purchases", "track my", "status of my"
    };

    private static readonly string[] HumanHandoffPatterns =
    {
        "human", "agent", "representative", "real person", "speak to someone",
        "customer service", "talk to", "connect me", "live chat"
    };

    public HybridIntentClassifier(
        ILlmProvider llmProvider,
        IOptions<QueryUnderstandingOptions> options,
        ILogger<HybridIntentClassifier> logger)
    {
        _llmProvider = llmProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IntentClassificationResult> ClassifyIntentAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new IntentClassificationResult
            {
                PrimaryIntent = IntentType.Unknown,
                PrimaryConfidence = 0f,
                UsedRuleBased = true
            };
        }

        var stopwatch = Stopwatch.StartNew();
        var normalizedQuery = NormalizeQuery(query);

        // Try rule-based classification first (fast path)
        var ruleBasedResult = ClassifyWithRules(normalizedQuery);

        if (ruleBasedResult.PrimaryConfidence >= _options.RuleBasedConfidenceThreshold)
        {
            stopwatch.Stop();
            ruleBasedResult.ProcessingTime = stopwatch.Elapsed;
            ruleBasedResult.UsedRuleBased = true;

            _logger.LogDebug(
                "Rule-based intent classification succeeded: {Intent} with confidence {Confidence} in {Time}ms",
                ruleBasedResult.PrimaryIntent,
                ruleBasedResult.PrimaryConfidence,
                stopwatch.ElapsedMilliseconds);

            return ruleBasedResult;
        }

        // LLM fallback if enabled and rule-based confidence is low
        if (_options.EnableLlmFallback)
        {
            try
            {
                var llmResult = await ClassifyWithLlmAsync(normalizedQuery, cancellationToken);
                var combinedResult = CombineResults(ruleBasedResult, llmResult);

                stopwatch.Stop();
                combinedResult.ProcessingTime = stopwatch.Elapsed;
                combinedResult.UsedLlmFallback = true;

                _logger.LogDebug(
                    "LLM fallback intent classification: {Intent} with confidence {Confidence} in {Time}ms",
                    combinedResult.PrimaryIntent,
                    combinedResult.PrimaryConfidence,
                    stopwatch.ElapsedMilliseconds);

                return combinedResult;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LLM fallback failed, using rule-based result");
            }
        }

        stopwatch.Stop();
        ruleBasedResult.ProcessingTime = stopwatch.Elapsed;
        ruleBasedResult.UsedRuleBased = true;

        return ruleBasedResult;
    }

    private string NormalizeQuery(string query)
    {
        return query.ToLowerInvariant()
            .Trim()
            .Replace("  ", " ")
            .Replace("?", "")
            .Replace("!", "")
            .Replace(".", "");
    }

    private IntentClassificationResult ClassifyWithRules(string normalizedQuery)
    {
        var scores = new Dictionary<IntentType, float>();

        foreach (var intent in Enum.GetValues<IntentType>())
        {
            scores[intent] = 0f;
        }

        // Check greeting patterns
        foreach (var pattern in GreetingPatterns.Keys)
        {
            if (normalizedQuery.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                scores[IntentType.Greeting] = Math.Max(scores[IntentType.Greeting], 0.95f);
            }
        }

        // Check farewell patterns
        foreach (var pattern in FarewellPatterns.Keys)
        {
            if (normalizedQuery.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                scores[IntentType.Farewell] = Math.Max(scores[IntentType.Farewell], 0.95f);
            }
        }

        // Check human handoff patterns (high priority)
        foreach (var pattern in HumanHandoffPatterns)
        {
            if (normalizedQuery.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                scores[IntentType.HumanHandoff] = Math.Max(scores[IntentType.HumanHandoff], 0.9f);
            }
        }

        // Check support patterns
        foreach (var pattern in SupportPatterns)
        {
            if (normalizedQuery.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                scores[IntentType.Support] += 0.25f;
            }
        }

        // Check account patterns
        foreach (var pattern in AccountPatterns)
        {
            if (normalizedQuery.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                scores[IntentType.Account] += 0.3f;
            }
        }

        // Check comparison patterns
        foreach (var pattern in ComparisonPatterns)
        {
            if (normalizedQuery.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                scores[IntentType.Comparison] += 0.25f;
            }
        }

        // Check discovery patterns
        foreach (var pattern in DiscoveryPatterns)
        {
            if (normalizedQuery.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                scores[IntentType.Discovery] += 0.25f;
            }
        }

        // Check transactional patterns
        foreach (var pattern in TransactionalPatterns)
        {
            if (normalizedQuery.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                scores[IntentType.Transactional] += 0.2f;
            }
        }

        // Check navigational patterns
        foreach (var pattern in NavigationalPatterns)
        {
            if (normalizedQuery.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                scores[IntentType.Navigational] += 0.2f;
            }
        }

        // Check informational patterns
        foreach (var pattern in InformationalPatterns)
        {
            if (normalizedQuery.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                scores[IntentType.Informational] += 0.25f;
            }
        }

        // Question word indicators
        if (normalizedQuery.StartsWith("what") || normalizedQuery.StartsWith("how") ||
            normalizedQuery.StartsWith("why") || normalizedQuery.StartsWith("when") ||
            normalizedQuery.StartsWith("where") || normalizedQuery.StartsWith("which"))
        {
            scores[IntentType.Informational] += 0.15f;
        }

        // Cap all scores at 1.0
        foreach (var key in scores.Keys.ToList())
        {
            scores[key] = Math.Min(scores[key], 1.0f);
        }

        // If no strong signals, check for product-related keywords
        var maxScore = scores.Values.Max();
        if (maxScore < 0.5f)
        {
            var productKeywords = new[] { "product", "item", "buy", "shop", "store" };
            if (productKeywords.Any(k => normalizedQuery.Contains(k)))
            {
                scores[IntentType.Navigational] = Math.Max(scores[IntentType.Navigational], 0.5f);
            }
        }

        // Find primary and secondary intents
        var sortedScores = scores.OrderByDescending(kvp => kvp.Value).ToList();
        var primaryIntent = sortedScores[0].Key;
        var primaryConfidence = sortedScores[0].Value;

        // If primary score is still very low, default to Unknown
        if (primaryConfidence < 0.2f)
        {
            primaryIntent = IntentType.Unknown;
            primaryConfidence = 0.3f;
        }

        var secondaryIntent = sortedScores.Count > 1 && sortedScores[1].Value > 0.2f
            ? sortedScores[1].Key
            : (IntentType?)null;
        var secondaryConfidence = sortedScores.Count > 1 ? sortedScores[1].Value : 0f;

        return new IntentClassificationResult
        {
            PrimaryIntent = primaryIntent,
            PrimaryConfidence = primaryConfidence,
            SecondaryIntent = secondaryIntent,
            SecondaryConfidence = secondaryConfidence > 0 ? secondaryConfidence : null,
            AllIntentScores = scores
        };
    }

    private async Task<IntentClassificationResult> ClassifyWithLlmAsync(
        string normalizedQuery,
        CancellationToken cancellationToken)
    {
        var prompt = $@"You are an expert intent classifier for an e-commerce chatbot. Analyze the user query and determine the primary intent.

USER QUERY: ""{normalizedQuery}""

AVAILABLE INTENTS:
1. Informational - User wants to know details about products (features, specifications, usage)
2. Navigational - User is looking for specific products or categories to browse
3. Transactional - User wants to buy, order, or get pricing/shipping information
4. Support - User has problems, needs help, or wants returns/refunds
5. Comparison - User wants to compare products or features
6. Discovery - User wants recommendations or suggestions for products
7. Account - User is asking about their account, orders, or personal information
8. Greeting - User is greeting or starting conversation
9. Farewell - User is ending conversation or saying thanks
10. HumanHandoff - User wants to speak with a human agent
11. Unknown - Cannot determine intent

ANALYSIS REQUIREMENTS:
- Consider the context and implied meaning, not just keywords
- Look for action verbs and question types
- Assign confidence scores (0.0-1.0) based on clarity of intent
- If multiple intents are present, identify primary and secondary intents

OUTPUT FORMAT (JSON only, no other text):
{{""primary_intent"": ""Informational"", ""primary_confidence"": 0.9, ""secondary_intent"": ""Navigational"", ""secondary_confidence"": 0.3}}";

        var request = new GenerationRequest
        {
            Messages = new List<GenerationMessage>
            {
                new GenerationMessage { Role = "user", Content = prompt }
            },
            Temperature = 0.1f,
            MaxTokens = 200,
            Stream = false
        };

        var response = await _llmProvider.GenerateResponseAsync(request, cancellationToken);
        return ParseLlmResponse(response.Content);
    }

    private IntentClassificationResult ParseLlmResponse(string jsonResponse)
    {
        if (string.IsNullOrWhiteSpace(jsonResponse))
        {
            return CreateUnknownResult();
        }

        try
        {
            var json = ExtractJsonObject(jsonResponse);
            
            if (string.IsNullOrEmpty(json))
            {
                _logger.LogDebug("No valid JSON object found in LLM intent response");
                return CreateUnknownResult();
            }

            var parsed = JsonSerializer.Deserialize<LlmIntentResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            });

            if (parsed != null && !string.IsNullOrEmpty(parsed.PrimaryIntent))
            {
                var primaryIntent = ParseIntentType(parsed.PrimaryIntent);
                var secondaryIntent = !string.IsNullOrEmpty(parsed.SecondaryIntent)
                    ? ParseIntentType(parsed.SecondaryIntent)
                    : (IntentType?)null;

                return new IntentClassificationResult
                {
                    PrimaryIntent = primaryIntent,
                    PrimaryConfidence = parsed.PrimaryConfidence > 0 ? parsed.PrimaryConfidence : 0.5f,
                    SecondaryIntent = secondaryIntent,
                    SecondaryConfidence = parsed.SecondaryConfidence > 0 ? parsed.SecondaryConfidence : null,
                    AllIntentScores = new Dictionary<IntentType, float>
                    {
                        { primaryIntent, parsed.PrimaryConfidence > 0 ? parsed.PrimaryConfidence : 0.5f }
                    }
                };
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "JSON parsing failed for LLM intent response: {Response}",
                jsonResponse.Length > 200 ? jsonResponse.Substring(0, 200) + "..." : jsonResponse);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse LLM intent response");
        }

        return CreateUnknownResult();
    }

    private static IntentClassificationResult CreateUnknownResult()
    {
        return new IntentClassificationResult
        {
            PrimaryIntent = IntentType.Unknown,
            PrimaryConfidence = 0.3f,
            AllIntentScores = new Dictionary<IntentType, float> { { IntentType.Unknown, 0.3f } }
        };
    }

    private static string? ExtractJsonObject(string response)
    {
        var jsonStart = response.IndexOf('{');
        if (jsonStart < 0)
        {
            return null;
        }

        var braceCount = 0;
        var inString = false;
        var escapeNext = false;

        for (var i = jsonStart; i < response.Length; i++)
        {
            var c = response[i];

            if (escapeNext)
            {
                escapeNext = false;
                continue;
            }

            if (c == '\\' && inString)
            {
                escapeNext = true;
                continue;
            }

            if (c == '"')
            {
                inString = !inString;
                continue;
            }

            if (inString)
            {
                continue;
            }

            if (c == '{')
            {
                braceCount++;
            }
            else if (c == '}')
            {
                braceCount--;
                if (braceCount == 0)
                {
                    return response.Substring(jsonStart, i - jsonStart + 1);
                }
            }
        }

        return null;
    }

    private IntentType ParseIntentType(string intentString)
    {
        if (string.IsNullOrEmpty(intentString))
            return IntentType.Unknown;

        return intentString.ToLowerInvariant() switch
        {
            "informational" => IntentType.Informational,
            "navigational" => IntentType.Navigational,
            "transactional" => IntentType.Transactional,
            "support" => IntentType.Support,
            "comparison" => IntentType.Comparison,
            "discovery" => IntentType.Discovery,
            "account" => IntentType.Account,
            "greeting" => IntentType.Greeting,
            "farewell" => IntentType.Farewell,
            "humanhandoff" => IntentType.HumanHandoff,
            "human_handoff" => IntentType.HumanHandoff,
            _ => IntentType.Unknown
        };
    }

    private IntentClassificationResult CombineResults(
        IntentClassificationResult ruleBased,
        IntentClassificationResult llmBased)
    {
        // If rule-based has high confidence, prefer it
        if (ruleBased.PrimaryConfidence >= 0.7f)
        {
            return ruleBased;
        }

        // If LLM has significantly higher confidence, use it
        if (llmBased.PrimaryConfidence > ruleBased.PrimaryConfidence + 0.2f)
        {
            return llmBased;
        }

        // Otherwise, blend the results
        var blended = new IntentClassificationResult();

        if (llmBased.PrimaryConfidence > ruleBased.PrimaryConfidence)
        {
            blended.PrimaryIntent = llmBased.PrimaryIntent;
            blended.SecondaryIntent = ruleBased.PrimaryIntent != llmBased.PrimaryIntent
                ? ruleBased.PrimaryIntent
                : llmBased.SecondaryIntent;
        }
        else
        {
            blended.PrimaryIntent = ruleBased.PrimaryIntent;
            blended.SecondaryIntent = llmBased.PrimaryIntent != ruleBased.PrimaryIntent
                ? llmBased.PrimaryIntent
                : ruleBased.SecondaryIntent;
        }

        blended.PrimaryConfidence = (ruleBased.PrimaryConfidence + llmBased.PrimaryConfidence) / 2f;
        blended.SecondaryConfidence = Math.Min(
            ruleBased.PrimaryConfidence,
            llmBased.PrimaryConfidence);

        // Combine all intent scores
        blended.AllIntentScores = new Dictionary<IntentType, float>();
        foreach (var intent in Enum.GetValues<IntentType>())
        {
            var ruleScore = ruleBased.AllIntentScores.GetValueOrDefault(intent, 0f);
            var llmScore = llmBased.AllIntentScores.GetValueOrDefault(intent, 0f);
            blended.AllIntentScores[intent] = (ruleScore + llmScore) / 2f;
        }

        return blended;
    }

    private class LlmIntentResponse
    {
        public string PrimaryIntent { get; set; } = string.Empty;
        public float PrimaryConfidence { get; set; }
        public string? SecondaryIntent { get; set; }
        public float SecondaryConfidence { get; set; }
    }
}
