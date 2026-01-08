using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace APME.Chat;

/// <summary>
/// Repository interface for ChatMessage entity.
/// </summary>
public interface IChatMessageRepository : IRepository<ChatMessage, Guid>
{
    /// <summary>
    /// Gets all messages for a session, ordered by sequence number.
    /// </summary>
    Task<List<ChatMessage>> GetBySessionAsync(
        Guid sessionId,
        bool includeArchived = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent messages for a session (last N messages).
    /// </summary>
    Task<List<ChatMessage>> GetRecentMessagesAsync(
        Guid sessionId,
        int count,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets messages within a date range for a session.
    /// Used for loading context within retention period.
    /// </summary>
    Task<List<ChatMessage>> GetMessagesInRangeAsync(
        Guid sessionId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the next sequence number for a session.
    /// </summary>
    Task<int> GetNextSequenceNumberAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of messages in a session.
    /// </summary>
    Task<int> GetMessageCountAsync(
        Guid sessionId,
        bool includeArchived = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets messages that need to be archived (older than retention period).
    /// </summary>
    Task<List<ChatMessage>> GetMessagesToArchiveAsync(
        DateTime archiveBeforeDate,
        int maxCount = 1000,
        CancellationToken cancellationToken = default);
}




