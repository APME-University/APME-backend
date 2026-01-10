using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using APME.AI;
using Volo.Abp.Application.Services;

namespace APME.Chat;

/// <summary>
/// Orchestrator service for chat operations.
/// Coordinates RAG retrieval, context building, LLM generation, and persistence.
/// </summary>
public interface IChatOrchestratorService : IApplicationService
{
    /// <summary>
    /// Processes a chat message and streams the response.
    /// This is the main entry point for chat operations.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="customerId">Customer ID (validated)</param>
    /// <param name="message">User message</param>
    /// <param name="onToken">Callback for each token streamed</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the complete assistant message and context products</returns>
    Task<ProcessMessageResult> ProcessMessageAsync(
        Guid sessionId,
        Guid customerId,
        string message,
        Func<string, Task> onToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or gets an active session for a customer.
    /// </summary>
    Task<ChatSessionDto> GetOrCreateSessionAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a session by ID, validating it belongs to the customer.
    /// </summary>
    Task<ChatSessionDto?> GetSessionAsync(
        Guid sessionId,
        Guid customerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent messages for a session.
    /// </summary>
    Task<List<ChatMessageResponseDto>> GetRecentMessagesAsync(
        Guid sessionId,
        Guid customerId,
        int count = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Archives a session, marking it as archived.
    /// Archived sessions are retained but not shown in active list.
    /// </summary>
    Task ArchiveSessionAsync(
        Guid sessionId,
        Guid customerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all sessions for a customer (including archived), ordered by last activity.
    /// </summary>
    Task<List<ChatSessionDto>> GetAllSessionsAsync(
        Guid customerId,
        int skipCount = 0,
        int maxResultCount = 50,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of processing a chat message.
/// </summary>
public class ProcessMessageResult
{
    /// <summary>
    /// The complete assistant response text.
    /// </summary>
    public string Response { get; set; } = string.Empty;

    /// <summary>
    /// The enriched context products used for the response.
    /// </summary>
    public List<ProductSearchResult> ContextProducts { get; set; } = new();
}




