using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using APME.Categories;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace APME.Products;

public class SearchableTextService : ISearchableTextService, ITransientDependency
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<Brand, Guid> _brandRepository;
    private readonly IRepository<Category, Guid> _categoryRepository;
    private readonly IRepository<ProductTag, Guid> _productTagRepository;
    private readonly IRepository<ProductAttribute, Guid> _productAttributeRepository;
    private readonly IRepository<ProductAttributeValue, Guid> _productAttributeValueRepository;
    private readonly ILogger<SearchableTextService> _logger;

    public SearchableTextService(
        IRepository<Product, Guid> productRepository,
        IRepository<Brand, Guid> brandRepository,
        IRepository<Category, Guid> categoryRepository,
        IRepository<ProductTag, Guid> productTagRepository,
        IRepository<ProductAttribute, Guid> productAttributeRepository,
        IRepository<ProductAttributeValue, Guid> productAttributeValueRepository,
        ILogger<SearchableTextService> logger)
    {
        _productRepository = productRepository;
        _brandRepository = brandRepository;
        _categoryRepository = categoryRepository;
        _productTagRepository = productTagRepository;
        _productAttributeRepository = productAttributeRepository;
        _productAttributeValueRepository = productAttributeValueRepository;
        _logger = logger;
    }

    public async Task<string> BuildAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.FindAsync(productId, cancellationToken: cancellationToken);
        if (product == null)
        {
            _logger.LogWarning("Product {ProductId} not found for SearchableText generation", productId);
            return string.Empty;
        }

        var sb = new StringBuilder();

        // Name
        sb.Append(product.Name);

        // Brand
        if (product.BrandId.HasValue)
        {
            var brand = await _brandRepository.FindAsync(product.BrandId.Value, cancellationToken: cancellationToken);
            if (brand != null)
            {
                sb.Append(" | Brand: ").Append(brand.Name);
            }
        }

        // Category path
        if (product.CategoryId.HasValue)
        {
            var categoryPath = await BuildCategoryPathAsync(product.CategoryId.Value, cancellationToken);
            if (!string.IsNullOrEmpty(categoryPath))
            {
                sb.Append(" | Category: ").Append(categoryPath);
            }
        }

        // Tags
        var tagQueryable = await _productTagRepository.GetQueryableAsync();
        var tags = tagQueryable.Where(t => t.ProductId == productId).Select(t => t.Tag).ToList();
        if (tags.Any())
        {
            sb.Append(" | Tags: ").Append(string.Join(", ", tags));
        }

        // Short description or description
        var desc = product.ShortDescription ?? product.Description;
        if (!string.IsNullOrWhiteSpace(desc))
        {
            var truncated = desc.Length > 300 ? desc.Substring(0, 300) + "..." : desc;
            sb.Append(" | Desc: ").Append(truncated);
        }

        // Searchable attribute values
        var attrValueQueryable = await _productAttributeValueRepository.GetQueryableAsync();
        var attrValues = attrValueQueryable
            .Where(v => v.ProductId == productId)
            .ToList();

        if (attrValues.Any())
        {
            var attrIds = attrValues.Select(v => v.ProductAttributeId).Distinct().ToList();
            var attrQueryable = await _productAttributeRepository.GetQueryableAsync();
            var searchableAttrs = attrQueryable
                .Where(a => attrIds.Contains(a.Id) && a.IsSearchable)
                .OrderByDescending(a => a.EmbeddingPriority)
                .ToList();

            if (searchableAttrs.Any())
            {
                sb.Append(" | Attrs: ");
                var attrParts = searchableAttrs.Select(a =>
                {
                    var values = string.Join("; ", attrValues
                        .Where(v => v.ProductAttributeId == a.Id)
                        .Select(v => v.DisplayValue ?? v.TextValue ?? string.Empty));
                    return $"{a.DisplayName}={values}";
                });
                sb.Append(string.Join(", ", attrParts));
            }
        }

        // Search hints
        if (!string.IsNullOrWhiteSpace(product.SearchHints))
        {
            sb.Append(" | Hints: ").Append(product.SearchHints);
        }

        var result = sb.ToString();
        _logger.LogDebug("SearchableText built for product {ProductId}: {Length} chars", productId, result.Length);
        return result;
    }

    private async Task<string> BuildCategoryPathAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        var parts = new List<string>();
        var currentId = (Guid?)categoryId;

        while (currentId.HasValue)
        {
            var category = await _categoryRepository.FindAsync(currentId.Value, cancellationToken: cancellationToken);
            if (category == null) break;
            parts.Insert(0, category.Name);
            currentId = category.ParentId;
        }

        return string.Join(" > ", parts);
    }
}
