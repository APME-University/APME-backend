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

public class ProductDetailsHandler : IIntentHandler, ITransientDependency
{
    private readonly IRepository<Product, Guid> _productRepo;
    private readonly IDataFilter _dataFilter;

    public ProductDetailsHandler(IRepository<Product, Guid> productRepo, IDataFilter dataFilter)
    {
        _productRepo = productRepo;
        _dataFilter = dataFilter;
    }

    public IntentType TargetIntent => IntentType.ProductDetails;

    public async Task<IntentHandlerResult> HandleAsync(
        IntentClassificationResult classification,
        ClassificationContext context,
        CancellationToken ct = default)
    {
        // Try entity first, then extract from message, then last viewed
        var productName = classification.GetEntity("product_name")
                          ?? classification.GetEntity("brand");

        // If no entity extracted, try to extract a meaningful keyword from the message
        if (productName is null)
        {
            var words = context.CurrentMessage
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 3 && !new[] { "what", "specs", "specifications", "details", "about", "tell", "me", "the", "please", "show" }.Contains(w.ToLowerInvariant()))
                .ToList();
            productName = words.FirstOrDefault();
        }

        Product? product = null;

        if (productName is not null)
        {
            // Search by name in Name, SearchableText, or ShortDescription
            List<Product> allProducts;
            using (_dataFilter.Disable<IMultiTenant>())
            {
                allProducts = await _productRepo.GetListAsync(
                    p => p.IsActive, cancellationToken: ct);
            }

            product = allProducts.FirstOrDefault(p =>
                    p.Name.Contains(productName, StringComparison.OrdinalIgnoreCase))
                ?? allProducts.FirstOrDefault(p =>
                    p.SearchableText != null && p.SearchableText.Contains(productName, StringComparison.OrdinalIgnoreCase))
                ?? allProducts.FirstOrDefault(p =>
                    p.ShortDescription != null && p.ShortDescription.Contains(productName, StringComparison.OrdinalIgnoreCase));
        }

        if (product is null && context.LastViewedProductIds.Count > 0)
        {
            using (_dataFilter.Disable<IMultiTenant>())
            {
                product = await _productRepo.FirstOrDefaultAsync(
                    p => p.Id == context.LastViewedProductIds[0] && p.IsActive, ct);
            }
        }

        if (product is null)
        {
            return new IntentHandlerResult
            {
                Reply = "Which product would you like details about? Please specify a product name."
            };
        }

        var reply = $"**{product.Name}**\n" +
                    $"Price: {product.Currency} {product.Price:F2}" +
                    $"{(product.SalePrice.HasValue ? $" (Sale: {product.Currency} {product.SalePrice.Value:F2})" : "")}\n" +
                    $"{(product.ShortDescription is not null ? product.ShortDescription + "\n" : "")}" +
                    $"Stock: {(product.StockQuantity > 0 ? "In stock" : "Out of stock")}";

        return new IntentHandlerResult
        {
            Reply = reply,
            ReferencedProductIds = [product.Id]
        };
    }
}
