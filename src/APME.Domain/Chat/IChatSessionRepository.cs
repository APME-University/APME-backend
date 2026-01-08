using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace APME.Chat;

/// <summary>
/// Repository interface for ChatSession aggregate.
/// </summary>
public interface IChatSessionRepository : IRepository<ChatSession, Guid>
{
    /// <summary>
    /// Gets the active session for a customer, or creates a new one if none exists.
    /// </summary>
    Task<ChatSession> GetOrCreateActiveSessionAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a session by ID with validation that it belongs to the customer.
    /// </summary>
    Task<ChatSession?> GetByCustomerAsync(
        Guid sessionId,
        Guid customerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active sessions for a customer.
    /// </summary>
    Task<List<ChatSession>> GetActiveSessionsAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all sessions (including archived) for a customer, ordered by last activity.
    /// </summary>
    Task<List<ChatSession>> GetAllSessionsAsync(
        Guid customerId,
        int skipCount = 0,
        int maxResultCount = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a customer has an active session.
    /// </summary>
    Task<bool> HasActiveSessionAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);
}




