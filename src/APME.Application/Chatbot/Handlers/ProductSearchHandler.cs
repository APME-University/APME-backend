using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using APME.Categories;
using APME.Chat;
using APME.Chatbot.Embeddings;
using APME.Chatbot.Handlers;
using APME.Chatbot.Intents;
using APME.Products;
using Microsoft.Extensions.Logging;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;

namespace APME.Chatbot.Handlers;

/// <summary>
/// Phase 3: Uses IHybridSearchService (semantic + keyword) as primary search.
/// Keyword-only path preserved as safety net when hybrid returns 0 results.
/// </summary>
public class ProductSearchHandler : IIntentHandler, ITransientDependency
{
    private readonly IHybridSearchService _hybridSearch;
    private readonly IRepository<Product, Guid> _productRepo;
    private readonly IRepository<Category, Guid> _categoryRepo;
    private readonly IRepository<Brand, Guid> _brandRepo;
    private readonly IDataFilter _dataFilter;
    private readonly ILogger<ProductSearchHandler> _logger;

    private static readonly string[] StopWords =
    [
        "show", "find", "get", "give", "me", "my", "the", "a", "an", "i",
        "want", "need", "looking", "for", "with", "under", "over", "below",
        "above", "between", "than", "less", "more", "about", "please",
        "can", "could", "would", "some", "any", "all", "and", "or",
        "that", "this", "what", "which", "how", "where", "who", "when"
    ];

    public ProductSearchHandler(
        IHybridSearchService hybridSearch,
        IRepository<Product, Guid> productRepo,
        IRepository<Category, Guid> categoryRepo,
        IRepository<Brand, Guid> brandRepo,
        IDataFilter dataFilter,
        ILogger<ProductSearchHandler> logger)
    {
        _hybridSearch = hybridSearch;
        _productRepo = productRepo;
        _categoryRepo = categoryRepo;
        _brandRepo = brandRepo;
        _dataFilter = dataFilter;
        _logger = logger;
    }

    public IntentType TargetIntent => IntentType.ProductSearch;

    public async Task<IntentHandlerResult> HandleAsync(
        IntentClassificationResult classification,
        ClassificationContext context,
        CancellationToken ct = default)
    {
        var query = classification.RewrittenQuery ?? context.CurrentMessage;

        // ── Build hard-filter constraints from extracted entities ──
        var request = await BuildSearchRequestAsync(classification, context, ct);

        // ── Delegate to hybrid search (semantic + keyword) ──────────
        var productIds = await _hybridSearch.SearchProductsAsync(query, request, ct);

        // ── Fallback: if hybrid returns nothing, try pure keyword ──
        if (productIds.Count == 0)
        {
            _logger.LogInformation("Hybrid search returned 0 results; falling back to keyword-only path.");
            return await KeywordFallbackAsync(classification, context, ct);
        }

        // ── Load products for display ───────────────────────────────
        List<Product> products;
        using (_dataFilter.Disable<IMultiTenant>())
        {
            products = await _productRepo.GetListAsync(
                p => productIds.Contains(p.Id), cancellationToken: ct);
        }

        // Preserve the ranking order from hybrid search
        var ordered = productIds
            .Select(id => products.FirstOrDefault(p => p.Id == id))
            .Where(p => p != null)
            .Select(p => p!)
            .Take(6)
            .ToList();

        if (ordered.Count == 0)
        {
            return new IntentHandlerResult
            {
                Reply = "I couldn't find products matching that description. Could you try different keywords, or would you like to browse a category?"
            };
        }

        var lines = ordered.Select(p =>
            $"- **{p.Name}** — {p.Currency} {p.Price:F2}{(p.SalePrice.HasValue ? $" (on sale: {p.Currency} {p.SalePrice.Value:F2})" : "")}");

        var searchDesc = string.Join(", ", query.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(4));
        var reply = $"Here are some products matching \"{searchDesc}\":\n{string.Join("\n", lines)}";

        return new IntentHandlerResult
        {
            Reply = reply,
            ReferencedProductIds = ordered.Select(p => p.Id).ToList()
        };
    }

    /// <summary>
    /// Builds an EmbeddingSearchRequest from extracted entities (category, brand, price_range).
    /// </summary>
    private async Task<EmbeddingSearchRequest> BuildSearchRequestAsync(
        IntentClassificationResult classification,
        ClassificationContext context,
        CancellationToken ct)
    {
        // Resolve category entity → CategoryId
        Guid? categoryId = null;
        var categoryName = classification.GetEntity("category");
        if (!string.IsNullOrWhiteSpace(categoryName))
        {
            using (_dataFilter.Disable<IMultiTenant>())
            {
                var categories = await _categoryRepo.GetListAsync(
                    c => c.IsActive, cancellationToken: ct);
                var match = categories.FirstOrDefault(c =>
                    KeywordMatches(categoryName, c.Name.ToLowerInvariant()) ||
                    c.Slug.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
                categoryId = match?.Id;
            }
        }

        // Resolve brand entity → BrandId
        Guid? brandId = null;
        var brandName = classification.GetEntity("brand");
        if (!string.IsNullOrWhiteSpace(brandName))
        {
            using (_dataFilter.Disable<IMultiTenant>())
            {
                var brands = await _brandRepo.GetListAsync(
                    b => b.IsActive, cancellationToken: ct);
                var match = brands.FirstOrDefault(b =>
                    b.Name.Contains(brandName, StringComparison.OrdinalIgnoreCase));
                brandId = match?.Id;
            }
        }

        // Parse price range entity
        decimal? maxPrice = null;
        decimal? minPrice = null;
        var priceRange = classification.GetEntity("price_range")
            ?? ExtractPriceRange(context.CurrentMessage);
        if (!string.IsNullOrWhiteSpace(priceRange))
            ParsePriceRange(priceRange, out minPrice, out maxPrice);

        return new EmbeddingSearchRequest
        {
            QueryVector = null,  // populated by HybridSearchService
            TopK = 25,
            ModelName = string.Empty, // populated by HybridSearchService
            CategoryId = categoryId,
            BrandId = brandId,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            InStockOnly = true
        };
    }

    /// <summary>
    /// Keyword-only fallback — preserved from Phase 2 for when hybrid search returns 0.
    /// </summary>
    private async Task<IntentHandlerResult> KeywordFallbackAsync(
        IntentClassificationResult classification,
        ClassificationContext context,
        CancellationToken ct)
    {
        var productName = classification.GetEntity("product_name");
        var category = classification.GetEntity("category");
        var brand = classification.GetEntity("brand");
        var priceRange = classification.GetEntity("price_range");

        var searchKeywords = new[] { productName, brand, category }
            .Where(e => e != null)
            .Select(e => e!)
            .ToList();

        if (searchKeywords.Count == 0)
        {
            var fallback = classification.RewrittenQuery ?? context.CurrentMessage;
            searchKeywords = ExtractKeywords(fallback);
        }

        if (priceRange == null)
            priceRange = ExtractPriceRange(context.CurrentMessage);

        List<Product> allProducts;
        using (_dataFilter.Disable<IMultiTenant>())
        {
            allProducts = await _productRepo.GetListAsync(
                p => p.IsActive, cancellationToken: ct);
        }

        List<Category> allCategories;
        using (_dataFilter.Disable<IMultiTenant>())
        {
            allCategories = await _categoryRepo.GetListAsync(
                c => c.IsActive, cancellationToken: ct);
        }

        var matchedCategoryIds = allCategories
            .Where(c => searchKeywords.Any(kw => KeywordMatches(kw, c.Name.ToLowerInvariant())))
            .Select(c => c.Id)
            .ToList();

        var scored = matchedCategoryIds.Count > 0
            ? allProducts
                .Where(p => p.CategoryId.HasValue && matchedCategoryIds.Contains(p.CategoryId.Value))
                .Select(p => new { Product = p, Score = 1 })
                .OrderBy(p => p.Product.Price)
                .Take(10)
                .ToList()
            : allProducts
                .Select(p =>
                {
                    var searchable = $"{p.Name} {p.SearchableText} {p.ShortDescription}".ToLowerInvariant();
                    var matchCount = searchKeywords.Count(kw => KeywordMatches(kw, searchable));
                    return new { Product = p, Score = matchCount };
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ThenBy(p => p.Product.Price)
                .Take(10)
                .ToList();

        if (priceRange != null && scored.Count > 0)
        {
            var maxPrice = ParseMaxPrice(priceRange);
            if (maxPrice > 0)
            {
                var filtered = scored.Where(p => p.Product.Price <= maxPrice).ToList();
                if (filtered.Count > 0) scored = filtered;
            }
        }

        scored = scored.Take(5).ToList();

        if (scored.Count == 0)
        {
            var queryDesc = string.Join(", ", searchKeywords);
            return new IntentHandlerResult
            {
                Reply = $"I couldn't find any products matching \"{queryDesc}\". Could you try different keywords?"
            };
        }

        var ids = scored.Select(p => p.Product.Id).ToList();
        var lines = scored.Select(p =>
            $"- **{p.Product.Name}** — {p.Product.Currency} {p.Product.Price:F2}{(p.Product.SalePrice.HasValue ? $" (on sale: {p.Product.Currency} {p.Product.SalePrice.Value:F2})" : "")}");

        var searchDesc = string.Join(", ", searchKeywords);
        var reply = $"Here are some products matching \"{searchDesc}\":\n{string.Join("\n", lines)}";

        return new IntentHandlerResult
        {
            Reply = reply,
            ReferencedProductIds = ids
        };
    }

    private static bool KeywordMatches(string keyword, string searchableText)
    {
        var kw = keyword.ToLowerInvariant();
        if (searchableText.Contains(kw)) return true;
        var stems = new[] { kw, kw.TrimEnd('s'), kw.Length > 2 && kw.EndsWith("es") ? kw[..^2] : kw, kw + "s", kw + "es" };
        return stems.Any(s => s.Length >= 3 && searchableText.Contains(s));
    }

    private static List<string> ExtractKeywords(string text)
    {
        var cleaned = System.Text.RegularExpressions.Regex.Replace(
            text, @"[\$€£]\d+|\b\d+\s*(sar|usd|eur|gbp|rs|₹)\b", "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return cleaned
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2)
            .Where(w => !StopWords.Contains(w.ToLowerInvariant()))
            .Where(w => !int.TryParse(w.TrimStart('$', '€', '£'), out _))
            .Take(4)
            .ToList();
    }

    private static string? ExtractPriceRange(string message)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            message,
            @"(?:under|below|less\s+than|max|up\s+to)\s+[\$€£]?(\d+)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success ? match.Value : null;
    }

    private static decimal ParseMaxPrice(string priceRange)
    {
        var digits = new string(priceRange.Where(char.IsDigit).ToArray());
        return decimal.TryParse(digits, out var price) ? price : 0;
    }

    private static void ParsePriceRange(string input, out decimal? min, out decimal? max)
    {
        min = max = null;
        var nums = System.Text.RegularExpressions.Regex
            .Matches(input, @"\d+(?:\.\d+)?")
            .Select(m => decimal.Parse(m.Value))
            .OrderBy(v => v)
            .ToList();

        if (nums.Count == 0) return;

        var lower = input.ToLowerInvariant();
        if (lower.Contains("under") || lower.Contains("below") || lower.Contains("less"))
            max = nums[0];
        else if (lower.Contains("over") || lower.Contains("above") || lower.Contains("more"))
            min = nums[0];
        else if (nums.Count >= 2)
        { min = nums[0]; max = nums[1]; }
        else
            max = nums[0];
    }
}
