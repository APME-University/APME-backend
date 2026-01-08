using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace APME.Chat;

/// <summary>
/// Service for building chat context from session history.
/// </summary>
public interface IChatContextBuilder
{
    /// <summary>
    /// Loads the chat context for a session, including recent messages.
    /// </summary>
    Task<ChatContext> LoadContextAsync(
        Guid sessionId,
        Guid customerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds a prompt string from the chat context for LLM consumption.
    /// Applies sliding window and token budgeting.
    /// </summary>
    string BuildPrompt(
        ChatContext context,
        string currentMessage,
        int maxTokens = 4000);

    /// <summary>
    /// Converts chat context messages to the format expected by AIChatService.
    /// </summary>
    List<AI.ChatMessage> ToChatMessages(ChatContext context);
}




