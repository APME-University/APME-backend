using System;
using System.Threading;
using System.Threading.Tasks;

namespace APME.Chat;

/// <summary>
/// Service interface for chat context management.
/// Handles loading, optimization, and pruning of conversation context.
/// </summary>
public interface IChatContextService
{
    /// <summary>
    /// Loads the session context from the database.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="customerId">The customer ID for validation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The loaded chat context.</returns>
    Task<ChatContext> LoadSessionContextAsync(
        Guid sessionId,
        Guid customerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Optimizes the context for the current query.
    /// Applies temporal weighting and relevance filtering.
    /// </summary>
    /// <param name="context">The current context.</param>
    /// <param name="currentQuery">The current user query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The optimized context.</returns>
    Task<ChatContext> OptimizeContextAsync(
        ChatContext context,
        string currentQuery,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies token budget pruning to the context.
    /// Removes oldest messages to fit within the token budget.
    /// </summary>
    /// <param name="context">The context to prune.</param>
    /// <param name="maxTokens">Maximum token count allowed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The pruned context.</returns>
    Task<ChatContext> ApplyContextPruningAsync(
        ChatContext context,
        int maxTokens,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies temporal weighting to messages in the context.
    /// Recent messages are weighted higher than older ones.
    /// </summary>
    /// <param name="context">The context to weight.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The context with weighted messages.</returns>
    Task<ChatContext> ApplyTemporalWeightingAsync(
        ChatContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Estimates the token count for the given context.
    /// </summary>
    /// <param name="context">The context to estimate.</param>
    /// <returns>Estimated token count.</returns>
    int EstimateTokenCount(ChatContext context);
}
