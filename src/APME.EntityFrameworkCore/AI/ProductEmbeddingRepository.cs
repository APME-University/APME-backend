using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using APME.AI;
using APME.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using Volo.Abp.Domain.Repositories.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;

namespace APME.EntityFrameworkCore.AI;

/// <summary>
/// EF Core implementation of IProductEmbeddingRepository with pgvector support.
/// SRS Reference: AI Chatbot - Vector Storage & RAG Integration
/// </summary>
public class ProductEmbeddingRepository : EfCoreRepository<APMEDbContext, ProductEmbedding, Guid>, IProductEmbeddingRepository
{
    public ProductEmbeddingRepository(IDbContextProvider<APMEDbContext> dbContextProvider)
        : base(dbContextProvider)
    {
    }

    /// <inheritdoc />
    public async Task<List<ProductEmbeddingSearchResult>> SearchSimilarAsync(
        Vector queryEmbedding,
        int topK = 10,
        Guid? tenantId = null,
        Guid? shopId = null,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync();

        var query = dbContext.ProductEmbeddings.AsQueryable();

        // Apply filters
        if (activeOnly)
        {
            query = query.Where(e => e.IsActive);
        }

        if (tenantId.HasValue)
        {
            query = query.Where(e => e.TenantId == tenantId.Value);
        }

        if (shopId.HasValue)
        {
            query = query.Where(e => e.ShopId == shopId.Value);
        }

        // Execute vector similarity search using pgvector cosine distance
        // Order by distance (ascending) - smaller distance = more similar
        var results = await query
            .OrderBy(e => e.Embedding.CosineDistance(queryEmbedding))
            .Take(topK)
            .Select(e => new
            {
                Embedding = e,
                Distance = e.Embedding.CosineDistance(queryEmbedding)
            })
            .ToListAsync(cancellationToken);

        // Convert to search results with normalized similarity score
        // Cosine distance is 0-2 (0 = identical, 2 = opposite)
        // Convert to similarity: 1 - (distance / 2) gives 0-1 range
        return results.Select(r => new ProductEmbeddingSearchResult
        {
            Embedding = r.Embedding,
            SimilarityScore = 1.0 - (r.Distance / 2.0)
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<List<ProductEmbedding>> GetByProductIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync();

        return await dbContext.ProductEmbeddings
            .Where(e => e.ProductId == productId)
            .OrderBy(e => e.ChunkIndex)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteByProductIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync();

        await dbContext.ProductEmbeddings
            .Where(e => e.ProductId == productId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<Guid>> GetProductsNeedingEmbeddingAsync(
        int embeddingVersion,
        int batchSize = 100,
        CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync();

        // Find products that have embeddings with old version
        var productsWithOldEmbeddings = await dbContext.ProductEmbeddings
            .Where(e => e.EmbeddingVersion < embeddingVersion)
            .Select(e => e.ProductId)
            .Distinct()
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        return productsWithOldEmbeddings;
    }

    /// <inheritdoc />
    public async Task<ProductEmbedding> UpsertAsync(
        ProductEmbedding embedding,
        CancellationToken cancellationToken = default)
    {
        var dbContext = await GetDbContextAsync();

        var existing = await dbContext.ProductEmbeddings
            .FirstOrDefaultAsync(
                e => e.ProductId == embedding.ProductId && e.ChunkIndex == embedding.ChunkIndex,
                cancellationToken);

        if (existing != null)
        {
            // Update existing embedding
            existing.UpdateEmbedding(
                embedding.Embedding,
                embedding.ChunkText,
                embedding.EmbeddingVersion,
                embedding.EmbeddingModel,
                embedding.CanonicalDocumentVersion,
                embedding.PayloadJson);

            await dbContext.SaveChangesAsync(cancellationToken);
            return existing;
        }
        else
        {
            // Insert new embedding
            await dbContext.ProductEmbeddings.AddAsync(embedding, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            return embedding;
        }
    }
}



