using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Pgvector;
using Volo.Abp.Domain.Repositories;

namespace APME.AI;

/// <summary>
/// Repository interface for ProductEmbedding with semantic search capabilities.
/// SRS Reference: AI Chatbot - Vector Storage & RAG Integration
/// </summary>
public interface IProductEmbeddingRepository : IRepository<ProductEmbedding, Guid>
{
    /// <summary>
    /// Finds similar products using vector similarity search (cosine distance).
    /// </summary>
    /// <param name="queryEmbedding">The query vector to search against.</param>
    /// <param name="topK">Maximum number of results to return.</param>
    /// <param name="tenantId">Optional tenant filter (null = platform-wide).</param>
    /// <param name="shopId">Optional shop filter.</param>
    /// <param name="activeOnly">Whether to only return active embeddings.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of similar embeddings with similarity scores.</returns>
    Task<List<ProductEmbeddingSearchResult>> SearchSimilarAsync(
        Vector queryEmbedding,
        int topK = 10,
        Guid? tenantId = null,
        Guid? shopId = null,
        bool activeOnly = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all embeddings for a specific product.
    /// </summary>
    Task<List<ProductEmbedding>> GetByProductIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all embeddings for a specific product.
    /// </summary>
    Task DeleteByProductIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets products that need embedding generation or update.
    /// </summary>
    /// <param name="embeddingVersion">Current embedding version to check against.</param>
    /// <param name="batchSize">Maximum number of products to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<List<Guid>> GetProductsNeedingEmbeddingAsync(
        int embeddingVersion,
        int batchSize = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts an embedding (insert or update based on ProductId + ChunkIndex).
    /// </summary>
    Task<ProductEmbedding> UpsertAsync(
        ProductEmbedding embedding,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Search result containing embedding and similarity score.
/// </summary>
public class ProductEmbeddingSearchResult
{
    /// <summary>
    /// The matched embedding.
    /// </summary>
    public ProductEmbedding Embedding { get; set; } = null!;

    /// <summary>
    /// Similarity score (0 = identical, higher = more different for cosine distance).
    /// Normalized to 0-1 range where 1 = most similar.
    /// </summary>
    public double SimilarityScore { get; set; }

    /// <summary>
    /// The product ID for convenience.
    /// </summary>
    public Guid ProductId => Embedding.ProductId;
}








