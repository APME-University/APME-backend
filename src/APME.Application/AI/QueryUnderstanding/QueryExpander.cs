using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace APME.AI.QueryUnderstanding;

/// <summary>
/// Query expander using synonym dictionaries, contextual expansion, and LLM-based generation.
/// </summary>
public class QueryExpander : IQueryExpander, ITransientDependency
{
    private readonly ILlmProvider _llmProvider;
    private readonly QueryUnderstandingOptions _options;
    private readonly ILogger<QueryExpander> _logger;

    // Product synonyms dictionary
    private static readonly Dictionary<string, List<string>> ProductSynonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        // Electronics
        { "smartphone", new List<string> { "mobile phone", "cell phone", "phone", "handset" } },
        { "phone", new List<string> { "smartphone", "mobile phone", "cell phone", "cellphone" } },
        { "laptop", new List<string> { "notebook", "portable computer", "ultrabook", "notebook computer" } },
        { "computer", new List<string> { "pc", "desktop", "workstation" } },
        { "tv", new List<string> { "television", "smart tv", "led tv", "oled tv", "flat screen" } },
        { "television", new List<string> { "tv", "smart tv", "screen" } },
        { "headphones", new List<string> { "earphones", "earbuds", "headset", "audio headphones", "wireless earbuds" } },
        { "earbuds", new List<string> { "headphones", "earphones", "wireless earbuds", "true wireless" } },
        { "watch", new List<string> { "smartwatch", "wrist watch", "timepiece", "smart watch" } },
        { "camera", new List<string> { "digital camera", "dslr", "mirrorless camera", "webcam" } },
        { "tablet", new List<string> { "ipad", "android tablet", "e-reader", "slate" } },
        { "speaker", new List<string> { "bluetooth speaker", "wireless speaker", "soundbar", "audio speaker" } },
        
        // Fashion
        { "shoes", new List<string> { "footwear", "sneakers", "trainers", "boots", "kicks" } },
        { "sneakers", new List<string> { "shoes", "trainers", "athletic shoes", "running shoes" } },
        { "dress", new List<string> { "gown", "frock", "outfit", "attire" } },
        { "shirt", new List<string> { "top", "blouse", "tee", "t-shirt" } },
        { "pants", new List<string> { "trousers", "jeans", "bottoms", "slacks" } },
        { "jeans", new List<string> { "denim", "pants", "denim pants" } },
        { "jacket", new List<string> { "coat", "blazer", "outerwear", "hoodie" } },
        { "bag", new List<string> { "backpack", "handbag", "purse", "tote", "luggage" } },
        
        // Home
        { "sofa", new List<string> { "couch", "settee", "loveseat", "sectional" } },
        { "couch", new List<string> { "sofa", "settee", "loveseat" } },
        { "bed", new List<string> { "mattress", "bedframe", "bedroom furniture" } },
        { "table", new List<string> { "desk", "dining table", "coffee table" } },
        { "chair", new List<string> { "seat", "stool", "armchair", "office chair" } },
        
        // Appliances
        { "refrigerator", new List<string> { "fridge", "freezer", "cooler" } },
        { "fridge", new List<string> { "refrigerator", "freezer" } },
        { "washer", new List<string> { "washing machine", "laundry machine" } },
        { "dryer", new List<string> { "tumble dryer", "clothes dryer" } },
        { "vacuum", new List<string> { "vacuum cleaner", "hoover", "robot vacuum" } }
    };

    // Attribute synonyms
    private static readonly Dictionary<string, List<string>> AttributeSynonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        { "cheap", new List<string> { "affordable", "budget", "inexpensive", "low cost", "economical" } },
        { "expensive", new List<string> { "premium", "luxury", "high-end", "costly", "pricey" } },
        { "small", new List<string> { "compact", "mini", "tiny", "portable", "little" } },
        { "large", new List<string> { "big", "huge", "spacious", "roomy", "oversized" } },
        { "fast", new List<string> { "quick", "speedy", "high-performance", "powerful", "rapid" } },
        { "best", new List<string> { "top rated", "highest rated", "most popular", "recommended" } },
        { "new", new List<string> { "latest", "newest", "recent", "modern", "current" } },
        { "wireless", new List<string> { "bluetooth", "cordless", "wifi", "wi-fi" } },
        { "waterproof", new List<string> { "water resistant", "weatherproof", "water-resistant" } },
        { "lightweight", new List<string> { "light", "featherweight", "portable" } }
    };

    // Category-specific expansion terms
    private static readonly Dictionary<string, List<string>> CategoryExpansions = new(StringComparer.OrdinalIgnoreCase)
    {
        { "electronics", new List<string> { "gadgets", "tech", "technology", "electronic devices" } },
        { "clothing", new List<string> { "apparel", "fashion", "wear", "garments" } },
        { "shoes", new List<string> { "footwear", "sneakers", "boots", "sandals" } },
        { "home", new List<string> { "household", "home decor", "furniture", "home goods" } },
        { "beauty", new List<string> { "cosmetics", "skincare", "makeup", "personal care" } },
        { "sports", new List<string> { "athletic", "fitness", "outdoor", "sportswear" } }
    };

    public QueryExpander(
        ILlmProvider llmProvider,
        IOptions<QueryUnderstandingOptions> options,
        ILogger<QueryExpander> logger)
    {
        _llmProvider = llmProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<QueryExpansionResult> ExpandQueryAsync(
        string query,
        List<EntityExtractionResult> entities,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || !_options.EnableQueryExpansion)
        {
            return new QueryExpansionResult
            {
                OriginalQuery = query,
                ExpandedQueries = new List<ExpandedQuery>
                {
                    new ExpandedQuery { Query = query, ExpansionType = "original", Confidence = 1.0f }
                }
            };
        }

        var stopwatch = Stopwatch.StartNew();
        var expandedQueries = new List<ExpandedQuery>
        {
            new ExpandedQuery { Query = query, ExpansionType = "original", Confidence = 1.0f }
        };

        // Step 1: Synonym expansion
        var synonymExpanded = ApplySynonymExpansion(query);
        expandedQueries.AddRange(synonymExpanded);

        // Step 2: Entity-based expansion
        var entityExpanded = ApplyEntityExpansion(query, entities);
        expandedQueries.AddRange(entityExpanded);

        // Step 3: Category-specific expansion
        var categoryExpanded = ApplyCategoryExpansion(query, entities);
        expandedQueries.AddRange(categoryExpanded);

        // Step 4: LLM-based contextual expansion (if enabled and budget allows)
        if (_options.EnableLlmFallback && expandedQueries.Count < _options.MaxQueryExpansions)
        {
            try
            {
                var llmExpanded = await ApplyLlmExpansionAsync(query, entities, cancellationToken);
                expandedQueries.AddRange(llmExpanded);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LLM query expansion failed");
            }
        }

        // Deduplicate and limit
        expandedQueries = DeduplicateExpansions(expandedQueries);
        expandedQueries = expandedQueries
            .OrderByDescending(q => q.Confidence)
            .Take(_options.MaxQueryExpansions)
            .ToList();

        stopwatch.Stop();

        _logger.LogDebug(
            "Query expansion completed in {Time}ms. Generated {Count} variants",
            stopwatch.ElapsedMilliseconds,
            expandedQueries.Count);

        return new QueryExpansionResult
        {
            OriginalQuery = query,
            ExpandedQueries = expandedQueries,
            ProcessingTime = stopwatch.Elapsed
        };
    }

    private List<ExpandedQuery> ApplySynonymExpansion(string query)
    {
        var expanded = new List<ExpandedQuery>();
        var normalizedQuery = query.ToLowerInvariant();

        // Product synonyms
        foreach (var synonym in ProductSynonyms)
        {
            if (normalizedQuery.Contains(synonym.Key))
            {
                foreach (var replacement in synonym.Value.Take(2))
                {
                    var expandedQuery = query.Replace(synonym.Key, replacement, StringComparison.OrdinalIgnoreCase);
                    if (!string.Equals(expandedQuery, query, StringComparison.OrdinalIgnoreCase))
                    {
                        expanded.Add(new ExpandedQuery
                        {
                            Query = expandedQuery,
                            ExpansionType = "product_synonym",
                            Confidence = 0.85f
                        });
                    }
                }
            }
        }

        // Attribute synonyms
        foreach (var synonym in AttributeSynonyms)
        {
            if (normalizedQuery.Contains(synonym.Key))
            {
                foreach (var replacement in synonym.Value.Take(2))
                {
                    var expandedQuery = query.Replace(synonym.Key, replacement, StringComparison.OrdinalIgnoreCase);
                    if (!string.Equals(expandedQuery, query, StringComparison.OrdinalIgnoreCase))
                    {
                        expanded.Add(new ExpandedQuery
                        {
                            Query = expandedQuery,
                            ExpansionType = "attribute_synonym",
                            Confidence = 0.8f
                        });
                    }
                }
            }
        }

        return expanded;
    }

    private List<ExpandedQuery> ApplyEntityExpansion(string query, List<EntityExtractionResult> entities)
    {
        var expanded = new List<ExpandedQuery>();

        // Brand-based expansion
        var brands = entities.Where(e => e.EntityType == EntityTypes.Brand && e.Confidence >= 0.8f).ToList();
        foreach (var brand in brands)
        {
            // Add brand at the beginning if not already there
            if (!query.StartsWith(brand.Value, StringComparison.OrdinalIgnoreCase))
            {
                expanded.Add(new ExpandedQuery
                {
                    Query = $"{brand.Value} {query}",
                    ExpansionType = "brand_prefix",
                    Confidence = 0.75f
                });
            }
        }

        // Color-based expansion
        var colors = entities.Where(e => e.EntityType == EntityTypes.Color && e.Confidence >= 0.8f).ToList();
        foreach (var color in colors)
        {
            // Create query emphasizing the color
            expanded.Add(new ExpandedQuery
            {
                Query = $"{color.Value} {query}",
                ExpansionType = "color_emphasis",
                Confidence = 0.7f
            });
        }

        return expanded;
    }

    private List<ExpandedQuery> ApplyCategoryExpansion(string query, List<EntityExtractionResult> entities)
    {
        var expanded = new List<ExpandedQuery>();
        var normalizedQuery = query.ToLowerInvariant();

        // Check for category entities
        var categories = entities.Where(e => e.EntityType == EntityTypes.Category && e.Confidence >= 0.8f).ToList();

        foreach (var category in categories)
        {
            var categoryKey = category.NormalizedValue ?? category.Value.ToLowerInvariant();
            
            if (CategoryExpansions.TryGetValue(categoryKey, out var expansions))
            {
                foreach (var expansion in expansions.Take(2))
                {
                    if (!normalizedQuery.Contains(expansion))
                    {
                        expanded.Add(new ExpandedQuery
                        {
                            Query = $"{query} {expansion}",
                            ExpansionType = "category_expansion",
                            Confidence = 0.7f
                        });
                    }
                }
            }
        }

        // Also check if query contains category keywords directly
        foreach (var categoryExpansion in CategoryExpansions)
        {
            if (normalizedQuery.Contains(categoryExpansion.Key))
            {
                foreach (var expansion in categoryExpansion.Value.Take(1))
                {
                    var expandedQuery = $"{query} {expansion}";
                    expanded.Add(new ExpandedQuery
                    {
                        Query = expandedQuery,
                        ExpansionType = "category_related",
                        Confidence = 0.65f
                    });
                }
            }
        }

        return expanded;
    }

    private async Task<List<ExpandedQuery>> ApplyLlmExpansionAsync(
        string query,
        List<EntityExtractionResult> entities,
        CancellationToken cancellationToken)
    {
        var entityContext = entities.Count > 0
            ? string.Join(", ", entities.Select(e => $"{e.EntityType}: {e.Value}"))
            : "none detected";

        var prompt = $@"You are an expert query expander for e-commerce search. Generate 2-3 alternative search queries that capture the same intent but use different words or phrasings.

ORIGINAL QUERY: ""{query}""
DETECTED ENTITIES: {entityContext}

REQUIREMENTS:
- Generate queries that would help find the same products
- Use synonyms, related terms, and alternative phrasings
- Keep the core intent intact
- Include both specific and broader terms
- Queries should be concise (2-6 words each)

OUTPUT FORMAT (JSON array only, no other text):
[""alternative query 1"", ""alternative query 2"", ""alternative query 3""]";

        var request = new GenerationRequest
        {
            Messages = new List<GenerationMessage>
            {
                new GenerationMessage { Role = "user", Content = prompt }
            },
            Temperature = 0.3f,
            MaxTokens = 200,
            Stream = false
        };

        var response = await _llmProvider.GenerateResponseAsync(request, cancellationToken);
        return ParseLlmExpansionResponse(response.Content);
    }

    private List<ExpandedQuery> ParseLlmExpansionResponse(string jsonResponse)
    {
        var expanded = new List<ExpandedQuery>();

        if (string.IsNullOrWhiteSpace(jsonResponse))
        {
            return expanded;
        }

        try
        {
            var json = ExtractJsonArray(jsonResponse);
            
            if (string.IsNullOrEmpty(json))
            {
                _logger.LogDebug("No valid JSON array found in LLM expansion response");
                return expanded;
            }

            var queries = JsonSerializer.Deserialize<List<string>>(json, new JsonSerializerOptions
            {
                AllowTrailingCommas = true
            });

            if (queries != null)
            {
                foreach (var q in queries.Where(q => !string.IsNullOrWhiteSpace(q)).Take(3))
                {
                    expanded.Add(new ExpandedQuery
                    {
                        Query = q.Trim(),
                        ExpansionType = "llm_contextual",
                        Confidence = 0.75f
                    });
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "JSON parsing failed for LLM expansion response: {Response}",
                jsonResponse.Length > 200 ? jsonResponse.Substring(0, 200) + "..." : jsonResponse);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse LLM expansion response");
        }

        return expanded;
    }

    private static string? ExtractJsonArray(string response)
    {
        var jsonStart = response.IndexOf('[');
        if (jsonStart < 0)
        {
            return null;
        }

        var bracketCount = 0;
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

            if (c == '[')
            {
                bracketCount++;
            }
            else if (c == ']')
            {
                bracketCount--;
                if (bracketCount == 0)
                {
                    return response.Substring(jsonStart, i - jsonStart + 1);
                }
            }
        }

        return null;
    }

    private List<ExpandedQuery> DeduplicateExpansions(List<ExpandedQuery> queries)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var deduplicated = new List<ExpandedQuery>();

        foreach (var query in queries.OrderByDescending(q => q.Confidence))
        {
            var normalized = query.Query.Trim().ToLowerInvariant();
            if (!seen.Contains(normalized))
            {
                seen.Add(normalized);
                deduplicated.Add(query);
            }
        }

        return deduplicated;
    }
}
