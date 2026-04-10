using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace APME.AI;

/// <summary>
/// Anti-corruption layer interface for LLM providers.
/// Abstracts the underlying LLM implementation (Ollama, OpenAI, etc.)
/// to enable model swapping and testability.
/// </summary>
public interface ILlmProvider
{
    /// <summary>
    /// Generates a streaming response from the LLM.
    /// </summary>
    /// <param name="request">The generation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async enumerable of tokens.</returns>
    IAsyncEnumerable<string> GenerateStreamingResponseAsync(
        GenerationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a complete (non-streaming) response from the LLM.
    /// </summary>
    /// <param name="request">The generation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The complete response.</returns>
    Task<GenerationResponse> GenerateResponseAsync(
        GenerationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about the current model.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Model information.</returns>
    Task<ModelInfo> GetModelInfoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests connectivity to the LLM provider.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if connected successfully.</returns>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the name of the current generation model.
    /// </summary>
    string ModelName { get; }
}
