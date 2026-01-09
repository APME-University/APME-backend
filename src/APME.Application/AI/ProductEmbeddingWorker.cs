using System;
using System.Text.Json;
using System.Threading.Tasks;
using APME.Products;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;

namespace APME.AI;

/// <summary>
/// Background worker for generating product embeddings.
/// Triggered by ProductChanged events via Hangfire.
/// SRS Reference: AI Chatbot RAG Architecture - Background Embedding Pipeline
/// </summary>
public class ProductEmbeddingWorker : ITransientDependency
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IProductEmbeddingRepository _embeddingRepository;
    private readonly IOllamaEmbeddingService _embeddingService;
    private readonly IContentChunker _contentChunker;
    private readonly ICanonicalDocumentBuilder _canonicalDocumentBuilder;
    private readonly AIOptions _options;
    private readonly IDataFilter _dataFilter;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly ILogger<ProductEmbeddingWorker> _logger;

    public ProductEmbeddingWorker(
        IRepository<Product, Guid> productRepository,
        IProductEmbeddingRepository embeddingRepository,
        IOllamaEmbeddingService embeddingService,
        IContentChunker contentChunker,
        ICanonicalDocumentBuilder canonicalDocumentBuilder,
        IOptions<AIOptions> options,
        IDataFilter dataFilter,
        IUnitOfWorkManager unitOfWorkManager,
        ILogger<ProductEmbeddingWorker> logger)
    {
        _productRepository = productRepository;
        _embeddingRepository = embeddingRepository;
        _embeddingService = embeddingService;
        _contentChunker = contentChunker;
        _canonicalDocumentBuilder = canonicalDocumentBuilder;
        _options = options.Value;
        _dataFilter = dataFilter;
        _unitOfWorkManager = unitOfWorkManager;
        _logger = logger;
    }

    /// <summary>
    /// Main entry point for Hangfire job.
    /// Generates embeddings for a single product.
    /// </summary>
    /// <param name="productId">The product ID to process.</param>
    [UnitOfWork]
    public virtual async Task GenerateEmbeddingAsync(Guid productId)
    {
        if (!_options.EnableEmbeddingGeneration)
        {
            _logger.LogDebug("Embedding generation disabled, skipping product {ProductId}", productId);
            return;
        }

        _logger.LogInformation("Starting embedding generation for product {ProductId}", productId);

        try
        {
            // Disable tenant filter to access product across tenants
            using (_dataFilter.Disable<IMultiTenant>())
            {
                var product = await _productRepository.FindAsync(productId);

                if (product == null)
                {
                    _logger.LogWarning("Product {ProductId} not found, skipping embedding", productId);
                    return;
                }

                // Skip if product is not eligible for embedding
                if (!product.IsActive || !product.IsPublished)
                {
                    _logger.LogInformation(
                        "Product {ProductId} is not active/published, deactivating embeddings",
                        productId);
                    await DeactivateEmbeddingsAsync(productId);
                    return;
                }

                await GenerateAndStoreEmbeddingsAsync(product);

                // Mark embedding as generated on the product
                product.MarkEmbeddingGenerated();
                await _productRepository.UpdateAsync(product);

                _logger.LogInformation(
                    "Successfully generated embedding for product {ProductId} ({ProductName})",
                    productId, product.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to generate embedding for product {ProductId}",
                productId);
            throw; // Let Hangfire handle retry
        }
    }

    /// <summary>
    /// Deletes all embeddings for a product.
    /// Called when product is deleted.
    /// </summary>
    [UnitOfWork]
    public virtual async Task DeleteEmbeddingsAsync(Guid productId)
    {
        _logger.LogInformation("Deleting embeddings for product {ProductId}", productId);

        try
        {
            await _embeddingRepository.DeleteByProductIdAsync(productId);
            _logger.LogInformation("Deleted embeddings for product {ProductId}", productId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete embeddings for product {ProductId}", productId);
            throw;
        }
    }

    /// <summary>
    /// Deactivates embeddings for a product (soft delete).
    /// Called when product is unpublished.
    /// </summary>
    [UnitOfWork]
    public virtual async Task DeactivateEmbeddingsAsync(Guid productId)
    {
        _logger.LogInformation("Deactivating embeddings for product {ProductId}", productId);

        try
        {
            var embeddings = await _embeddingRepository.GetByProductIdAsync(productId);
            
            foreach (var embedding in embeddings)
            {
                embedding.Deactivate();
            }

            _logger.LogInformation(
                "Deactivated {Count} embeddings for product {ProductId}",
                embeddings.Count, productId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deactivate embeddings for product {ProductId}", productId);
            throw;
        }
    }

    /// <summary>
    /// Activates embeddings for a product.
    /// Called when product is published.
    /// </summary>
    [UnitOfWork]
    public virtual async Task ActivateEmbeddingsAsync(Guid productId)
    {
        _logger.LogInformation("Activating embeddings for product {ProductId}", productId);

        try
        {
            var embeddings = await _embeddingRepository.GetByProductIdAsync(productId);

            if (embeddings.Count == 0)
            {
                // No existing embeddings, generate new ones
                await GenerateEmbeddingAsync(productId);
                return;
            }

            foreach (var embedding in embeddings)
            {
                embedding.Activate();
            }

            _logger.LogInformation(
                "Activated {Count} embeddings for product {ProductId}",
                embeddings.Count, productId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to activate embeddings for product {ProductId}", productId);
            throw;
        }
    }

    /// <summary>
    /// Generates and stores embeddings for a product.
    /// </summary>
    private async Task GenerateAndStoreEmbeddingsAsync(Product product)
    {
        // Get or build canonical document
        var canonicalDocument = product.GetCanonicalDocument();
        
        if (canonicalDocument == null)
        {
            _logger.LogDebug("Building canonical document for product {ProductId}", product.Id);
            canonicalDocument = await _canonicalDocumentBuilder.BuildAsync(product);
            product.UpdateCanonicalDocument(canonicalDocument);
        }

        // Get embedding text from canonical document
        var embeddingText = canonicalDocument.ToEmbeddingText();

        _logger.LogDebug(
            "Embedding text for product {ProductId}: {Length} chars",
            product.Id, embeddingText.Length);

        // Chunk content if needed
        var chunks = _contentChunker.ChunkContent(embeddingText, product.Name);

        _logger.LogDebug(
            "Product {ProductId} split into {ChunkCount} chunks",
            product.Id, chunks.Count);

        // Build payload metadata
        var payload = BuildPayload(canonicalDocument);

        // Generate and store embeddings for each chunk
        foreach (var chunk in chunks)
        {
            var embedding = await _embeddingService.GenerateEmbeddingAsync(chunk.Text);

            var embeddingEntity = new ProductEmbedding(
                Guid.NewGuid(),
                product.Id,
                product.TenantId,
                product.ShopId,
                chunk.Index,
                chunk.Text,
                embedding,
                _embeddingService.ModelVersion,
                _embeddingService.ModelName,
                canonicalDocument.SchemaVersion,
                payload);

            await _embeddingRepository.UpsertAsync(embeddingEntity);

            _logger.LogDebug(
                "Stored embedding chunk {ChunkIndex} for product {ProductId}",
                chunk.Index, product.Id);
        }
    }

    /// <summary>
    /// Builds payload JSON for quick context retrieval.
    /// </summary>
    private string BuildPayload(CanonicalProductDocument document)
    {
        var payload = new
        {
            document.ProductId,
            document.Name,
            document.ShopId,
            document.ShopName,
            document.CategoryName,
            document.Price,
            document.IsInStock,
            document.IsOnSale,
            document.SKU
        };

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }
}









