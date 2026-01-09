using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace APME.AI;

/// <summary>
/// Service interface for AI-powered chat with RAG context.
/// SRS Reference: AI Chatbot RAG Architecture - Chat Service
/// </summary>
public interface IAIChatService : IApplicationService
{
    /// <summary>
    /// Processes a chat message and generates an AI response using RAG.
    /// </summary>
    /// <param name="request">The chat request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The chat response with AI-generated content.</returns>
    Task<ChatResponse> ChatAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a streaming response (for real-time chat).
    /// </summary>
    /// <param name="request">The chat request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    IAsyncEnumerable<string> ChatStreamAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Request for AI chat.
/// </summary>
public class ChatRequest
{
    /// <summary>
    /// The user's message/query.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional conversation history for context.
    /// </summary>
    public List<ChatMessage>? ConversationHistory { get; set; }

    /// <summary>
    /// Optional tenant filter for product context.
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Optional shop filter for product context.
    /// </summary>
    public Guid? ShopId { get; set; }

    /// <summary>
    /// Number of relevant products to retrieve for context.
    /// Default: 5
    /// </summary>
    public int ContextProductCount { get; set; } = 5;

    /// <summary>
    /// Session/conversation ID for tracking.
    /// </summary>
    public string? SessionId { get; set; }
}

/// <summary>
/// A message in the conversation.
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// The role (user, assistant, system).
    /// </summary>
    public string Role { get; set; } = "user";

    /// <summary>
    /// The message content.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of the message.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Response from AI chat.
/// </summary>
public class ChatResponse
{
    /// <summary>
    /// The AI-generated response.
    /// </summary>
    public string Response { get; set; } = string.Empty;

    /// <summary>
    /// Products that were used as context.
    /// </summary>
    public List<ProductSearchResult> ContextProducts { get; set; } = new();

    /// <summary>
    /// Token usage information.
    /// </summary>
    public TokenUsage? TokenUsage { get; set; }

    /// <summary>
    /// Response generation time in milliseconds.
    /// </summary>
    public long GenerationTimeMs { get; set; }

    /// <summary>
    /// Session ID for conversation tracking.
    /// </summary>
    public string? SessionId { get; set; }
}

/// <summary>
/// Token usage statistics.
/// </summary>
public class TokenUsage
{
    /// <summary>
    /// Tokens used for the prompt.
    /// </summary>
    public int PromptTokens { get; set; }

    /// <summary>
    /// Tokens generated in the response.
    /// </summary>
    public int CompletionTokens { get; set; }

    /// <summary>
    /// Total tokens used.
    /// </summary>
    public int TotalTokens => PromptTokens + CompletionTokens;
}









