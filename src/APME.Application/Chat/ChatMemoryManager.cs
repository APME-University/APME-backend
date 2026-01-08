using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace APME.Chat;

/// <summary>
/// Implementation of chat memory management with sliding window and token budgeting.
/// </summary>
public class ChatMemoryManager : IChatMemoryManager, ITransientDependency
{
    private readonly ChatOptions _options;
    private readonly ILogger<ChatMemoryManager> _logger;

    public ChatMemoryManager(
        IOptions<ChatOptions> options,
        ILogger<ChatMemoryManager> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public List<ChatMessageDto> ApplySlidingWindow(
        List<ChatMessageDto> messages,
        int maxMessages = 20)
    {
        if (messages.Count <= maxMessages)
        {
            return messages;
        }

        _logger.LogDebug(
            "Applying sliding window: {Total} messages -> {Max} messages",
            messages.Count,
            maxMessages);

        // Keep the most recent messages
        return messages
            .OrderByDescending(m => m.SequenceNumber)
            .Take(maxMessages)
            .OrderBy(m => m.SequenceNumber)
            .ToList();
    }

    public PruneResult PruneToTokenBudget(
        List<ChatMessageDto> messages,
        int maxTokens = 4000)
    {
        var result = new PruneResult
        {
            PrunedMessages = new List<ChatMessageDto>(messages)
        };

        // Estimate current token count
        var currentTokens = EstimateTokenCount(result.PrunedMessages);

        if (currentTokens <= maxTokens)
        {
            result.FinalTokenCount = currentTokens;
            return result;
        }

        _logger.LogDebug(
            "Pruning messages: {CurrentTokens} tokens -> {MaxTokens} tokens",
            currentTokens,
            maxTokens);

        // Remove oldest messages until we fit within budget
        // Always keep at least the last message
        var sortedMessages = result.PrunedMessages
            .OrderBy(m => m.SequenceNumber)
            .ToList();

        var keptMessages = new List<ChatMessageDto>();
        var tokenCount = 0;

        // Add messages from newest to oldest until we hit the budget
        for (int i = sortedMessages.Count - 1; i >= 0; i--)
        {
            var message = sortedMessages[i];
            var messageTokens = EstimateTokenCount(message.Content);

            if (tokenCount + messageTokens <= maxTokens || keptMessages.Count == 0)
            {
                keptMessages.Insert(0, message);
                tokenCount += messageTokens;
            }
            else
            {
                break;
            }
        }

        result.PrunedMessages = keptMessages.OrderBy(m => m.SequenceNumber).ToList();
        result.RemovedMessageCount = messages.Count - keptMessages.Count;
        result.FinalTokenCount = EstimateTokenCount(result.PrunedMessages);

        _logger.LogInformation(
            "Pruned {RemovedCount} messages. Final token count: {FinalTokens}",
            result.RemovedMessageCount,
            result.FinalTokenCount);

        return result;
    }

    public int EstimateTokenCount(List<ChatMessageDto> messages)
    {
        return messages.Sum(m => EstimateTokenCount(m.Content));
    }

    public int EstimateTokenCount(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return 0;
        }

        // Simple approximation: ~4 characters per token
        // More accurate would require a tokenizer, but this is sufficient for budgeting
        return (int)Math.Ceiling(content.Length / 4.0);
    }
}




