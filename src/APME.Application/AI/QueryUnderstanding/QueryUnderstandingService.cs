using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace APME.AI.QueryUnderstanding;

/// <summary>
/// Main query understanding service that orchestrates intent classification,
/// entity recognition, query rewriting, and query expansion.
/// </summary>
public class QueryUnderstandingService : IQueryUnderstandingService, ITransientDependency
{
    private readonly IIntentClassifier _intentClassifier;
    private readonly IEntityRecognizer _entityRecognizer;
    private readonly IQueryRewriter _queryRewriter;
    private readonly IQueryExpander _queryExpander;
    private readonly QueryUnderstandingOptions _options;
    private readonly ILogger<QueryUnderstandingService> _logger;

    // Stop words for keyword extraction
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "the", "is", "are", "was", "were", "be", "been", "being",
        "have", "has", "had", "do", "does", "did", "will", "would", "could",
        "should", "may", "might", "must", "shall", "can", "need", "dare",
        "ought", "used", "to", "of", "in", "for", "on", "with", "at", "by",
        "from", "as", "into", "through", "during", "before", "after", "above",
        "below", "between", "under", "again", "further", "then", "once", "here",
        "there", "when", "where", "why", "how", "all", "each", "few", "more",
        "most", "other", "some", "such", "no", "nor", "not", "only", "own",
        "same", "so", "than", "too", "very", "just", "and", "but", "if", "or",
        "because", "until", "while", "although", "i", "me", "my", "you", "your",
        "we", "our", "they", "their", "it", "its", "this", "that", "these", "those",
        "what", "which", "who", "whom", "whose", "show", "find", "get", "want",
        "looking", "search", "please", "thank", "thanks", "hi", "hello", "hey"
    };

    public QueryUnderstandingService(
        IIntentClassifier intentClassifier,
        IEntityRecognizer entityRecognizer,
        IQueryRewriter queryRewriter,
        IQueryExpander queryExpander,
        IOptions<QueryUnderstandingOptions> options,
        ILogger<QueryUnderstandingService> logger)
    {
        _intentClassifier = intentClassifier;
        _entityRecognizer = entityRecognizer;
        _queryRewriter = queryRewriter;
        _queryExpander = queryExpander;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<QueryAnalysisResult> AnalyzeQueryAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new QueryAnalysisResult
            {
                OriginalQuery = query ?? string.Empty,
                ProcessedQuery = string.Empty,
                Intent = new IntentClassificationResult
                {
                    PrimaryIntent = IntentType.Unknown,
                    PrimaryConfidence = 0f
                },
                Confidence = 0f
            };
        }

        var stopwatch = Stopwatch.StartNew();

        _logger.LogDebug("Starting query analysis for: {Query}", query);

        try
        {
            // Step 1: Intent classification
            var intentResult = await _intentClassifier.ClassifyIntentAsync(query, cancellationToken);
            
            _logger.LogDebug(
                "Intent classification: {Intent} (confidence: {Confidence})",
                intentResult.PrimaryIntent,
                intentResult.PrimaryConfidence);

            // Step 2: Entity recognition
            var entities = await _entityRecognizer.RecognizeEntitiesAsync(query, cancellationToken);
            
            _logger.LogDebug("Entity recognition found {Count} entities", entities.Count);

            // Step 3: Query rewriting (if enabled and needed)
            QueryRewriteResult? rewriteResult = null;
            var processedQuery = query;

            if (_options.EnableQueryRewriting)
            {
                rewriteResult = await _queryRewriter.RewriteQueryAsync(
                    query, intentResult, entities, cancellationToken);
                
                if (rewriteResult.WasRewritten)
                {
                    processedQuery = rewriteResult.RewrittenQuery;
                    _logger.LogDebug(
                        "Query rewritten: '{Original}' -> '{Rewritten}'",
                        query, processedQuery);
                }
            }

            // Step 4: Query expansion (if enabled)
            var expandedQueries = new List<string> { processedQuery };

            if (_options.EnableQueryExpansion)
            {
                var expansionResult = await _queryExpander.ExpandQueryAsync(
                    processedQuery, entities, cancellationToken);
                
                expandedQueries = expansionResult.ExpandedQueries
                    .Where(q => q.Confidence >= _options.MinimumExpansionConfidence)
                    .Select(q => q.Query)
                    .Distinct()
                    .ToList();

                _logger.LogDebug("Query expansion generated {Count} variants", expandedQueries.Count);
            }

            // Step 5: Extract keywords
            var keywords = ExtractKeywords(processedQuery, entities);

            // Step 6: Build suggested filters
            var suggestedFilters = BuildSuggestedFilters(entities);

            // Step 7: Calculate overall confidence
            var overallConfidence = CalculateOverallConfidence(intentResult, entities, rewriteResult);

            stopwatch.Stop();

            var result = new QueryAnalysisResult
            {
                OriginalQuery = query,
                ProcessedQuery = processedQuery,
                Intent = intentResult,
                Entities = entities,
                Rewrite = rewriteResult,
                ExpandedQueries = expandedQueries,
                Keywords = keywords,
                SuggestedFilters = suggestedFilters,
                Confidence = overallConfidence,
                AnalyzedAt = DateTime.UtcNow,
                ProcessingTime = stopwatch.Elapsed
            };

            _logger.LogInformation(
                "Query analysis completed in {Time}ms. Intent: {Intent}, Entities: {EntityCount}, Confidence: {Confidence}",
                stopwatch.ElapsedMilliseconds,
                intentResult.PrimaryIntent,
                entities.Count,
                overallConfidence);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during query analysis for: {Query}", query);

            stopwatch.Stop();

            // Return a fallback result
            return new QueryAnalysisResult
            {
                OriginalQuery = query,
                ProcessedQuery = query,
                Intent = new IntentClassificationResult
                {
                    PrimaryIntent = IntentType.Unknown,
                    PrimaryConfidence = 0.1f,
                    AllIntentScores = new Dictionary<IntentType, float> { { IntentType.Unknown, 0.1f } }
                },
                Entities = new List<EntityExtractionResult>(),
                ExpandedQueries = new List<string> { query },
                Confidence = 0.1f,
                AnalyzedAt = DateTime.UtcNow,
                ProcessingTime = stopwatch.Elapsed
            };
        }
    }

    private List<string> ExtractKeywords(string query, List<EntityExtractionResult> entities)
    {
        // Split query into words
        var words = Regex.Split(query, @"\W+")
            .Where(w => w.Length > 2 && !StopWords.Contains(w))
            .Select(w => w.ToLowerInvariant())
            .Distinct()
            .ToList();

        // Add entity values as keywords with high priority
        var entityKeywords = entities
            .Where(e => e.Confidence >= 0.7f)
            .Select(e => e.NormalizedValue ?? e.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.ToLowerInvariant())
            .Distinct();

        // Merge and prioritize
        var keywords = entityKeywords.Concat(words).Distinct().ToList();

        return keywords;
    }

    private Dictionary<string, object> BuildSuggestedFilters(List<EntityExtractionResult> entities)
    {
        var filters = new Dictionary<string, object>();

        // Category filter
        var category = entities.FirstOrDefault(e => 
            e.EntityType == EntityTypes.Category && e.Confidence >= 0.7f);
        if (category != null)
        {
            filters["category"] = category.NormalizedValue ?? category.Value;
        }

        // Brand filter
        var brand = entities.FirstOrDefault(e => 
            e.EntityType == EntityTypes.Brand && e.Confidence >= 0.8f);
        if (brand != null)
        {
            filters["brand"] = brand.NormalizedValue ?? brand.Value;
        }

        // Color filter
        var color = entities.FirstOrDefault(e => 
            e.EntityType == EntityTypes.Color && e.Confidence >= 0.8f);
        if (color != null)
        {
            filters["color"] = color.NormalizedValue ?? color.Value;
        }

        // Size filter
        var size = entities.FirstOrDefault(e => 
            e.EntityType == EntityTypes.Size && e.Confidence >= 0.7f);
        if (size != null)
        {
            filters["size"] = size.NormalizedValue ?? size.Value;
        }

        // Price filters
        var priceMax = entities.FirstOrDefault(e => e.EntityType == EntityTypes.PriceMax);
        if (priceMax != null && decimal.TryParse(priceMax.NormalizedValue, out var maxPrice))
        {
            filters["maxPrice"] = maxPrice;
        }

        var priceMin = entities.FirstOrDefault(e => e.EntityType == EntityTypes.PriceMin);
        if (priceMin != null && decimal.TryParse(priceMin.NormalizedValue, out var minPrice))
        {
            filters["minPrice"] = minPrice;
        }

        var priceRange = entities.FirstOrDefault(e => e.EntityType == EntityTypes.PriceRange);
        if (priceRange != null && priceRange.Metadata.TryGetValue("min", out var rangeMin) &&
            priceRange.Metadata.TryGetValue("max", out var rangeMax))
        {
            filters["minPrice"] = rangeMin;
            filters["maxPrice"] = rangeMax;
        }

        // Material filter
        var material = entities.FirstOrDefault(e => 
            e.EntityType == EntityTypes.Material && e.Confidence >= 0.7f);
        if (material != null)
        {
            filters["material"] = material.NormalizedValue ?? material.Value;
        }

        // Intent modifier (for sorting/ranking)
        var modifier = entities.FirstOrDefault(e => 
            e.EntityType == EntityTypes.IntentModifier && e.Confidence >= 0.7f);
        if (modifier != null)
        {
            var sortHint = modifier.NormalizedValue switch
            {
                "budget" => "price_asc",
                "premium" => "price_desc",
                "new" => "date_desc",
                "top_rated" => "rating_desc",
                "popular" => "popularity_desc",
                _ => null
            };

            if (sortHint != null)
            {
                filters["sortBy"] = sortHint;
            }
        }

        return filters;
    }

    private float CalculateOverallConfidence(
        IntentClassificationResult intentResult,
        List<EntityExtractionResult> entities,
        QueryRewriteResult? rewriteResult)
    {
        // Weighted average of different confidence factors
        var intentConfidence = intentResult.PrimaryConfidence;
        var entityConfidence = entities.Any() 
            ? entities.Average(e => e.Confidence) 
            : 0.3f;
        var rewriteConfidence = rewriteResult?.Confidence ?? 1.0f;

        // Calculate weighted score
        var weightedScore = 
            (intentConfidence * 0.4f) +
            (entityConfidence * 0.35f) +
            (rewriteConfidence * 0.25f);

        // Bonus for high-confidence entities
        if (entities.Any(e => e.Confidence >= 0.9f))
        {
            weightedScore += 0.1f;
        }

        // Penalty for ambiguous intent
        if (intentResult.SecondaryConfidence.HasValue &&
            intentResult.PrimaryConfidence - intentResult.SecondaryConfidence.Value < 0.2f)
        {
            weightedScore -= 0.15f;
        }

        // Penalty for no entities in product-related intents
        if (!entities.Any() && intentResult.PrimaryIntent is 
            IntentType.Navigational or IntentType.Informational or 
            IntentType.Transactional or IntentType.Comparison)
        {
            weightedScore -= 0.1f;
        }

        return Math.Clamp(weightedScore, 0.1f, 1.0f);
    }
}
