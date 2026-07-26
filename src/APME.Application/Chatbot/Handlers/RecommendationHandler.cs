using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using APME.Chat;
using APME.Chatbot.Handlers;
using APME.Chatbot.Intents;
using APME.Products;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;

namespace APME.Chatbot.Handlers;

public class RecommendationHandler : IIntentHandler, ITransientDependency
{
    private readonly IRepository<Product, Guid> _productRepo;
    private readonly IDataFilter _dataFilter;

    public RecommendationHandler(IRepository<Product, Guid> productRepo, IDataFilter dataFilter)
    {
        _productRepo = productRepo;
        _dataFilter = dataFilter;
    }

    public IntentType TargetIntent => IntentType.Recommendation;

    public async Task<IntentHandlerResult> HandleAsync(
        IntentClassificationResult classification,
        ClassificationContext context,
        CancellationToken ct = default)
    {
        // Use extracted category/brand/use_case entities to filter
        var category = classification.GetEntity("category");
        var brand = classification.GetEntity("brand");
        var useCase = classification.GetEntity("use_case");

        // Build search keywords from entities
        var searchKeywords = new[] { category, brand, useCase }
            .Where(e => e != null)
            .Select(e => e!)
            .ToList();

        List<Product> allProducts;
        using (_dataFilter.Disable<IMultiTenant>())
        {
            allProducts = await _productRepo.GetListAsync(
                p => p.IsActive, cancellationToken: ct);
        }

        // Try featured products first
        var products = allProducts.Where(p => p.IsFeatured).Take(5).ToList();

        // If no featured products, try matching by entity keywords
        if (products.Count == 0 && searchKeywords.Count > 0)
        {
            products = allProducts
                .Select(p =>
                {
                    var searchable = $"{p.Name} {p.SearchableText} {p.ShortDescription}".ToLowerInvariant();
                    var score = searchKeywords.Count(kw => searchable.Contains(kw.ToLowerInvariant()));
                    return new { Product = p, Score = score };
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Take(5)
                .Select(x => x.Product)
                .ToList();
        }

        // If still nothing, show any active products
        if (products.Count == 0)
        {
            products = allProducts.Take(5).ToList();
        }

        if (products.Count == 0)
        {
            return new IntentHandlerResult
            {
                Reply = "I don't have any products to recommend right now. Could you tell me what kind of product you're looking for?"
            };
        }

        var ids = products.Select(p => p.Id).ToList();
        var lines = products.Select(p =>
            $"- **{p.Name}** — {p.Currency} {p.Price:F2}{(p.SalePrice.HasValue ? $" (sale: {p.Currency} {p.SalePrice.Value:F2})" : "")}");

        var prefix = searchKeywords.Count > 0
            ? $"Based on your preferences ({string.Join(", ", searchKeywords)}), here are my recommendations:"
            : "Here are some popular picks for you:";

        return new IntentHandlerResult
        {
            Reply = $"{prefix}\n{string.Join("\n", lines)}",
            ReferencedProductIds = ids
        };
    }
}
