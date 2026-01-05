using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pgvector;

namespace APME.AI;

/// <summary>
/// Service interface for generating embeddings using Ollama.
/// SRS Reference: AI Chatbot RAG Architecture - Embedding Generation
/// </summary>
public interface IOllamaEmbeddingService
{
    /// <summary>
    /// Generates an embedding vector for the given text.
    /// </summary>
    /// <param name="text">The text to embed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The embedding vector.</returns>
    Task<Vector> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates embedding vectors for multiple texts in batch.
    /// </summary>
    /// <param name="texts">The texts to embed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of embedding vectors in the same order as input texts.</returns>
    Task<List<Vector>> GenerateEmbeddingsAsync(
        IEnumerable<string> texts,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the dimension of embeddings produced by the current model.
    /// </summary>
    int EmbeddingDimension { get; }

    /// <summary>
    /// Gets the current embedding model name.
    /// </summary>
    string ModelName { get; }

    /// <summary>
    /// Gets the current embedding model version for tracking.
    /// </summary>
    int ModelVersion { get; }

    /// <summary>
    /// Tests connectivity to the Ollama server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if connection is successful.</returns>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}



