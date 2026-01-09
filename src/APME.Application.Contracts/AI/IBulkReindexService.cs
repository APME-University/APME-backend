using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace APME.AI;

/// <summary>
/// Interface for bulk reindex operations.
/// SRS Reference: AI Chatbot RAG Architecture - Operational Concerns
/// </summary>
public interface IBulkReindexService : IApplicationService
{
    /// <summary>
    /// Triggers a full reindex of all active products.
    /// </summary>
    Task<BulkReindexResult> TriggerBulkReindexAsync(
        Guid? tenantId = null,
        Guid? shopId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reindexes only products with outdated embeddings.
    /// </summary>
    Task<BulkReindexResult> ReindexOutdatedEmbeddingsAsync(
        int batchSize = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets embedding statistics.
    /// </summary>
    Task<EmbeddingStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests connectivity to the embedding service.
    /// </summary>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of total and active embeddings.
    /// </summary>
    Task<(long Total, long Active)> GetEmbeddingCountsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a bulk reindex operation.
/// </summary>
public class BulkReindexResult
{
    public bool Success { get; set; }
    public int TotalProducts { get; set; }
    public int JobsEnqueued { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long DurationMs => CompletedAt.HasValue 
        ? (long)(CompletedAt.Value - StartedAt).TotalMilliseconds 
        : 0;
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Statistics about product embeddings.
/// </summary>
public class EmbeddingStatistics
{
    public long TotalEmbeddings { get; set; }
    public long ActiveEmbeddings { get; set; }
    public long InactiveEmbeddings { get; set; }
    public long UniqueProducts { get; set; }
    public long OutdatedEmbeddings { get; set; }
    public long ProductsNeedingEmbedding { get; set; }
    public int CurrentModelVersion { get; set; }
    public string CurrentModelName { get; set; } = string.Empty;
    public Dictionary<string, long> EmbeddingsByModel { get; set; } = new();
    public Dictionary<int, long> EmbeddingsByVersion { get; set; } = new();
}









