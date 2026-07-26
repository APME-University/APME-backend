using System;
using Pgvector;
using Volo.Abp.Domain.Entities;

namespace APME.AI;

/// <summary>
/// Vector embedding entity for semantic search in the RAG architecture.
/// Stores product embeddings generated from canonical documents.
/// SRS Reference: AI Chatbot - Vector Storage & Indexing
/// </summary>
public class ProductEmbedding : Entity<Guid>
{
    /// <summary>
    /// Reference to the source product.
    /// </summary>
    public Guid ProductId { get; private set; }

    /// <summary>
    /// Tenant ID for optional tenant-scoped filtering.
    /// Note: Embeddings are stored at host level for platform-wide search,
    /// but TenantId is preserved for tenant-specific queries if needed.
    /// </summary>
    public Guid? TenantId { get; private set; }

    /// <summary>
    /// Shop ID for shop-level filtering.
    /// </summary>
    public Guid ShopId { get; private set; }

    /// <summary>
    /// Chunk index for products with multiple embedding chunks.
    /// Index 0 is the primary/full embedding.
    /// </summary>
    public int ChunkIndex { get; private set; }

    /// <summary>
    /// The source text that was embedded (for debugging and context).
    /// </summary>
    public string ChunkText { get; private set; } = string.Empty;

    /// <summary>
    /// The vector embedding (pgvector type).
    /// Dimension depends on the embedding model (e.g., 768 for gemma2).
    /// </summary>
    public Vector Embedding { get; private set; } = null!;

    /// <summary>
    /// Version of the embedding model used.
    /// Used for tracking and bulk re-indexing when models change.
    /// </summary>
    public int EmbeddingVersion { get; private set; }

    /// <summary>
    /// Name of the embedding model (e.g., "gemma2", "nomic-embed-text").
    /// </summary>
    public string EmbeddingModel { get; private set; } = string.Empty;

    /// <summary>
    /// Timestamp when this embedding was generated.
    /// </summary>
    public DateTime GeneratedAt { get; private set; }

    /// <summary>
    /// JSON payload containing product metadata for quick context retrieval.
    /// Includes: name, price, category, shop name, availability, etc.
    /// </summary>
    public string? PayloadJson { get; private set; }

    /// <summary>
    /// Schema version of the canonical document this embedding was generated from.
    /// Used for idempotency and determining if re-embedding is needed.
    /// </summary>
    public int CanonicalDocumentVersion { get; private set; }

    /// <summary>
    /// Whether this embedding is active and should be included in search.
    /// Set to false when product is unpublished or deleted.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Lifecycle status of this embedding (NotEmbedded, Queued, Processing, Current, Stale, Failed).
    /// </summary>
    public EmbeddingStatus Status { get; set; } = EmbeddingStatus.NotEmbedded;

    /// <summary>
    /// Number of retry attempts for failed embedding generation.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Error message from the last failed embedding attempt.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Timestamp of the last embedding attempt (success or failure).
    /// </summary>
    public DateTime? LastAttemptAt { get; set; }

    /// <summary>
    /// MD5 hash of Product.SearchableText at the time of embedding.
    /// Compared on every product save to detect staleness without re-reading the vector.
    /// </summary>
    public string? SearchableTextHash { get; set; }

    protected ProductEmbedding()
    {
        // Required by EF Core
    }

    public ProductEmbedding(
        Guid id,
        Guid productId,
        Guid? tenantId,
        Guid shopId,
        int chunkIndex,
        string chunkText,
        Vector embedding,
        int embeddingVersion,
        string embeddingModel,
        int canonicalDocumentVersion,
        string? payloadJson = null) : base(id)
    {
        ProductId = productId;
        TenantId = tenantId;
        ShopId = shopId;
        ChunkIndex = chunkIndex;
        ChunkText = chunkText;
        Embedding = embedding;
        EmbeddingVersion = embeddingVersion;
        EmbeddingModel = embeddingModel;
        CanonicalDocumentVersion = canonicalDocumentVersion;
        PayloadJson = payloadJson;
        GeneratedAt = DateTime.UtcNow;
        IsActive = true;
    }

    /// <summary>
    /// Updates the embedding vector (when model changes or content updates).
    /// </summary>
    public void UpdateEmbedding(
        Vector embedding,
        string chunkText,
        int embeddingVersion,
        string embeddingModel,
        int canonicalDocumentVersion,
        string? payloadJson)
    {
        Embedding = embedding;
        ChunkText = chunkText;
        EmbeddingVersion = embeddingVersion;
        EmbeddingModel = embeddingModel;
        CanonicalDocumentVersion = canonicalDocumentVersion;
        PayloadJson = payloadJson;
        GeneratedAt = DateTime.UtcNow;
        Status = EmbeddingStatus.Current;
        LastAttemptAt = DateTime.UtcNow;
        ErrorMessage = null;
    }

    /// <summary>
    /// Marks this embedding as processing (job started).
    /// </summary>
    public void MarkProcessing()
    {
        Status = EmbeddingStatus.Processing;
        LastAttemptAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks this embedding as failed with an error message.
    /// </summary>
    public void MarkFailed(string error, int maxRetries = 3)
    {
        RetryCount++;
        ErrorMessage = error;
        LastAttemptAt = DateTime.UtcNow;
        Status = RetryCount >= maxRetries
            ? EmbeddingStatus.Failed
            : EmbeddingStatus.Queued;
    }

    /// <summary>
    /// Marks this embedding as stale (SearchableText changed after embedding was generated).
    /// </summary>
    public void MarkStale()
    {
        Status = EmbeddingStatus.Stale;
    }

    /// <summary>
    /// Marks this embedding as queued for (re-)generation.
    /// </summary>
    public void MarkQueued()
    {
        Status = EmbeddingStatus.Queued;
    }

    /// <summary>
    /// Computes MD5 hash of text for staleness detection.
    /// </summary>
    public static string ComputeHash(string text)
    {
        var bytes = System.Security.Cryptography.MD5.HashData(
            System.Text.Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes);
    }

    /// <summary>
    /// Activates this embedding for search results.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Deactivates this embedding (product unpublished/deleted).
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Updates the metadata payload.
    /// </summary>
    public void UpdatePayload(string? payloadJson)
    {
        PayloadJson = payloadJson;
    }
}









