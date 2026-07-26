using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using APME.Chat;
using APME.Chatbot.Handlers;
using APME.Chatbot.Intents;
using APME.Categories;
using APME.Products;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;

namespace APME.Chatbot.Handlers;

public class CategoryBrowseHandler : IIntentHandler, ITransientDependency
{
    private readonly IRepository<Category, Guid> _categoryRepo;
    private readonly IRepository<Product, Guid> _productRepo;
    private readonly IDataFilter _dataFilter;

    public CategoryBrowseHandler(
        IRepository<Category, Guid> categoryRepo,
        IRepository<Product, Guid> productRepo,
        IDataFilter dataFilter)
    {
        _categoryRepo = categoryRepo;
        _productRepo = productRepo;
        _dataFilter = dataFilter;
    }

    public IntentType TargetIntent => IntentType.CategoryBrowse;

    public async Task<IntentHandlerResult> HandleAsync(
        IntentClassificationResult classification,
        ClassificationContext context,
        CancellationToken ct = default)
    {
        var categoryName = classification.GetEntity("category")
                           ?? context.ActiveCategorySlug;

        Category? category = null;

        if (categoryName is not null)
        {
            // Search by name or slug across all active categories
            List<Category> allCategories;
            using (_dataFilter.Disable<IMultiTenant>())
            {
                allCategories = await _categoryRepo.GetListAsync(
                    c => c.IsActive, cancellationToken: ct);
            }

            category = allCategories.FirstOrDefault(c =>
                    c.Name.Contains(categoryName, StringComparison.OrdinalIgnoreCase))
                ?? allCategories.FirstOrDefault(c =>
                    c.Slug.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
        }

        if (category is null)
        {
            // Show all active categories (not just top-level, since ParentId may not be set)
            List<Category> categories;
            using (_dataFilter.Disable<IMultiTenant>())
            {
                categories = (await _categoryRepo.GetListAsync(
                    c => c.IsActive,
                    cancellationToken: ct))
                    .Take(10).ToList();
            }

            if (categories.Count == 0)
            {
                return new IntentHandlerResult
                {
                    Reply = "No categories are available right now. Please try again later."
                };
            }

            var lines = categories.Select(c => $"- **{c.Name}**");
            return new IntentHandlerResult
            {
                Reply = $"Here are our product categories:\n{string.Join("\n", lines)}\n\nWhich category interests you?"
            };
        }

        // Show products in the found category
        List<Product> products;
        using (_dataFilter.Disable<IMultiTenant>())
        {
            products = (await _productRepo.GetListAsync(
                p => p.IsActive && p.CategoryId == category.Id,
                cancellationToken: ct))
                .Take(5).ToList();
        }

        if (products.Count == 0)
        {
            return new IntentHandlerResult
            {
                Reply = $"The **{category.Name}** category doesn't have any products at the moment.",
                ReferencedCategoryIds = [category.Id]
            };
        }

        var productLines = products.Select(p =>
            $"- **{p.Name}** — {p.Currency} {p.Price:F2}");

        return new IntentHandlerResult
        {
            Reply = $"Here are some products in **{category.Name}**:\n{string.Join("\n", productLines)}",
            ReferencedProductIds = products.Select(p => p.Id).ToList(),
            ReferencedCategoryIds = [category.Id]
        };
    }
}
