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
}




