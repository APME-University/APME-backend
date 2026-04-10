namespace APME.AI;

/// <summary>
/// Configuration options for query understanding services.
/// </summary>
public class QueryUnderstandingOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "QueryUnderstanding";

    /// <summary>
    /// Confidence threshold for rule-based classification.
    /// If below this, LLM fallback is triggered.
    /// </summary>
    public float RuleBasedConfidenceThreshold { get; set; } = 0.7f;

    /// <summary>
    /// Minimum confidence for including entities in results.
    /// </summary>
    public float MinimumEntityConfidence { get; set; } = 0.5f;

    /// <summary>
    /// Maximum number of expanded query variants to generate.
    /// </summary>
    public int MaxQueryExpansions { get; set; } = 5;

    /// <summary>
    /// Whether to enable query rewriting for ambiguous queries.
    /// </summary>
    public bool EnableQueryRewriting { get; set; } = true;

    /// <summary>
    /// Whether to enable query expansion.
    /// </summary>
    public bool EnableQueryExpansion { get; set; } = true;

    /// <summary>
    /// Whether to enable LLM fallback when rule-based fails.
    /// </summary>
    public bool EnableLlmFallback { get; set; } = true;

    /// <summary>
    /// Timeout for LLM calls in milliseconds.
    /// </summary>
    public int LlmTimeoutMilliseconds { get; set; } = 2000;

    /// <summary>
    /// Cache duration for query analysis results in minutes.
    /// </summary>
    public int CacheDurationMinutes { get; set; } = 60;

    /// <summary>
    /// Maximum number of LLM calls per query analysis.
    /// </summary>
    public int MaxLlmCallsPerRequest { get; set; } = 3;

    /// <summary>
    /// Confidence threshold below which query rewriting is triggered.
    /// </summary>
    public float RewriteConfidenceThreshold { get; set; } = 0.6f;

    /// <summary>
    /// Minimum word count for queries that don't need rewriting.
    /// </summary>
    public int MinQueryWordsForNoRewrite { get; set; } = 3;

    /// <summary>
    /// Minimum expansion confidence to include in results.
    /// </summary>
    public float MinimumExpansionConfidence { get; set; } = 0.6f;

    /// <summary>
    /// Whether to load categories from database for entity recognition.
    /// </summary>
    public bool LoadCategoriesFromDatabase { get; set; } = true;

    /// <summary>
    /// Cache duration for loaded categories in minutes.
    /// </summary>
    public int CategoryCacheDurationMinutes { get; set; } = 30;
}
