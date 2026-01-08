using System.Collections.Generic;

namespace APME.Chat;

/// <summary>
/// Service for managing chat memory and context window.
/// Handles token budgeting, pruning, and sliding window logic.
/// </summary>
public interface IChatMemoryManager
{
    /// <summary>
    /// Applies sliding window to messages, keeping only the most recent N messages.
    /// </summary>
    List<ChatMessageDto> ApplySlidingWindow(
        List<ChatMessageDto> messages,
        int maxMessages = 20);

    /// <summary>
    /// Prunes messages to fit within token budget.
    /// Removes oldest messages first.
    /// </summary>
    PruneResult PruneToTokenBudget(
        List<ChatMessageDto> messages,
        int maxTokens = 4000);

    /// <summary>
    /// Estimates token count for a list of messages.
    /// Uses simple approximation: ~4 characters per token.
    /// </summary>
    int EstimateTokenCount(List<ChatMessageDto> messages);

    /// <summary>
    /// Estimates token count for a single message.
    /// </summary>
    int EstimateTokenCount(string content);
}

/// <summary>
/// Result of pruning operation.
/// </summary>
public class PruneResult
{
    public List<ChatMessageDto> PrunedMessages { get; set; } = new();
    public int RemovedMessageCount { get; set; }
    public int FinalTokenCount { get; set; }
}




