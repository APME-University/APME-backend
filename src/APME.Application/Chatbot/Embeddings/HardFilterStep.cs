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
/// Applies entity-extracted constraints (category, brand, price, stock)
/// to narrow vector search candidates. Pure in-memory operation after the DB call.
/// </summary>
public class HardFilterStep : ITransientDependency
{
    private readonly IRepository<Product, Guid> _products;
    private readonly IDataFilter _dataFilter;
    private readonly ILogger<HardFilterStep> _logger;

    public HardFilterStep(
        IRepository<Product, Guid> products,
        IDataFilter dataFilter,
        ILogger<HardFilterStep> logger)
    {
        _products = products;
        _dataFilter = dataFilter;
        _logger = logger;
    }

    public async Task<IReadOnlyList<VectorSearchResult>> FilterAsync(
        IReadOnlyList<VectorSearchResult> candidates,
        EmbeddingSearchRequest request,
        CancellationToken ct = default)
    {
        if (candidates.Count == 0) return candidates;

        var ids = candidates.Select(c => c.ProductId).ToList();

        IReadOnlyList<Product> matchingProducts;
        using (_dataFilter.Disable<IMultiTenant>())
        {
            var query = await _products.GetQueryableAsync();
            query = query.Where(p => ids.Contains(p.Id) && p.IsActive);

            if (request.InStockOnly)
                query = query.Where(p => p.StockStatus != StockStatus.OutOfStock &&
                                         p.StockStatus != StockStatus.Discontinued);

            if (request.CategoryId.HasValue)
                query = query.Where(p => p.CategoryId == request.CategoryId.Value);

            if (request.BrandId.HasValue)
                query = query.Where(p => p.BrandId == request.BrandId.Value);

            if (request.MaxPrice.HasValue)
                query = query.Where(p =>
                    (p.SalePrice ?? p.Price) <= request.MaxPrice.Value);

            if (request.MinPrice.HasValue)
                query = query.Where(p =>
                    (p.SalePrice ?? p.Price) >= request.MinPrice.Value);

            matchingProducts = await _products.GetListAsync(
                p => ids.Contains(p.Id) && p.IsActive, cancellationToken: ct);

            // Re-apply filters in memory since GetListAsync doesn't use the IQueryable
            matchingProducts = matchingProducts.Where(p =>
            {
                if (request.InStockOnly &&
                    (p.StockStatus == StockStatus.OutOfStock ||
                     p.StockStatus == StockStatus.Discontinued))
                    return false;
                if (request.CategoryId.HasValue && p.CategoryId != request.CategoryId.Value)
                    return false;
                if (request.BrandId.HasValue && p.BrandId != request.BrandId.Value)
                    return false;
                var effectivePrice = p.SalePrice ?? p.Price;
                if (request.MaxPrice.HasValue && effectivePrice > request.MaxPrice.Value)
                    return false;
                if (request.MinPrice.HasValue && effectivePrice < request.MinPrice.Value)
                    return false;
                return true;
            }).ToList();
        }

        var allowedIds = matchingProducts.Select(p => p.Id).ToHashSet();

        var filtered = candidates
            .Where(c => allowedIds.Contains(c.ProductId))
            .ToList();

        _logger.LogDebug(
            "HardFilterStep: {Before} candidates → {After} after applying constraints",
            candidates.Count, filtered.Count);

        return filtered;
    }
}
