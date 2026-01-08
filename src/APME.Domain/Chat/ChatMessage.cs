using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace APME.Chat;

/// <summary>
/// Represents a message in a chat session.
/// Messages are ordered by SequenceNumber for deterministic ordering.
/// </summary>
public class ChatMessage : CreationAuditedEntity<Guid>
{
    /// <summary>
    /// Session ID this message belongs to.
    /// </summary>
    public Guid SessionId { get; protected set; }

    /// <summary>
    /// Sequence number for deterministic ordering within a session.
    /// Starts at 1 and increments for each message in the session.
    /// </summary>
    public int SequenceNumber { get; protected set; }

    /// <summary>
    /// Role of the message sender: "user" or "assistant".
    /// </summary>
    public ChatMessageRole Role { get; protected set; }

    /// <summary>
    /// Message content.
    /// </summary>
    public string Content { get; protected set; }

    /// <summary>
    /// Whether this message has been archived (soft delete).
    /// Messages older than retention period are marked as archived.
    /// </summary>
    public bool IsArchived { get; protected set; }

    /// <summary>
    /// Timestamp when the message was archived.
    /// </summary>
    public DateTime? ArchivedAt { get; protected set; }

    /// <summary>
    /// Optional metadata stored as JSON.
    /// Can store token usage, generation time, product IDs referenced, etc.
    /// </summary>
    public string? Metadata { get; protected set; }

    protected ChatMessage()
    {
        // Required by EF Core
    }

    public ChatMessage(
        Guid id,
        Guid sessionId,
        int sequenceNumber,
        ChatMessageRole role,
        string content) : base(id)
    {
        SessionId = sessionId;
        SequenceNumber = sequenceNumber;
        Role = role;
        Content = content ?? throw new ArgumentNullException(nameof(content));
        IsArchived = false;
    }

    /// <summary>
    /// Marks the message as archived.
    /// </summary>
    public void Archive()
    {
        IsArchived = true;
        ArchivedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets optional metadata as JSON.
    /// </summary>
    public void SetMetadata(string? metadata)
    {
        Metadata = metadata;
    }
}



