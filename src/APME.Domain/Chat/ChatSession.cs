using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace APME.Chat;

/// <summary>
/// Represents a chat session between a customer and the AI assistant.
/// Tracks session lifecycle, status, and metadata.
/// </summary>
public class ChatSession : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// Customer ID who owns this session.
    /// Required - chat requires customer authentication.
    /// </summary>
    public Guid CustomerId { get; protected set; }

    /// <summary>
    /// Session status: Active, Archived, Expired.
    /// </summary>
    public ChatSessionStatus Status { get; protected set; }

    /// <summary>
    /// Timestamp when the session was last active (last message sent/received).
    /// Used for session timeout detection.
    /// </summary>
    public DateTime LastActivityAt { get; protected set; }

    /// <summary>
    /// Optional metadata stored as JSON for extensibility.
    /// Can store client info, user agent, etc.
    /// </summary>
    public string? Metadata { get; protected set; }

    /// <summary>
    /// Title or summary of the conversation (auto-generated or user-provided).
    /// </summary>
    public string? Title { get; protected set; }

    protected ChatSession()
    {
        // Required by EF Core
    }

    public ChatSession(
        Guid id,
        Guid customerId) : base(id)
    {
        CustomerId = customerId;
        Status = ChatSessionStatus.Active;
        LastActivityAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the last activity timestamp.
    /// Called whenever a message is sent or received.
    /// </summary>
    public void UpdateActivity()
    {
        LastActivityAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the session as archived.
    /// Archived sessions are retained but not shown in active list.
    /// </summary>
    public void Archive()
    {
        Status = ChatSessionStatus.Archived;
    }

    /// <summary>
    /// Marks the session as expired (e.g., due to timeout).
    /// </summary>
    public void Expire()
    {
        Status = ChatSessionStatus.Expired;
    }

    /// <summary>
    /// Reactivates an archived or expired session.
    /// </summary>
    public void Reactivate()
    {
        Status = ChatSessionStatus.Active;
        UpdateActivity();
    }

    /// <summary>
    /// Sets the session title.
    /// </summary>
    public void SetTitle(string? title)
    {
        Title = title;
    }

    /// <summary>
    /// Sets optional metadata as JSON.
    /// </summary>
    public void SetMetadata(string? metadata)
    {
        Metadata = metadata;
    }

    /// <summary>
    /// Checks if the session is active.
    /// </summary>
    public bool IsActive()
    {
        return Status == ChatSessionStatus.Active;
    }

    /// <summary>
    /// Checks if the session has expired based on timeout.
    /// </summary>
    public bool IsExpired(TimeSpan timeout)
    {
        if (!IsActive())
        {
            return false;
        }

        return DateTime.UtcNow - LastActivityAt > timeout;
    }
}



