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

public class ProductComparisonHandler : IIntentHandler, ITransientDependency
{
    private readonly IRepository<Product, Guid> _productRepo;
    private readonly IDataFilter _dataFilter;

    public ProductComparisonHandler(IRepository<Product, Guid> productRepo, IDataFilter dataFilter)
    {
        _productRepo = productRepo;
        _dataFilter = dataFilter;
    }

    public IntentType TargetIntent => IntentType.ProductComparison;

    public async Task<IntentHandlerResult> HandleAsync(
        IntentClassificationResult classification,
        ClassificationContext context,
        CancellationToken ct = default)
    {
        // First try context compare list, then look up by extracted entity names
        var products = new List<Product>();

        if (context.CompareList.Count >= 2)
        {
            using (_dataFilter.Disable<IMultiTenant>())
            {
                products = (await _productRepo.GetListAsync(
                    p => context.CompareList.Contains(p.Id) && p.IsActive,
                    cancellationToken: ct)).ToList();
            }
        }

        if (products.Count() < 2)
        {
            // Look up products by entity names extracted by the classifier
            var entityNames = classification.Entities
                .Where(e => e.Type is "product_name" or "brand")
                .Select(e => e.Value)
                .Distinct()
                .ToList();

            if (entityNames.Count > 0)
            {
                List<Product> allProducts;
                using (_dataFilter.Disable<IMultiTenant>())
                {
                    allProducts = await _productRepo.GetListAsync(
                        p => p.IsActive, cancellationToken: ct);
                }

                foreach (var name in entityNames.Take(2))
                {
                    var match = allProducts.FirstOrDefault(p =>
                        p.Name.Contains(name, StringComparison.OrdinalIgnoreCase) ||
                        (p.SearchableText != null && p.SearchableText.Contains(name, StringComparison.OrdinalIgnoreCase)));
                    if (match != null && products.All(p => p.Id != match.Id))
                        products.Add(match);
                }
            }
        }

        if (products.Count() < 2)
        {
            return new IntentHandlerResult
            {
                Reply = "I'd be happy to compare products! Please tell me which two products you'd like to compare."
            };
        }

        var p1 = products[0];
        var p2 = products[1];

        var reply = $"Here's a comparison:\n\n" +
                    $"| | **{p1.Name}** | **{p2.Name}** |\n" +
                    $"|---|---|---|\n" +
                    $"| Price | {p1.Currency} {p1.Price:F2}{(p1.SalePrice.HasValue ? $" ~~{p1.Currency} {p1.SalePrice.Value:F2}~~" : "")} | {p2.Currency} {p2.Price:F2}{(p2.SalePrice.HasValue ? $" ~~{p2.Currency} {p2.SalePrice.Value:F2}~~" : "")} |\n" +
                    $"| Stock | {(p1.StockQuantity > 0 ? "In stock" : "Out of stock")} | {(p2.StockQuantity > 0 ? "In stock" : "Out of stock")} |\n" +
                    $"{(p1.ShortDescription != null || p2.ShortDescription != null ? $"| Description | {p1.ShortDescription ?? "—"} | {p2.ShortDescription ?? "—"} |\n" : "")}" +
                    $"\nWould you like more details on either of these?";

        return new IntentHandlerResult
        {
            Reply = reply,
            ReferencedProductIds = products.Select(p => p.Id).ToList()
        };
    }
}
