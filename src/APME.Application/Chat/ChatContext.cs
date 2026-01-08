using System;
using System.Collections.Generic;
using APME.AI;

namespace APME.Chat;

/// <summary>
/// Represents the context for a chat conversation.
/// Contains conversation history, session info, and RAG context.
/// </summary>
public class ChatContext
{
    /// <summary>
    /// Session ID for this conversation.
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// Customer ID who owns this session.
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// Conversation messages (user and assistant).
    /// Ordered by sequence number.
    /// </summary>
    public List<ChatMessageDto> Messages { get; set; } = new();

    /// <summary>
    /// Products retrieved from RAG semantic search for context.
    /// </summary>
    public List<ProductSearchResult> ContextProducts { get; set; } = new();

    /// <summary>
    /// Total token count estimated for the context.
    /// </summary>
    public int EstimatedTokenCount { get; set; }

    /// <summary>
    /// Whether the context has been pruned due to token budget.
    /// </summary>
    public bool WasPruned { get; set; }

    /// <summary>
    /// Number of messages removed during pruning.
    /// </summary>
    public int PrunedMessageCount { get; set; }
}

/// <summary>
/// DTO for a chat message in the context.
/// </summary>
public class ChatMessageDto
{
    public Guid Id { get; set; }
    public int SequenceNumber { get; set; }
    public ChatMessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? Metadata { get; set; }
}




