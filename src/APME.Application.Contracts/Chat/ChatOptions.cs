namespace APME.Chat;

/// <summary>
/// Configuration options for the chat system.
/// </summary>
public class ChatOptions
{
    public const string SectionName = "Chat";

    /// <summary>
    /// Maximum number of messages to keep in context.
    /// Default: 20
    /// </summary>
    public int MaxContextMessages { get; set; } = 20;

    /// <summary>
    /// Maximum token count for context window.
    /// Default: 4000
    /// </summary>
    public int MaxContextTokens { get; set; } = 4000;

    /// <summary>
    /// Number of days to retain messages before archiving.
    /// Default: 30
    /// </summary>
    public int MessageRetentionDays { get; set; } = 30;

    /// <summary>
    /// Session timeout in minutes.
    /// Sessions inactive for this duration are considered expired.
    /// Default: 30
    /// </summary>
    public int SessionTimeoutMinutes { get; set; } = 30;

    /// <summary>
    /// Rate limit: maximum messages per minute per customer.
    /// Default: 20
    /// </summary>
    public int RateLimitMessagesPerMinute { get; set; } = 20;

    /// <summary>
    /// Number of products to retrieve from RAG for context.
    /// Default: 5
    /// </summary>
    public int ContextProductCount { get; set; } = 5;

    /// <summary>
    /// Confidence threshold for primary RAG response.
    /// Responses below this threshold trigger fallback strategies.
    /// Default: 0.6
    /// </summary>
    public float PrimaryConfidenceThreshold { get; set; } = 0.6f;

    /// <summary>
    /// Confidence threshold for expanded search response.
    /// Responses below this threshold trigger rule-based fallback.
    /// Default: 0.4
    /// </summary>
    public float ExpandedConfidenceThreshold { get; set; } = 0.4f;

    /// <summary>
    /// Minimum relevance score for RAG retrieval results.
    /// Documents below this score are filtered out.
    /// Default: 0.45
    /// </summary>
    public float MinRelevanceThreshold { get; set; } = 0.45f;

    /// <summary>
    /// Whether to enable query understanding service.
    /// Default: true
    /// </summary>
    public bool EnableQueryUnderstanding { get; set; } = true;

    /// <summary>
    /// Whether to enable fallback strategies.
    /// Default: true
    /// </summary>
    public bool EnableFallbackStrategies { get; set; } = true;
}




