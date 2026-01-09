using System.Threading.Tasks;
using APME.AI;
using APME.Events;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace APME.EventHandlers;

/// <summary>
/// Handles ProductChangedEto events and enqueues embedding generation jobs.
/// SRS Reference: AI Chatbot RAG Architecture - Background Embedding Pipeline
/// </summary>
public class ProductChangedEventHandler : 
    IDistributedEventHandler<ProductChangedEto>,
    IDistributedEventHandler<ProductCreatedEto>,
    IDistributedEventHandler<ProductUpdatedEto>,
    IDistributedEventHandler<ProductDeletedEto>,
    ITransientDependency
{
    private readonly AIOptions _options;
    private readonly ILogger<ProductChangedEventHandler> _logger;

    public ProductChangedEventHandler(
        IOptions<AIOptions> options,
        ILogger<ProductChangedEventHandler> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Handles generic product changed events.
    /// </summary>
    public Task HandleEventAsync(ProductChangedEto eventData)
    {
        return ProcessEventAsync(eventData);
    }

    /// <summary>
    /// Handles product created events.
    /// </summary>
    public Task HandleEventAsync(ProductCreatedEto eventData)
    {
        return ProcessEventAsync(eventData);
    }

    /// <summary>
    /// Handles product updated events.
    /// </summary>
    public Task HandleEventAsync(ProductUpdatedEto eventData)
    {
        return ProcessEventAsync(eventData);
    }

    /// <summary>
    /// Handles product deleted events.
    /// </summary>
    public Task HandleEventAsync(ProductDeletedEto eventData)
    {
        return ProcessEventAsync(eventData);
    }

    /// <summary>
    /// Processes product change events and enqueues appropriate background jobs.
    /// </summary>
    private Task ProcessEventAsync(ProductChangedEto eventData)
    {
        if (!_options.EnableEmbeddingGeneration)
        {
            _logger.LogDebug(
                "Embedding generation disabled, ignoring event for product {ProductId}",
                eventData.ProductId);
            return Task.CompletedTask;
        }

        _logger.LogInformation(
            "Processing product change event: {ProductId} ({ProductName}), Type: {ChangeType}, Eligible: {IsEligible}",
            eventData.ProductId,
            eventData.ProductName,
            eventData.ChangeType,
            eventData.IsEligibleForEmbedding);

        switch (eventData.ChangeType)
        {
            case ProductChangeType.Created:
            case ProductChangeType.Updated:
                if (eventData.IsEligibleForEmbedding)
                {
                    // Enqueue embedding generation job
                    BackgroundJob.Enqueue<ProductEmbeddingWorker>(
                        worker => worker.GenerateEmbeddingAsync(eventData.ProductId));
                    
                    _logger.LogDebug(
                        "Enqueued embedding generation job for product {ProductId}",
                        eventData.ProductId);
                }
                else
                {
                    // Product not eligible, deactivate existing embeddings
                    BackgroundJob.Enqueue<ProductEmbeddingWorker>(
                        worker => worker.DeactivateEmbeddingsAsync(eventData.ProductId));
                    
                    _logger.LogDebug(
                        "Enqueued embedding deactivation job for product {ProductId}",
                        eventData.ProductId);
                }
                break;

            case ProductChangeType.Deleted:
                // Delete all embeddings for the product
                BackgroundJob.Enqueue<ProductEmbeddingWorker>(
                    worker => worker.DeleteEmbeddingsAsync(eventData.ProductId));
                
                _logger.LogDebug(
                    "Enqueued embedding deletion job for product {ProductId}",
                    eventData.ProductId);
                break;

            case ProductChangeType.Published:
                // Activate or generate embeddings
                BackgroundJob.Enqueue<ProductEmbeddingWorker>(
                    worker => worker.ActivateEmbeddingsAsync(eventData.ProductId));
                
                _logger.LogDebug(
                    "Enqueued embedding activation job for product {ProductId}",
                    eventData.ProductId);
                break;

            case ProductChangeType.Unpublished:
                // Deactivate embeddings (soft delete)
                BackgroundJob.Enqueue<ProductEmbeddingWorker>(
                    worker => worker.DeactivateEmbeddingsAsync(eventData.ProductId));
                
                _logger.LogDebug(
                    "Enqueued embedding deactivation job for product {ProductId}",
                    eventData.ProductId);
                break;

            case ProductChangeType.BulkReindex:
                // Handle bulk reindex - this is typically triggered by admin
                BackgroundJob.Enqueue<ProductEmbeddingWorker>(
                    worker => worker.GenerateEmbeddingAsync(eventData.ProductId));
                
                _logger.LogDebug(
                    "Enqueued bulk reindex job for product {ProductId}",
                    eventData.ProductId);
                break;

            default:
                _logger.LogWarning(
                    "Unknown product change type: {ChangeType} for product {ProductId}",
                    eventData.ChangeType, eventData.ProductId);
                break;
        }

        return Task.CompletedTask;
    }
}









