using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using APME.Categories;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Caching;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;

namespace APME.AI.QueryUnderstanding;

/// <summary>
/// String extensions for common operations
/// </summary>
internal static class StringExtensions
{
    /// <summary>
    /// Truncates a string to a specified maximum length and adds an ellipsis if truncated
    /// </summary>
    public static string TruncateWithEllipsis(this string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        return value.Substring(0, maxLength - 3) + "...";
    }
}

/// <summary>
/// Hybrid entity recognizer using dictionary lookups, regex patterns, and LLM fallback.
/// </summary>
public class HybridEntityRecognizer : IEntityRecognizer, ITransientDependency
{
    private readonly IRepository<Category, Guid> _categoryRepository;
    private readonly ILlmProvider _llmProvider;
    private readonly IDistributedCache<CategoryCacheItem> _categoryCache;
    private readonly IDataFilter _dataFilter;
    private readonly QueryUnderstandingOptions _options;
    private readonly ILogger<HybridEntityRecognizer> _logger;

    private const string CategoryCacheKey = "QueryUnderstanding:Categories";

    // Hardcoded common brands (extensible)
    private static readonly Dictionary<string, string> BrandKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        // Electronics
        { "apple", "Apple" }, { "samsung", "Samsung" }, { "sony", "Sony" },
        { "lg", "LG" }, { "dell", "Dell" }, { "hp", "HP" }, { "lenovo", "Lenovo" },
        { "asus", "ASUS" }, { "acer", "Acer" }, { "microsoft", "Microsoft" },
        { "google", "Google" }, { "oneplus", "OnePlus" }, { "xiaomi", "Xiaomi" },
        { "huawei", "Huawei" }, { "oppo", "OPPO" }, { "vivo", "Vivo" },
        { "realme", "Realme" }, { "motorola", "Motorola" }, { "nokia", "Nokia" },
        { "bose", "Bose" }, { "jbl", "JBL" }, { "beats", "Beats" },
        { "sennheiser", "Sennheiser" }, { "logitech", "Logitech" }, { "razer", "Razer" },
        
        // Fashion
        { "nike", "Nike" }, { "adidas", "Adidas" }, { "puma", "Puma" },
        { "reebok", "Reebok" }, { "new balance", "New Balance" }, { "under armour", "Under Armour" },
        { "levis", "Levi's" }, { "zara", "Zara" }, { "h&m", "H&M" },
        { "gap", "GAP" }, { "uniqlo", "Uniqlo" }, { "gucci", "Gucci" },
        { "louis vuitton", "Louis Vuitton" }, { "prada", "Prada" }, { "versace", "Versace" },
        
        // Home & Appliances
        { "dyson", "Dyson" }, { "philips", "Philips" }, { "bosch", "Bosch" },
        { "whirlpool", "Whirlpool" }, { "electrolux", "Electrolux" }, { "kitchenaid", "KitchenAid" },
        { "ikea", "IKEA" }, { "ninja", "Ninja" }, { "instant pot", "Instant Pot" }
    };

    // Color keywords
    private static readonly string[] ColorKeywords =
    {
        "red", "blue", "green", "black", "white", "yellow", "pink", "purple",
        "orange", "brown", "gray", "grey", "silver", "gold", "beige", "navy",
        "teal", "maroon", "coral", "cyan", "magenta", "turquoise", "ivory",
        "olive", "burgundy", "rose", "indigo", "violet", "lavender"
    };

    // Size patterns
    private static readonly string[] SizeKeywords =
    {
        "xs", "extra small", "small", "medium", "large", "xl", "extra large",
        "xxl", "2xl", "xxxl", "3xl", "4xl", "5xl", "one size", "free size"
    };

    // Material keywords
    private static readonly string[] MaterialKeywords =
    {
        "cotton", "polyester", "leather", "silk", "wool", "linen", "denim",
        "nylon", "velvet", "suede", "satin", "cashmere", "fleece", "canvas",
        "metal", "plastic", "wood", "glass", "ceramic", "stainless steel",
        "aluminum", "rubber", "bamboo", "organic"
    };

    // Intent modifiers
    private static readonly Dictionary<string, string> IntentModifiers = new(StringComparer.OrdinalIgnoreCase)
    {
        { "cheap", "budget" }, { "affordable", "budget" }, { "budget", "budget" },
        { "inexpensive", "budget" }, { "low cost", "budget" }, { "economical", "budget" },
        { "expensive", "premium" }, { "premium", "premium" }, { "luxury", "premium" },
        { "high-end", "premium" }, { "high end", "premium" }, { "designer", "premium" },
        { "latest", "new" }, { "newest", "new" }, { "new", "new" },
        { "recent", "new" }, { "2024", "new" }, { "2025", "new" }, { "2026", "new" },
        { "best", "top_rated" }, { "top rated", "top_rated" }, { "highest rated", "top_rated" },
        { "popular", "popular" }, { "trending", "popular" }, { "best selling", "popular" },
        { "bestseller", "popular" }, { "hot", "popular" }
    };

    public HybridEntityRecognizer(
        IRepository<Category, Guid> categoryRepository,
        ILlmProvider llmProvider,
        IDistributedCache<CategoryCacheItem> categoryCache,
        IDataFilter dataFilter,
        IOptions<QueryUnderstandingOptions> options,
        ILogger<HybridEntityRecognizer> logger)
    {
        _categoryRepository = categoryRepository;
        _llmProvider = llmProvider;
        _categoryCache = categoryCache;
        _dataFilter = dataFilter;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<List<EntityExtractionResult>> RecognizeEntitiesAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new List<EntityExtractionResult>();
        }

        var stopwatch = Stopwatch.StartNew();
        var normalizedQuery = query.ToLowerInvariant().Trim();
        var entities = new List<EntityExtractionResult>();

        // Step 1: Extract product IDs/SKUs (regex patterns)
        entities.AddRange(ExtractProductIds(normalizedQuery));

        // Step 2: Extract categories (from database)
        if (_options.LoadCategoriesFromDatabase)
        {
            var categoryEntities = await ExtractCategoriesAsync(normalizedQuery, cancellationToken);
            entities.AddRange(categoryEntities);
        }

        // Step 3: Extract brands (dictionary lookup)
        entities.AddRange(ExtractBrands(normalizedQuery));

        // Step 4: Extract prices (regex patterns)
        entities.AddRange(ExtractPrices(normalizedQuery));

        // Step 5: Extract colors (keyword lookup)
        entities.AddRange(ExtractColors(normalizedQuery));

        // Step 6: Extract sizes (keyword/regex)
        entities.AddRange(ExtractSizes(normalizedQuery));

        // Step 7: Extract materials (keyword lookup)
        entities.AddRange(ExtractMaterials(normalizedQuery));

        // Step 8: Extract intent modifiers
        entities.AddRange(ExtractIntentModifiers(normalizedQuery));

        // Step 9: Extract quantities
        entities.AddRange(ExtractQuantities(normalizedQuery));

        // Step 10: LLM fallback for complex entities (if needed)
        if (_options.EnableLlmFallback && entities.Count == 0)
        {
            try
            {
                var llmEntities = await ExtractEntitiesWithLlmAsync(normalizedQuery, cancellationToken);
                entities.AddRange(llmEntities);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LLM entity extraction failed");
            }
        }

        // Deduplicate and filter
        entities = DeduplicateEntities(entities);
        entities = FilterLowConfidenceEntities(entities);

        stopwatch.Stop();
        _logger.LogDebug(
            "Entity recognition completed in {Time}ms. Found {Count} entities",
            stopwatch.ElapsedMilliseconds,
            entities.Count);

        return entities;
    }

    private List<EntityExtractionResult> ExtractProductIds(string query)
    {
        var entities = new List<EntityExtractionResult>();

        // Pattern: SKU-123, PROD-456, ABC123
        var skuPatterns = new[]
        {
            new Regex(@"\b([A-Z]{2,5}[-_]?\d{3,10})\b", RegexOptions.IgnoreCase),
            new Regex(@"\b(sku|product|item|id)[:\s#]*([A-Z0-9-]{3,20})\b", RegexOptions.IgnoreCase),
            new Regex(@"\b(prod|pid|sku)[:\s]*(\d+)\b", RegexOptions.IgnoreCase)
        };

        foreach (var pattern in skuPatterns)
        {
            var matches = pattern.Matches(query);
            foreach (Match match in matches)
            {
                var value = match.Groups.Count > 2 ? match.Groups[2].Value : match.Groups[1].Value;
                if (value.Length >= 3)
                {
                    entities.Add(new EntityExtractionResult
                    {
                        EntityType = EntityTypes.ProductId,
                        Value = value,
                        NormalizedValue = value.ToUpperInvariant(),
                        Confidence = 0.85f,
                        StartPosition = match.Index,
                        EndPosition = match.Index + match.Length,
                        Source = "regex",
                        Metadata = new Dictionary<string, object> { { "pattern", pattern.ToString() } }
                    });
                }
            }
        }

        return entities;
    }

    private async Task<List<EntityExtractionResult>> ExtractCategoriesAsync(
        string query,
        CancellationToken cancellationToken)
    {
        var entities = new List<EntityExtractionResult>();

        var categoryKeywords = await GetCategoryKeywordsAsync(cancellationToken);

        foreach (var category in categoryKeywords)
        {
            var lowerCategory = category.Key;
            var index = query.IndexOf(lowerCategory, StringComparison.OrdinalIgnoreCase);

            if (index >= 0)
            {
                entities.Add(new EntityExtractionResult
                {
                    EntityType = EntityTypes.Category,
                    Value = category.Value.Name,
                    NormalizedValue = category.Value.Slug,
                    Confidence = 0.9f,
                    StartPosition = index,
                    EndPosition = index + lowerCategory.Length,
                    Source = "database",
                    Metadata = new Dictionary<string, object>
                    {
                        { "category_id", category.Value.Id.ToString() }
                    }
                });
            }
        }

        return entities;
    }

    private async Task<Dictionary<string, CategoryInfo>> GetCategoryKeywordsAsync(
        CancellationToken cancellationToken)
    {
        var cached = await _categoryCache.GetAsync(CategoryCacheKey);
        if (cached != null)
        {
            return cached.Categories;
        }

        Dictionary<string, CategoryInfo> categoryKeywords;

        using (_dataFilter.Disable<IMultiTenant>())
        {
            var categories = await _categoryRepository.GetListAsync(
                c => c.IsActive,
                cancellationToken: cancellationToken);

            categoryKeywords = categories
                .DistinctBy(c => c.Name.ToLowerInvariant())
                .ToDictionary(
                    c => c.Name.ToLowerInvariant(),
                    c => new CategoryInfo
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Slug = c.Slug
                    });

            // Also add slugs as keywords
            foreach (var category in categories.Where(c => !string.IsNullOrEmpty(c.Slug)))
            {
                var slugKey = category.Slug.ToLowerInvariant().Replace("-", " ");
                if (!categoryKeywords.ContainsKey(slugKey))
                {
                    categoryKeywords[slugKey] = new CategoryInfo
                    {
                        Id = category.Id,
                        Name = category.Name,
                        Slug = category.Slug
                    };
                }
            }
        }

        await _categoryCache.SetAsync(
            CategoryCacheKey,
            new CategoryCacheItem { Categories = categoryKeywords },
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.CategoryCacheDurationMinutes)
            });

        return categoryKeywords;
    }

    private List<EntityExtractionResult> ExtractBrands(string query)
    {
        var entities = new List<EntityExtractionResult>();

        foreach (var brand in BrandKeywords)
        {
            var index = query.IndexOf(brand.Key, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                entities.Add(new EntityExtractionResult
                {
                    EntityType = EntityTypes.Brand,
                    Value = brand.Value,
                    NormalizedValue = brand.Value.ToLowerInvariant(),
                    Confidence = 0.9f,
                    StartPosition = index,
                    EndPosition = index + brand.Key.Length,
                    Source = "dictionary"
                });
            }
        }

        return entities;
    }

    private List<EntityExtractionResult> ExtractPrices(string query)
    {
        var entities = new List<EntityExtractionResult>();

        // Exact price: $50, 50 dollars, 50 usd
        var exactPricePattern = new Regex(@"\$\s*(\d+(?:\.\d{1,2})?)|(\d+(?:\.\d{1,2})?)\s*(dollars?|usd|bucks)", RegexOptions.IgnoreCase);
        var exactMatches = exactPricePattern.Matches(query);
        foreach (Match match in exactMatches)
        {
            var priceValue = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
            if (decimal.TryParse(priceValue, out var price))
            {
                entities.Add(new EntityExtractionResult
                {
                    EntityType = EntityTypes.Price,
                    Value = match.Value,
                    NormalizedValue = price.ToString("F2"),
                    Confidence = 0.9f,
                    StartPosition = match.Index,
                    EndPosition = match.Index + match.Length,
                    Source = "regex",
                    Metadata = new Dictionary<string, object> { { "currency", "USD" } }
                });
            }
        }

        // Price max: under $100, below 50, less than $200
        var maxPricePattern = new Regex(@"(?:under|below|less than|up to|max|maximum)\s*\$?\s*(\d+(?:\.\d{1,2})?)", RegexOptions.IgnoreCase);
        var maxMatches = maxPricePattern.Matches(query);
        foreach (Match match in maxMatches)
        {
            if (decimal.TryParse(match.Groups[1].Value, out var maxPrice))
            {
                entities.Add(new EntityExtractionResult
                {
                    EntityType = EntityTypes.PriceMax,
                    Value = match.Value,
                    NormalizedValue = maxPrice.ToString("F2"),
                    Confidence = 0.85f,
                    StartPosition = match.Index,
                    EndPosition = match.Index + match.Length,
                    Source = "regex"
                });
            }
        }

        // Price min: over $50, above 100, more than $200, at least $100
        var minPricePattern = new Regex(@"(?:over|above|more than|at least|min|minimum)\s*\$?\s*(\d+(?:\.\d{1,2})?)", RegexOptions.IgnoreCase);
        var minMatches = minPricePattern.Matches(query);
        foreach (Match match in minMatches)
        {
            if (decimal.TryParse(match.Groups[1].Value, out var minPrice))
            {
                entities.Add(new EntityExtractionResult
                {
                    EntityType = EntityTypes.PriceMin,
                    Value = match.Value,
                    NormalizedValue = minPrice.ToString("F2"),
                    Confidence = 0.85f,
                    StartPosition = match.Index,
                    EndPosition = match.Index + match.Length,
                    Source = "regex"
                });
            }
        }

        // Price range: $50-$100, between $50 and $100
        var rangePattern = new Regex(@"\$?\s*(\d+)\s*(?:-|to)\s*\$?\s*(\d+)|between\s*\$?\s*(\d+)\s*and\s*\$?\s*(\d+)", RegexOptions.IgnoreCase);
        var rangeMatches = rangePattern.Matches(query);
        foreach (Match match in rangeMatches)
        {
            var minStr = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[3].Value;
            var maxStr = match.Groups[2].Success ? match.Groups[2].Value : match.Groups[4].Value;
            
            if (decimal.TryParse(minStr, out var rangeMin) && decimal.TryParse(maxStr, out var rangeMax))
            {
                entities.Add(new EntityExtractionResult
                {
                    EntityType = EntityTypes.PriceRange,
                    Value = match.Value,
                    NormalizedValue = $"{rangeMin:F2}-{rangeMax:F2}",
                    Confidence = 0.85f,
                    StartPosition = match.Index,
                    EndPosition = match.Index + match.Length,
                    Source = "regex",
                    Metadata = new Dictionary<string, object>
                    {
                        { "min", rangeMin },
                        { "max", rangeMax }
                    }
                });
            }
        }

        return entities;
    }

    private List<EntityExtractionResult> ExtractColors(string query)
    {
        var entities = new List<EntityExtractionResult>();

        foreach (var color in ColorKeywords)
        {
            var pattern = new Regex($@"\b{Regex.Escape(color)}\b", RegexOptions.IgnoreCase);
            var match = pattern.Match(query);
            if (match.Success)
            {
                entities.Add(new EntityExtractionResult
                {
                    EntityType = EntityTypes.Color,
                    Value = color,
                    NormalizedValue = color.ToLowerInvariant(),
                    Confidence = 0.85f,
                    StartPosition = match.Index,
                    EndPosition = match.Index + match.Length,
                    Source = "dictionary"
                });
            }
        }

        return entities;
    }

    private List<EntityExtractionResult> ExtractSizes(string query)
    {
        var entities = new List<EntityExtractionResult>();

        // Size keywords
        foreach (var size in SizeKeywords)
        {
            var pattern = new Regex($@"\b{Regex.Escape(size)}\b", RegexOptions.IgnoreCase);
            var match = pattern.Match(query);
            if (match.Success)
            {
                entities.Add(new EntityExtractionResult
                {
                    EntityType = EntityTypes.Size,
                    Value = size,
                    NormalizedValue = NormalizeSize(size),
                    Confidence = 0.8f,
                    StartPosition = match.Index,
                    EndPosition = match.Index + match.Length,
                    Source = "dictionary"
                });
            }
        }

        // Numeric sizes with 'size' keyword: size 10, size 42
        var numericSizePattern = new Regex(@"\bsize\s*:?\s*(\d+(?:\.\d)?)\b", RegexOptions.IgnoreCase);
        var matches = numericSizePattern.Matches(query);
        foreach (Match match in matches)
        {
            entities.Add(new EntityExtractionResult
            {
                EntityType = EntityTypes.Size,
                Value = match.Value,
                NormalizedValue = match.Groups[1].Value,
                Confidence = 0.85f,
                StartPosition = match.Index,
                EndPosition = match.Index + match.Length,
                Source = "regex"
            });
        }

        return entities;
    }

    private string NormalizeSize(string size)
    {
        return size.ToUpperInvariant() switch
        {
            "EXTRA SMALL" => "XS",
            "SMALL" => "S",
            "MEDIUM" => "M",
            "LARGE" => "L",
            "EXTRA LARGE" => "XL",
            "2XL" => "XXL",
            "3XL" => "XXXL",
            _ => size.ToUpperInvariant()
        };
    }

    private List<EntityExtractionResult> ExtractMaterials(string query)
    {
        var entities = new List<EntityExtractionResult>();

        foreach (var material in MaterialKeywords)
        {
            var pattern = new Regex($@"\b{Regex.Escape(material)}\b", RegexOptions.IgnoreCase);
            var match = pattern.Match(query);
            if (match.Success)
            {
                entities.Add(new EntityExtractionResult
                {
                    EntityType = EntityTypes.Material,
                    Value = material,
                    NormalizedValue = material.ToLowerInvariant(),
                    Confidence = 0.8f,
                    StartPosition = match.Index,
                    EndPosition = match.Index + match.Length,
                    Source = "dictionary"
                });
            }
        }

        return entities;
    }

    private List<EntityExtractionResult> ExtractIntentModifiers(string query)
    {
        var entities = new List<EntityExtractionResult>();

        foreach (var modifier in IntentModifiers)
        {
            var index = query.IndexOf(modifier.Key, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                entities.Add(new EntityExtractionResult
                {
                    EntityType = EntityTypes.IntentModifier,
                    Value = modifier.Key,
                    NormalizedValue = modifier.Value,
                    Confidence = 0.75f,
                    StartPosition = index,
                    EndPosition = index + modifier.Key.Length,
                    Source = "dictionary"
                });
            }
        }

        return entities;
    }

    private List<EntityExtractionResult> ExtractQuantities(string query)
    {
        var entities = new List<EntityExtractionResult>();

        var quantityPattern = new Regex(@"\b(\d+)\s*(?:pieces?|pcs?|items?|units?|pack|set)\b", RegexOptions.IgnoreCase);
        var matches = quantityPattern.Matches(query);
        foreach (Match match in matches)
        {
            if (int.TryParse(match.Groups[1].Value, out var quantity) && quantity > 0 && quantity < 1000)
            {
                entities.Add(new EntityExtractionResult
                {
                    EntityType = EntityTypes.Quantity,
                    Value = match.Value,
                    NormalizedValue = quantity.ToString(),
                    Confidence = 0.8f,
                    StartPosition = match.Index,
                    EndPosition = match.Index + match.Length,
                    Source = "regex"
                });
            }
        }

        return entities;
    }

    private async Task<List<EntityExtractionResult>> ExtractEntitiesWithLlmAsync(
        string query,
        CancellationToken cancellationToken)
    {
        var prompt = $@"You are an expert entity extraction system for an e-commerce platform. Extract product-related entities from the user query.

USER QUERY: ""{query}""

ENTITY TYPES TO EXTRACT:
- ProductId: Specific product identifiers (SKU, ID, model numbers)
- Category: Product categories (Electronics, Clothing, Home, etc.)
- Brand: Brand names
- Price: Price values or ranges
- Color: Color mentions
- Size: Size mentions
- Material: Material mentions
- Feature: Specific product features
- IntentModifier: Words that modify intent (cheap, premium, latest, best)

CRITICAL OUTPUT REQUIREMENTS:
1. Respond ONLY with a JSON array
2. Do NOT include any explanatory text, markdown formatting, or code blocks
3. Do NOT start with words like ""Here is"", ""The result is"", etc.
4. The response must start directly with [ and end with ]
5. If no entities found, return exactly: []

OUTPUT FORMAT:
[{{""entity_type"": ""Category"", ""value"": ""Electronics"", ""confidence"": 0.95}}]

EXAMPLES:
- Input: ""Show me cheap Samsung phones in blue""
- Output: [{{""entity_type"": ""Brand"", ""value"": ""Samsung"", ""confidence"": 0.9}}, {{""entity_type"": ""Color"", ""value"": ""blue"", ""confidence"": 0.8}}, {{""entity_type"": ""IntentModifier"", ""value"": ""cheap"", ""confidence"": 0.7}}]

- Input: ""laptops under 500 dollars""
- Output: [{{""entity_type"": ""Category"", ""value"": ""laptops"", ""confidence"": 0.8}}, {{""entity_type"": ""Price"", ""value"": ""under 500"", ""confidence"": 0.9}}]";

        var request = new GenerationRequest
        {
            Messages = new List<GenerationMessage>
            {
                new GenerationMessage { Role = "user", Content = prompt }
            },
            Temperature = 0.1f,
            MaxTokens = 300,
            Stream = false
        };

        var response = await _llmProvider.GenerateResponseAsync(request, cancellationToken);
        return ParseLlmEntityResponse(response.Content);
    }

    private List<EntityExtractionResult> ParseLlmEntityResponse(string jsonResponse)
    {
        var entities = new List<EntityExtractionResult>();

        if (string.IsNullOrWhiteSpace(jsonResponse))
        {
            _logger.LogDebug("LLM response is null or empty");
            return entities;
        }

        // Log the raw response for debugging (in development only)
        _logger.LogDebug("Raw LLM response: {Response}", jsonResponse.TruncateWithEllipsis(200));

        try
        {
            var json = ExtractJsonArray(jsonResponse);
            
            if (string.IsNullOrEmpty(json))
            {
                _logger.LogDebug("No valid JSON array found in LLM response");
                // Try to extract entities from plain text as fallback
                return ExtractEntitiesFromPlainText(jsonResponse);
            }

            // Validate JSON starts with valid character
            var trimmedJson = json.Trim();
            if (!IsValidJsonStart(trimmedJson))
            {
                _logger.LogWarning("Extracted JSON doesn't start with valid character: {JsonStart}", 
                    trimmedJson.Length > 0 ? trimmedJson[0].ToString() : "empty");
                return ExtractEntitiesFromPlainText(jsonResponse);
            }

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
            };

            // First, try to parse as array of objects
            try
            {
                var parsed = JsonSerializer.Deserialize<List<LlmEntityResult>>(trimmedJson, jsonOptions);

                if (parsed != null)
                {
                    foreach (var entity in parsed.Where(e => !string.IsNullOrEmpty(e.EntityType) && !string.IsNullOrEmpty(e.Value)))
                    {
                        entities.Add(new EntityExtractionResult
                        {
                            EntityType = entity.EntityType,
                            Value = entity.Value,
                            NormalizedValue = entity.Value?.ToLowerInvariant(),
                            Confidence = entity.Confidence > 0 ? entity.Confidence : 0.5f,
                            Source = "llm"
                        });
                    }
                    _logger.LogDebug("Successfully parsed {Count} entities from LLM response", entities.Count);
                    return entities;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogDebug(ex, "LLM returned non-object array, trying string array parsing");
            }

            // Fallback: try to parse as simple string array
            try
            {
                var stringArray = JsonSerializer.Deserialize<List<string>>(trimmedJson, jsonOptions);
                if (stringArray != null)
                {
                    foreach (var value in stringArray.Where(v => !string.IsNullOrWhiteSpace(v)))
                    {
                        // Try to infer entity type from the value
                        var entityType = InferEntityType(value.Trim());
                        entities.Add(new EntityExtractionResult
                        {
                            EntityType = entityType,
                            Value = value.Trim(),
                            NormalizedValue = value.Trim().ToLowerInvariant(),
                            Confidence = 0.5f,
                            Source = "llm"
                        });
                    }
                    _logger.LogDebug("Successfully parsed {Count} entities from string array", entities.Count);
                    return entities;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse LLM response as JSON array. Response: {Response}", 
                    jsonResponse.TruncateWithEllipsis(100));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error parsing LLM response");
            }

            // Final fallback: try to extract entities from plain text
            return ExtractEntitiesFromPlainText(jsonResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error in ParseLlmEntityResponse");
            return new List<EntityExtractionResult>();
        }
    }

    private static string InferEntityType(string value)
    {
        var lowerValue = value.ToLowerInvariant();

        // Check if it's a color
        if (ColorKeywords.Any(c => lowerValue.Equals(c, StringComparison.OrdinalIgnoreCase)))
        {
            return EntityTypes.Color;
        }

        // Check if it's a size
        if (SizeKeywords.Any(s => lowerValue.Contains(s, StringComparison.OrdinalIgnoreCase)))
        {
            return EntityTypes.Size;
        }

        // Check if it's a brand
        if (BrandKeywords.Keys.Any(b => lowerValue.Equals(b, StringComparison.OrdinalIgnoreCase)))
        {
            return EntityTypes.Brand;
        }

        // Check if it's a material
        if (MaterialKeywords.Any(m => lowerValue.Equals(m, StringComparison.OrdinalIgnoreCase)))
        {
            return EntityTypes.Material;
        }

        // Check if it looks like a price
        if (lowerValue.Contains("$") || lowerValue.Contains("dollar") || 
            decimal.TryParse(lowerValue.Replace("$", ""), out _))
        {
            return EntityTypes.Price;
        }

        // Default to feature
        return EntityTypes.Feature;
    }

    private static string? ExtractJsonArray(string response)
    {
        if (string.IsNullOrEmpty(response))
            return null;

        // First, try to find JSON array markers
        var jsonStart = response.IndexOf('[');
        if (jsonStart < 0)
        {
            // Try to find JSON object markers (in case LLM returns single object)
            jsonStart = response.IndexOf('{');
            if (jsonStart >= 0)
            {
                // Wrap single object in array
                var jsonEnd = FindJsonEnd(response, jsonStart);
                if (jsonEnd > jsonStart)
                {
                    var jsonObject = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                    return $"[{jsonObject}]";
                }
            }
            return null;
        }

        // Look for the matching closing bracket
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
                    var extractedJson = response.Substring(jsonStart, i - jsonStart + 1);
                    
                    // Validate the extracted JSON
                    if (IsValidJsonStructure(extractedJson))
                    {
                        return extractedJson;
                    }
                    
                    return null;
                }
            }
        }

        return null;
    }

    private static int FindJsonEnd(string response, int startIndex)
    {
        var braceCount = 0;
        var inString = false;
        var escapeNext = false;

        for (var i = startIndex; i < response.Length; i++)
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
                    return i;
                }
            }
        }

        return -1;
    }

    private static bool IsValidJsonStructure(string json)
    {
        if (string.IsNullOrEmpty(json))
            return false;

        var trimmed = json.Trim();
        
        // Basic structural validation
        if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
        {
            // Check for balanced brackets
            var bracketCount = 0;
            var inString = false;
            var escapeNext = false;

            for (int i = 0; i < trimmed.Length; i++)
            {
                var c = trimmed[i];

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
                    continue;

                if (c == '[')
                    bracketCount++;
                else if (c == ']')
                    bracketCount--;
            }

            return bracketCount == 0;
        }

        return false;
    }

    private List<EntityExtractionResult> DeduplicateEntities(List<EntityExtractionResult> entities)
    {
        var deduplicated = new List<EntityExtractionResult>();
        var seen = new HashSet<string>();

        foreach (var entity in entities.OrderByDescending(e => e.Confidence))
        {
            var key = $"{entity.EntityType}_{entity.NormalizedValue ?? entity.Value}".ToLowerInvariant();
            if (!seen.Contains(key))
            {
                seen.Add(key);
                deduplicated.Add(entity);
            }
        }

        return deduplicated;
    }

    private List<EntityExtractionResult> FilterLowConfidenceEntities(List<EntityExtractionResult> entities)
    {
        return entities.Where(e => e.Confidence >= _options.MinimumEntityConfidence).ToList();
    }

    private static bool IsValidJsonStart(string json)
    {
        if (string.IsNullOrEmpty(json))
            return false;

        var trimmed = json.Trim();
        return trimmed.StartsWith('[') || trimmed.StartsWith('{') || trimmed.StartsWith('"');
    }

    private List<EntityExtractionResult> ExtractEntitiesFromPlainText(string response)
    {
        var entities = new List<EntityExtractionResult>();
        
        if (string.IsNullOrWhiteSpace(response))
            return entities;

        _logger.LogDebug("Attempting to extract entities from plain text response");
        
        // Try to find common patterns in plain text
        var patterns = new[]
        {
            // Pattern: "Category: Electronics"
            @"(\w+):\s*([^\r\n,]+)",
            // Pattern: "Electronics (Category)"
            @"([^\r\n,]+)\s*\(\s*(\w+)\s*\)",
            // Pattern: Just words that might be entities
            @"\b[A-Z][a-zA-Z]+\b"
        };

        foreach (var pattern in patterns)
        {
            try
            {
                var matches = System.Text.RegularExpressions.Regex.Matches(response, pattern);
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    if (match.Groups.Count > 2)
                    {
                        // First pattern: type:value
                        var entityType = NormalizeEntityType(match.Groups[1].Value);
                        var value = match.Groups[2].Value.Trim();
                        
                        if (!string.IsNullOrEmpty(entityType) && !string.IsNullOrEmpty(value))
                        {
                            entities.Add(new EntityExtractionResult
                            {
                                EntityType = entityType,
                                Value = value,
                                NormalizedValue = value.ToLowerInvariant(),
                                Confidence = 0.3f, // Lower confidence for plain text extraction
                                Source = "plaintext_fallback"
                            });
                        }
                    }
                    else if (match.Groups.Count > 1)
                    {
                        // Single word pattern - try to infer type
                        var value = match.Groups[1].Value.Trim();
                        var entityType = InferEntityType(value);
                        
                        entities.Add(new EntityExtractionResult
                        {
                            EntityType = entityType,
                            Value = value,
                            NormalizedValue = value.ToLowerInvariant(),
                            Confidence = 0.2f, // Even lower confidence for inferred entities
                            Source = "plaintext_inferred"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error applying regex pattern for entity extraction");
            }
        }

        _logger.LogDebug("Extracted {Count} entities from plain text", entities.Count);
        return entities.DistinctBy(e => new { e.EntityType, e.Value }).ToList();
    }

    private static string NormalizeEntityType(string entityType)
    {
        if (string.IsNullOrEmpty(entityType))
            return EntityTypes.Feature;

        var normalized = entityType.ToLowerInvariant().Trim();
        
        return normalized switch
        {
            "category" or "categories" or "cat" => EntityTypes.Category,
            "brand" or "brands" => EntityTypes.Brand,
            "product" or "products" or "id" or "productid" => EntityTypes.ProductId,
            "price" or "prices" or "cost" or "pricing" => EntityTypes.Price,
            "color" or "colors" => EntityTypes.Color,
            "size" or "sizes" => EntityTypes.Size,
            "material" or "materials" => EntityTypes.Material,
            "feature" or "features" => EntityTypes.Feature,
            "intent" or "intentmodifier" or "modifier" => EntityTypes.IntentModifier,
            _ => EntityTypes.Feature
        };
    }

    private class LlmEntityResult
    {
        public string EntityType { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public float Confidence { get; set; }
    }
}

/// <summary>
/// Cache item for category keywords.
/// </summary>
public class CategoryCacheItem
{
    public Dictionary<string, CategoryInfo> Categories { get; set; } = new();
}

/// <summary>
/// Category information for entity recognition.
/// </summary>
public class CategoryInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}
