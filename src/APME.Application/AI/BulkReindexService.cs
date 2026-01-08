using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using APME.Events;
using APME.Products;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;

namespace APME.AI;

/// <summary>
/// Service for bulk reindexing product embeddings.
/// Used when embedding model changes or for initial setup.
/// SRS Reference: AI Chatbot RAG Architecture - Operational Concerns
/// </summary>
public class BulkReindexService : IBulkReindexService, ITransientDependency
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IProductEmbeddingRepository _embeddingRepository;
    private readonly IOllamaEmbeddingService _embeddingService;
    private readonly IDistributedEventBus _eventBus;
    private readonly AIOptions _options;
    private readonly IDataFilter _dataFilter;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly ILogger<BulkReindexService> _logger;

    public BulkReindexService(
        IRepository<Product, Guid> productRepository,
        IProductEmbeddingRepository embeddingRepository,
        IOllamaEmbeddingService embeddingService,
        IDistributedEventBus eventBus,
        IOptions<AIOptions> options,
        IDataFilter dataFilter,
        IUnitOfWorkManager unitOfWorkManager,
        ILogger<BulkReindexService> logger)
    {
        _productRepository = productRepository;
        _embeddingRepository = embeddingRepository;
        _embeddingService = embeddingService;
        _eventBus = eventBus;
        _options = options.Value;
        _dataFilter = dataFilter;
        _unitOfWorkManager = unitOfWorkManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<BulkReindexResult> TriggerBulkReindexAsync(
        Guid? tenantId = null,
        Guid? shopId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting bulk reindex (tenant={TenantId}, shop={ShopId})",
            tenantId, shopId);

        var result = new BulkReindexResult
        {
            StartedAt = DateTime.UtcNow
        };

        try
        {
            // Get all active, published products
            using (_dataFilter.Disable<IMultiTenant>())
            {
                var query = await _productRepository.GetQueryableAsync();
                
                query = query.Where(p => p.IsActive && p.IsPublished);

                if (tenantId.HasValue)
                {
                    query = query.Where(p => p.TenantId == tenantId.Value);
                }

                if (shopId.HasValue)
                {
                    query = query.Where(p => p.ShopId == shopId.Value);
                }

                var productIds = query.Select(p => p.Id).ToList();
                result.TotalProducts = productIds.Count;

                _logger.LogInformation(
                    "Found {Count} products for bulk reindex",
                    productIds.Count);

                // Enqueue jobs for each product
                foreach (var productId in productIds)
                {
                    try
                    {
                        BackgroundJob.Enqueue<ProductEmbeddingWorker>(
                            worker => worker.GenerateEmbeddingAsync(productId));
                        result.JobsEnqueued++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Failed to enqueue reindex job for product {ProductId}",
                            productId);
                        result.Errors.Add($"Failed to enqueue job for {productId}: {ex.Message}");
                    }
                }
            }

            result.CompletedAt = DateTime.UtcNow;
            result.Success = result.Errors.Count == 0;

            _logger.LogInformation(
                "Bulk reindex triggered: {Enqueued}/{Total} jobs enqueued in {Duration}ms",
                result.JobsEnqueued, result.TotalProducts, result.DurationMs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk reindex failed");
            result.Success = false;
            result.Errors.Add($"Bulk reindex failed: {ex.Message}");
            result.CompletedAt = DateTime.UtcNow;
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<BulkReindexResult> ReindexOutdatedEmbeddingsAsync(
        int batchSize = 100,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting reindex of outdated embeddings (current version: {Version})",
            _options.EmbeddingModelVersion);

        var result = new BulkReindexResult
        {
            StartedAt = DateTime.UtcNow
        };

        try
        {
            // Find products with outdated embeddings
            var productIds = await _embeddingRepository.GetProductsNeedingEmbeddingAsync(
                _options.EmbeddingModelVersion,
                batchSize,
                cancellationToken);

            result.TotalProducts = productIds.Count;

            _logger.LogInformation(
                "Found {Count} products with outdated embeddings",
                productIds.Count);

            // Enqueue jobs
            foreach (var productId in productIds)
            {
                try
                {
                    BackgroundJob.Enqueue<ProductEmbeddingWorker>(
                        worker => worker.GenerateEmbeddingAsync(productId));
                    result.JobsEnqueued++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to enqueue update job for product {ProductId}",
                        productId);
                    result.Errors.Add($"Failed to enqueue job for {productId}: {ex.Message}");
                }
            }

            result.CompletedAt = DateTime.UtcNow;
            result.Success = result.Errors.Count == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reindex outdated embeddings failed");
            result.Success = false;
            result.Errors.Add(ex.Message);
            result.CompletedAt = DateTime.UtcNow;
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<EmbeddingStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var stats = new EmbeddingStatistics();

        try
        {
            var embeddingsQuery = await _embeddingRepository.GetQueryableAsync();

            stats.TotalEmbeddings = embeddingsQuery.LongCount();
            stats.ActiveEmbeddings = embeddingsQuery.LongCount(e => e.IsActive);
            stats.InactiveEmbeddings = stats.TotalEmbeddings - stats.ActiveEmbeddings;

            // Count unique products
            stats.UniqueProducts = embeddingsQuery
                .Select(e => e.ProductId)
                .Distinct()
                .LongCount();

            // Get model distribution
            stats.EmbeddingsByModel = embeddingsQuery
                .GroupBy(e => e.EmbeddingModel)
                .Select(g => new { Model = g.Key, Count = g.LongCount() })
                .ToDictionary(x => x.Model, x => x.Count);

            // Get version distribution
            stats.EmbeddingsByVersion = embeddingsQuery
                .GroupBy(e => e.EmbeddingVersion)
                .Select(g => new { Version = g.Key, Count = g.LongCount() })
                .ToDictionary(x => x.Version, x => x.Count);

            stats.CurrentModelVersion = _options.EmbeddingModelVersion;
            stats.CurrentModelName = _options.EmbeddingModel;

            // Count outdated
            stats.OutdatedEmbeddings = embeddingsQuery
                .LongCount(e => e.EmbeddingVersion < _options.EmbeddingModelVersion);

            // Products needing embedding (active, published, but no embedding generated)
            using (_dataFilter.Disable<IMultiTenant>())
            {
                var productsQuery = await _productRepository.GetQueryableAsync();
                stats.ProductsNeedingEmbedding = productsQuery
                    .LongCount(p => p.IsActive && p.IsPublished && !p.EmbeddingGenerated);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get embedding statistics");
            throw;
        }

        return stats;
    }

    /// <inheritdoc />
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _embeddingService.TestConnectionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test Ollama connection");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<(long Total, long Active)> GetEmbeddingCountsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var embeddingsQuery = await _embeddingRepository.GetQueryableAsync();
            var total = embeddingsQuery.LongCount();
            var active = embeddingsQuery.LongCount(e => e.IsActive);
            return (total, active);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get embedding counts");
            throw;
        }
    }
}

// IBulkReindexService, BulkReindexResult, and EmbeddingStatistics moved to APME.Application.Contracts/AI/

