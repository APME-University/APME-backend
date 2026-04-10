using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using APME.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace APME.Chat;

/// <summary>
/// Service for managing chat context with temporal weighting and token budgeting.
/// </summary>
public class ChatContextService : IChatContextService, ITransientDependency
{
    private readonly IChatSessionRepository _sessionRepository;
    private readonly IChatMessageRepository _messageRepository;
    private readonly ChatOptions _options;
    private readonly ILogger<ChatContextService> _logger;

    private const int CharsPerToken = 4;

    public ChatContextService(
        IChatSessionRepository sessionRepository,
        IChatMessageRepository messageRepository,
        IOptions<ChatOptions> options,
        ILogger<ChatContextService> logger)
    {
        _sessionRepository = sessionRepository;
        _messageRepository = messageRepository;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ChatContext> LoadSessionContextAsync(
        Guid sessionId,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepository.GetByCustomerAsync(
            sessionId,
            customerId,
            cancellationToken);

        if (session == null)
        {
            throw new InvalidOperationException(
                $"Session {sessionId} not found or does not belong to customer {customerId}");
        }

        var messages = await _messageRepository.GetRecentMessagesAsync(
            sessionId,
            _options.MaxContextMessages,
            cancellationToken);

        var context = new ChatContext
        {
            SessionId = sessionId,
            CustomerId = customerId,
            Messages = messages.Select(m => new ChatMessageDto
            {
                Id = m.Id,
                SequenceNumber = m.SequenceNumber,
                Role = m.Role,
                Content = m.Content,
                CreatedAt = m.CreationTime,
                Metadata = m.Metadata
            }).ToList()
        };

        context.EstimatedTokenCount = EstimateTokenCount(context);

        _logger.LogDebug(
            "Loaded context for session {SessionId}: {MessageCount} messages, ~{TokenCount} tokens",
            sessionId,
            context.Messages.Count,
            context.EstimatedTokenCount);

        return context;
    }

    /// <inheritdoc />
    public async Task<ChatContext> OptimizeContextAsync(
        ChatContext context,
        string currentQuery,
        CancellationToken cancellationToken = default)
    {
        var optimizedContext = await ApplyTemporalWeightingAsync(context, cancellationToken);
        optimizedContext = await ApplyContextPruningAsync(
            optimizedContext,
            _options.MaxContextTokens,
            cancellationToken);

        _logger.LogDebug(
            "Optimized context: {OriginalMessages} -> {OptimizedMessages} messages, " +
            "{OriginalTokens} -> {OptimizedTokens} tokens",
            context.Messages.Count,
            optimizedContext.Messages.Count,
            context.EstimatedTokenCount,
            optimizedContext.EstimatedTokenCount);

        return optimizedContext;
    }

    /// <inheritdoc />
    public Task<ChatContext> ApplyContextPruningAsync(
        ChatContext context,
        int maxTokens,
        CancellationToken cancellationToken = default)
    {
        if (context.EstimatedTokenCount <= maxTokens)
        {
            return Task.FromResult(context);
        }

        var prunedMessages = new List<ChatMessageDto>();
        var currentTokenCount = 0;
        var removedCount = 0;

        var orderedMessages = context.Messages
            .OrderByDescending(m => m.SequenceNumber)
            .ToList();

        foreach (var message in orderedMessages)
        {
            var messageTokens = EstimateMessageTokenCount(message.Content);

            if (currentTokenCount + messageTokens <= maxTokens)
            {
                prunedMessages.Add(message);
                currentTokenCount += messageTokens;
            }
            else
            {
                removedCount++;
            }
        }

        prunedMessages = prunedMessages
            .OrderBy(m => m.SequenceNumber)
            .ToList();

        var prunedContext = new ChatContext
        {
            SessionId = context.SessionId,
            CustomerId = context.CustomerId,
            Messages = prunedMessages,
            ContextProducts = context.ContextProducts,
            EstimatedTokenCount = currentTokenCount,
            WasPruned = removedCount > 0,
            PrunedMessageCount = removedCount
        };

        if (removedCount > 0)
        {
            _logger.LogDebug(
                "Pruned {RemovedCount} messages from context to fit token budget",
                removedCount);
        }

        return Task.FromResult(prunedContext);
    }

    /// <inheritdoc />
    public Task<ChatContext> ApplyTemporalWeightingAsync(
        ChatContext context,
        CancellationToken cancellationToken = default)
    {
        if (context.Messages.Count == 0)
        {
            return Task.FromResult(context);
        }

        var now = DateTime.UtcNow;
        var weightedMessages = new List<(ChatMessageDto Message, double Weight)>();

        foreach (var message in context.Messages)
        {
            var timeDiffMinutes = (now - message.CreatedAt).TotalMinutes;
            var recencyWeight = Math.Max(0.1, 1.0 - (timeDiffMinutes / 60.0));

            var roleWeight = message.Role switch
            {
                ChatMessageRole.User => 1.2,
                ChatMessageRole.Assistant => 0.9,
                _ => 1.0
            };

            var sequenceWeight = 1.0;
            var maxSequence = context.Messages.Max(m => m.SequenceNumber);
            if (maxSequence > 0)
            {
                sequenceWeight = 0.5 + (0.5 * message.SequenceNumber / maxSequence);
            }

            var totalWeight = recencyWeight * roleWeight * sequenceWeight;
            weightedMessages.Add((message, totalWeight));
        }

        var sortedMessages = weightedMessages
            .OrderBy(m => m.Message.SequenceNumber)
            .Select(m => m.Message)
            .ToList();

        var weightedContext = new ChatContext
        {
            SessionId = context.SessionId,
            CustomerId = context.CustomerId,
            Messages = sortedMessages,
            ContextProducts = context.ContextProducts,
            EstimatedTokenCount = context.EstimatedTokenCount,
            WasPruned = context.WasPruned,
            PrunedMessageCount = context.PrunedMessageCount
        };

        return Task.FromResult(weightedContext);
    }

    /// <inheritdoc />
    public int EstimateTokenCount(ChatContext context)
    {
        return context.Messages.Sum(m => EstimateMessageTokenCount(m.Content));
    }

    private int EstimateMessageTokenCount(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return 0;
        }

        return (content.Length / CharsPerToken) + 4;
    }
}
