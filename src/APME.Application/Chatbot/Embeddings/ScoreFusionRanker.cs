using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using APME.Chatbot.Embeddings;
using APME.Products;
using Microsoft.Extensions.Logging;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;

namespace APME.Chatbot.Embeddings;

/// <summary>
/// Combines semantic similarity score with a lightweight popularity proxy.
/// Weights: 70% semantic, 30% popularity (IsFeatured + stock availability).
/// </summary>
public class ScoreFusionRanker : ITransientDependency
{
    private readonly IRepository<Product, Guid> _products;
    private readonly IDataFilter _dataFilter;
    private readonly ILogger<ScoreFusionRanker> _logger;

    public ScoreFusionRanker(
        IRepository<Product, Guid> products,
        IDataFilter dataFilter,
        ILogger<ScoreFusionRanker> logger)
    {
        _products = products;
        _dataFilter = dataFilter;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Guid>> RankAsync(
        IReadOnlyList<VectorSearchResult> candidates,
        int topK = 6,
        CancellationToken ct = default)
    {
        if (candidates.Count == 0) return [];

        var ids = candidates.Select(c => c.ProductId).ToList();

        List<Product> products;
        using (_dataFilter.Disable<IMultiTenant>())
        {
            products = await _products.GetListAsync(
                p => ids.Contains(p.Id), cancellationToken: ct);
        }

        var scored = candidates.Select(c =>
        {
            var product = products.FirstOrDefault(p => p.Id == c.ProductId);
            var popularity = ComputePopularityScore(product);

            return new VectorSearchResult
            {
                ProductId = c.ProductId,
                SimilarityScore = c.SimilarityScore,
                FinalScore = 0.7f * c.SimilarityScore + 0.3f * popularity
            };
        });

        return scored
            .OrderByDescending(r => r.FinalScore)
            .Take(topK)
            .Select(r => r.ProductId)
            .ToList();
    }

    private static float ComputePopularityScore(Product? product)
    {
        if (product == null) return 0f;

        float score = 0f;
        if (product.IsFeatured) score += 0.6f;
        if (product.StockStatus == StockStatus.InStock) score += 0.4f;
        return Math.Min(score, 1.0f);
    }
}
