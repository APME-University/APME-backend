using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using OllamaSharp.Models.Chat;

namespace APME.AI.Adapters;

/// <summary>
/// Adapter for converting OllamaSharp streaming responses to domain models.
/// </summary>
public static class StreamingResponseAdapter
{
    /// <summary>
    /// Aggregates a stream of OllamaSharp ChatResponseStream into a GenerationResponse.
    /// </summary>
    /// <param name="stream">The async enumerable of chat responses.</param>
    /// <param name="modelUsed">The model name used.</param>
    /// <returns>Aggregated GenerationResponse.</returns>
    public static async Task<GenerationResponse> AggregateStreamAsync(
        IAsyncEnumerable<ChatResponseStream?> stream,
        string modelUsed)
    {
        var stopwatch = Stopwatch.StartNew();
        var contentBuilder = new StringBuilder();
        var tokens = new List<string>();

        await foreach (var chunk in stream)
        {
            if (!string.IsNullOrEmpty(chunk?.Message?.Content))
            {
                contentBuilder.Append(chunk.Message.Content);
                tokens.Add(chunk.Message.Content);
            }
        }

        stopwatch.Stop();

        return new GenerationResponse
        {
            Content = contentBuilder.ToString(),
            ModelUsed = modelUsed,
            GenerationTimeMs = stopwatch.ElapsedMilliseconds,
            RequestId = Guid.NewGuid().ToString(),
            FinishReason = "stop",
            TokenUsage = new GenerationTokenUsage
            {
                CompletionTokens = tokens.Count
            }
        };
    }

    /// <summary>
    /// Creates a domain ChatMessage from an OllamaSharp Message.
    /// </summary>
    public static ChatMessage ToDomainMessage(Message ollamaMessage)
    {
        return new ChatMessage
        {
            Role = ollamaMessage.Role.ToString()?.ToLowerInvariant() ?? "user",
            Content = ollamaMessage.Content ?? string.Empty,
            Timestamp = DateTime.UtcNow
        };
    }
}
