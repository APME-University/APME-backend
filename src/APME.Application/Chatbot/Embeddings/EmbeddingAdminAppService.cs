using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using APME.AI;
using APME.Products;
using Hangfire;
using Microsoft.Extensions.Logging;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;

namespace APME.Chatbot.Embeddings;

/// <summary>
/// Admin surface: view embedding health, trigger reindex, retry failed.
/// </summary>
public class EmbeddingAdminAppService : ApplicationService
{
    private readonly IRepository<ProductEmbedding, Guid> _embeddings;
    private readonly IRepository<Product, Guid> _products;
    private readonly IBulkReindexService _bulkReindexService;
    private readonly IDataFilter _dataFilter;
    private readonly ILogger<EmbeddingAdminAppService> _logger;

    public EmbeddingAdminAppService(
        IRepository<ProductEmbedding, Guid> embeddings,
        IRepository<Product, Guid> products,
        IBulkReindexService bulkReindexService,
        IDataFilter dataFilter,
        ILogger<EmbeddingAdminAppService> logger)
    {
        _embeddings = embeddings;
        _products = products;
        _bulkReindexService = bulkReindexService;
        _dataFilter = dataFilter;
        _logger = logger;
    }

    public async Task<EmbeddingHealthDto> GetHealthAsync(CancellationToken ct = default)
    {
        int totalActive;
        List<ProductEmbedding> allEmbeddings;
        using (_dataFilter.Disable<IMultiTenant>())
        {
            totalActive = await _products.CountAsync(p => p.IsActive, ct);
            allEmbeddings = await _embeddings.GetListAsync(cancellationToken: ct);
        }

        var embeddedProductIds = allEmbeddings.Select(e => e.ProductId).Distinct().Count();

        return new EmbeddingHealthDto
        {
            TotalActiveProducts = totalActive,
            Current = allEmbeddings.Count(e => e.Status == EmbeddingStatus.Current),
            Queued = allEmbeddings.Count(e => e.Status == EmbeddingStatus.Queued),
            Processing = allEmbeddings.Count(e => e.Status == EmbeddingStatus.Processing),
            Stale = allEmbeddings.Count(e => e.Status == EmbeddingStatus.Stale),
            Failed = allEmbeddings.Count(e => e.Status == EmbeddingStatus.Failed),
            NotEmbedded = totalActive - embeddedProductIds
        };
    }

    public async Task ReindexProductAsync(Guid productId, CancellationToken ct = default)
    {
        _logger.LogInformation("Admin triggered reindex for product {ProductId}", productId);

        // Mark existing embeddings as Queued
        using (_dataFilter.Disable<IMultiTenant>())
        {
            var existing = await _embeddings.GetListAsync(
                e => e.ProductId == productId, cancellationToken: ct);
            foreach (var emb in existing)
            {
                emb.MarkQueued();
            }
        }

        BackgroundJob.Enqueue<ProductEmbeddingWorker>(
            worker => worker.GenerateEmbeddingAsync(productId));
    }

    public async Task ReindexAllAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Admin triggered full reindex");
        await _bulkReindexService.TriggerBulkReindexAsync(cancellationToken: ct);
    }

    public async Task RetryFailedAsync(CancellationToken ct = default)
    {
        List<ProductEmbedding> failed;
        using (_dataFilter.Disable<IMultiTenant>())
        {
            failed = await _embeddings.GetListAsync(
                e => e.Status == EmbeddingStatus.Failed);
        }

        foreach (var emb in failed)
        {
            emb.Status = EmbeddingStatus.Queued;
            emb.RetryCount = 0;
            emb.ErrorMessage = null;

            BackgroundJob.Enqueue<ProductEmbeddingWorker>(
                worker => worker.GenerateEmbeddingAsync(emb.ProductId));
        }

        _logger.LogInformation("Retrying {Count} failed embeddings", failed.Count);
    }
}

public class EmbeddingHealthDto
{
    public int TotalActiveProducts { get; set; }
    public int Current { get; set; }
    public int Queued { get; set; }
    public int Processing { get; set; }
    public int Stale { get; set; }
    public int Failed { get; set; }
    public int NotEmbedded { get; set; }
}
