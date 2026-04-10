using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace APME.AI.QueryUnderstanding;

/// <summary>
/// Intent types for query classification.
/// </summary>
public enum IntentType
{
    /// <summary>
    /// Unknown or unclassified intent.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Getting information about products (features, specifications, usage).
    /// </summary>
    Informational = 1,

    /// <summary>
    /// Finding specific products or categories to browse.
    /// </summary>
    Navigational = 2,

    /// <summary>
    /// Purchasing, adding to cart, checkout, pricing.
    /// </summary>
    Transactional = 3,

    /// <summary>
    /// Help, troubleshooting, returns, refunds.
    /// </summary>
    Support = 4,

    /// <summary>
    /// Comparing products or features.
    /// </summary>
    Comparison = 5,

    /// <summary>
    /// Exploring products without specific intent (recommendations).
    /// </summary>
    Discovery = 6,

    /// <summary>
    /// Account-related queries (orders, profile, history).
    /// </summary>
    Account = 7,

    /// <summary>
    /// Greeting or conversation starter.
    /// </summary>
    Greeting = 10,

    /// <summary>
    /// Farewell or conversation ender.
    /// </summary>
    Farewell = 11,

    /// <summary>
    /// Request for human agent.
    /// </summary>
    HumanHandoff = 12
}

/// <summary>
/// Result of intent classification.
/// </summary>
public class IntentClassificationResult
{
    /// <summary>
    /// Primary detected intent.
    /// </summary>
    public IntentType PrimaryIntent { get; set; } = IntentType.Unknown;

    /// <summary>
    /// Confidence score for primary intent (0-1).
    /// </summary>
    public float PrimaryConfidence { get; set; }

    /// <summary>
    /// Secondary intent if detected.
    /// </summary>
    public IntentType? SecondaryIntent { get; set; }

    /// <summary>
    /// Confidence score for secondary intent.
    /// </summary>
    public float? SecondaryConfidence { get; set; }

    /// <summary>
    /// All intent scores for debugging/analysis.
    /// </summary>
    public Dictionary<IntentType, float> AllIntentScores { get; set; } = new();

    /// <summary>
    /// Whether rule-based classification was used.
    /// </summary>
    public bool UsedRuleBased { get; set; }

    /// <summary>
    /// Whether LLM fallback was triggered.
    /// </summary>
    public bool UsedLlmFallback { get; set; }

    /// <summary>
    /// Time taken for classification.
    /// </summary>
    public TimeSpan ProcessingTime { get; set; }
}

/// <summary>
/// Entity types that can be extracted from queries.
/// </summary>
public static class EntityTypes
{
    public const string ProductId = "ProductId";
    public const string SKU = "SKU";
    public const string Category = "Category";
    public const string Brand = "Brand";
    public const string Price = "Price";
    public const string PriceMin = "PriceMin";
    public const string PriceMax = "PriceMax";
    public const string PriceRange = "PriceRange";
    public const string Color = "Color";
    public const string Size = "Size";
    public const string Material = "Material";
    public const string Feature = "Feature";
    public const string IntentModifier = "IntentModifier";
    public const string Quantity = "Quantity";
}

/// <summary>
/// Result of entity extraction.
/// </summary>
public class EntityExtractionResult
{
    /// <summary>
    /// The entity type (from EntityTypes constants).
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// The extracted value as found in the query.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Normalized value for search/filtering.
    /// </summary>
    public string? NormalizedValue { get; set; }

    /// <summary>
    /// Confidence score (0-1).
    /// </summary>
    public float Confidence { get; set; }

    /// <summary>
    /// Start position in the original query.
    /// </summary>
    public int StartPosition { get; set; }

    /// <summary>
    /// End position in the original query.
    /// </summary>
    public int EndPosition { get; set; }

    /// <summary>
    /// Additional metadata about the entity.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Source of extraction (rule, dictionary, llm).
    /// </summary>
    public string Source { get; set; } = "rule";
}

/// <summary>
/// Result of query rewriting.
/// </summary>
public class QueryRewriteResult
{
    /// <summary>
    /// The original query.
    /// </summary>
    public string OriginalQuery { get; set; } = string.Empty;

    /// <summary>
    /// The rewritten query.
    /// </summary>
    public string RewrittenQuery { get; set; } = string.Empty;

    /// <summary>
    /// Whether the query was actually rewritten.
    /// </summary>
    public bool WasRewritten { get; set; }

    /// <summary>
    /// Reason for rewriting (or not).
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Strategy used for rewriting.
    /// </summary>
    public string Strategy { get; set; } = string.Empty;

    /// <summary>
    /// Confidence in the rewrite quality.
    /// </summary>
    public float Confidence { get; set; }

    /// <summary>
    /// Processing time.
    /// </summary>
    public TimeSpan ProcessingTime { get; set; }
}

/// <summary>
/// Result of query expansion.
/// </summary>
public class QueryExpansionResult
{
    /// <summary>
    /// The original/processed query.
    /// </summary>
    public string OriginalQuery { get; set; } = string.Empty;

    /// <summary>
    /// List of expanded query variants.
    /// </summary>
    public List<ExpandedQuery> ExpandedQueries { get; set; } = new();

    /// <summary>
    /// Processing time.
    /// </summary>
    public TimeSpan ProcessingTime { get; set; }
}

/// <summary>
/// An expanded query variant.
/// </summary>
public class ExpandedQuery
{
    /// <summary>
    /// The expanded query text.
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Expansion type (synonym, contextual, category, brand).
    /// </summary>
    public string ExpansionType { get; set; } = string.Empty;

    /// <summary>
    /// Confidence score for this expansion.
    /// </summary>
    public float Confidence { get; set; }
}

/// <summary>
/// Complete result of query analysis.
/// </summary>
public class QueryAnalysisResult
{
    /// <summary>
    /// The original user query.
    /// </summary>
    public string OriginalQuery { get; set; } = string.Empty;

    /// <summary>
    /// The processed query (after rewriting if applied).
    /// </summary>
    public string ProcessedQuery { get; set; } = string.Empty;

    /// <summary>
    /// Intent classification result.
    /// </summary>
    public IntentClassificationResult Intent { get; set; } = new();

    /// <summary>
    /// Extracted entities.
    /// </summary>
    public List<EntityExtractionResult> Entities { get; set; } = new();

    /// <summary>
    /// Query rewrite result.
    /// </summary>
    public QueryRewriteResult? Rewrite { get; set; }

    /// <summary>
    /// Expanded query variants.
    /// </summary>
    public List<string> ExpandedQueries { get; set; } = new();

    /// <summary>
    /// Overall confidence score.
    /// </summary>
    public float Confidence { get; set; }

    /// <summary>
    /// Timestamp of analysis.
    /// </summary>
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Total processing time.
    /// </summary>
    public TimeSpan ProcessingTime { get; set; }

    /// <summary>
    /// Suggested filters based on entities.
    /// </summary>
    public Dictionary<string, object> SuggestedFilters { get; set; } = new();

    /// <summary>
    /// Extracted keywords for search.
    /// </summary>
    public List<string> Keywords { get; set; } = new();

    /// <summary>
    /// Whether the query is ambiguous.
    /// </summary>
    [JsonIgnore]
    public bool IsAmbiguous => Confidence < 0.6f || Entities.Count == 0;

    /// <summary>
    /// Whether the query requires disambiguation.
    /// </summary>
    [JsonIgnore]
    public bool RequiresDisambiguation => IsAmbiguous && 
        Intent.PrimaryIntent != IntentType.Support && 
        Intent.PrimaryIntent != IntentType.Greeting &&
        Intent.PrimaryIntent != IntentType.Farewell;

    /// <summary>
    /// Whether RAG retrieval should be used.
    /// </summary>
    [JsonIgnore]
    public bool ShouldUseRag => Intent.PrimaryIntent switch
    {
        IntentType.Informational => true,
        IntentType.Navigational => true,
        IntentType.Transactional => true,
        IntentType.Comparison => true,
        IntentType.Discovery => true,
        _ => false
    };

    /// <summary>
    /// Whether this query requires human handoff.
    /// </summary>
    [JsonIgnore]
    public bool RequiresHumanHandoff => Intent.PrimaryIntent == IntentType.HumanHandoff;
}
