using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using APME.AI;
using APME.Categories;
using APME.Chatbot.Embeddings;
using APME.Products;
using Microsoft.Extensions.Logging;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;

namespace APME.Chatbot.Embeddings;

/// <summary>
/// Orchestrator — the ONLY entry point for ProductSearchHandler.
/// Sequence: embed query → vector search → hard filter → score fusion → fallback.
/// </summary>
public class HybridSearchService : IHybridSearchService, ITransientDependency
{
    private readonly IOllamaEmbeddingService _embeddingService;
    private readonly IProductEmbeddingRepository _embeddingRepo;
    private readonly HardFilterStep _hardFilter;
    private readonly ScoreFusionRanker _ranker;
    private readonly IRepository<Product, Guid> _products;
    private readonly IRepository<Category, Guid> _categories;
    private readonly IDataFilter _dataFilter;
    private readonly ILogger<HybridSearchService> _logger;

    private const int MinResultsBeforeFallback = 3;
    private const int BroadRecallK = 25;
    private const int FinalReturnK = 6;

    public HybridSearchService(
        IOllamaEmbeddingService embeddingService,
        IProductEmbeddingRepository embeddingRepo,
        HardFilterStep hardFilter,
        ScoreFusionRanker ranker,
        IRepository<Product, Guid> products,
        IRepository<Category, Guid> categories,
        IDataFilter dataFilter,
        ILogger<HybridSearchService> logger)
    {
        _embeddingService = embeddingService;
        _embeddingRepo = embeddingRepo;
        _hardFilter = hardFilter;
        _ranker = ranker;
        _products = products;
        _categories = categories;
        _dataFilter = dataFilter;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Guid>> SearchProductsAsync(
        string query,
        EmbeddingSearchRequest request,
        CancellationToken ct = default)
    {
        // ── 1. Embed the query ────────────────────────────────────
        Pgvector.Vector queryVector;
        try
        {
            queryVector = await _embeddingService.GenerateEmbeddingAsync(query, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Embedding API failed; falling back to keyword search.");
            return await KeywordFallbackAsync(query, FinalReturnK, ct);
        }

        // ── 2. Vector search (broad recall) ──────────────────────
        var candidates = await _embeddingRepo.SearchSimilarAsync(
            queryVector,
            topK: BroadRecallK,
            activeOnly: true,
            cancellationToken: ct);

        if (candidates.Count == 0)
        {
            _logger.LogInformation(
                "Vector store returned 0 candidates. No embeddings indexed yet?");
            return await KeywordFallbackAsync(query, FinalReturnK, ct);
        }

        // Convert repository results to VectorSearchResult
        var vectorResults = candidates.Select(c => new VectorSearchResult
        {
            ProductId = c.ProductId,
            SimilarityScore = (float)c.SimilarityScore
        }).ToList();

        // ── 3. Hard filter (price, category, brand, stock) ───────
        var filtered = await _hardFilter.FilterAsync(vectorResults, request, ct);

        // ── 4. Fallback if filters were too aggressive ────────────
        if (filtered.Count < MinResultsBeforeFallback)
        {
            _logger.LogInformation(
                "Hard filter reduced {Before} candidates to {After}; activating keyword fallback.",
                vectorResults.Count, filtered.Count);

            var fallbackIds = await KeywordFallbackAsync(query, FinalReturnK, ct);

            // Merge: prefer vector results, pad with keyword fallback
            var merged = filtered.Select(r => r.ProductId)
                .Union(fallbackIds)
                .Take(FinalReturnK)
                .ToList();
            return merged;
        }

        // ── 5. Score fusion + re-rank ─────────────────────────────
        return await _ranker.RankAsync(filtered, FinalReturnK, ct);
    }

    /// <summary>
    /// Fallback: category-match first, then keyword scoring on SearchableText.
    /// Preserved from Phase 2 — never removed, only demoted.
    /// </summary>
    private async Task<IReadOnlyList<Guid>> KeywordFallbackAsync(
        string query, int topK, CancellationToken ct)
    {
        var keywords = query
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(k => k.Length > 2)
            .Take(5)
            .Select(k => k.ToLowerInvariant())
            .ToList();

        if (keywords.Count == 0) return [];

        // Category-match first (same logic as Phase 2 ProductSearchHandler)
        List<Category> allCategories;
        using (_dataFilter.Disable<IMultiTenant>())
        {
            allCategories = await _categories.GetListAsync(
                c => c.IsActive, cancellationToken: ct);
        }

        var matchedCategoryIds = allCategories
            .Where(c => keywords.Any(kw => KeywordMatches(kw, c.Name.ToLowerInvariant())))
            .Select(c => c.Id)
            .ToList();

        List<Product> allProducts;
        using (_dataFilter.Disable<IMultiTenant>())
        {
            allProducts = await _products.GetListAsync(
                p => p.IsActive, cancellationToken: ct);
        }

        if (matchedCategoryIds.Count > 0)
        {
            return allProducts
                .Where(p => p.CategoryId.HasValue && matchedCategoryIds.Contains(p.CategoryId.Value))
                .OrderBy(p => p.Price)
                .Take(topK)
                .Select(p => p.Id)
                .ToList();
        }

        // General keyword scoring fallback
        return allProducts
            .Select(p =>
            {
                var searchable = $"{p.Name} {p.SearchableText} {p.ShortDescription}".ToLowerInvariant();
                var matchCount = keywords.Count(kw => KeywordMatches(kw, searchable));
                return new { Product = p, Score = matchCount };
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(p => p.Product.Price)
            .Take(topK)
            .Select(p => p.Product.Id)
            .ToList();
    }

    private static bool KeywordMatches(string keyword, string text)
    {
        if (text.Contains(keyword)) return true;
        // Singular/plural tolerance: strip trailing 's' from both
        var kwSingular = keyword.EndsWith("s") ? keyword[..^1] : keyword;
        var textWords = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return textWords.Any(w =>
        {
            var wSingular = w.EndsWith("s") ? w[..^1] : w;
            return wSingular == kwSingular;
        });
    }
}
