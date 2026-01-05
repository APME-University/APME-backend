using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using APME.Categories;
using APME.Products;
using APME.Shops;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;

namespace APME.Products;

/// <summary>
/// Builds canonical product documents for AI embedding generation.
/// Aggregates product data with category, shop, and normalized attribute information.
/// SRS Reference: AI Chatbot RAG Architecture - Canonical Document Generation
/// </summary>
public class CanonicalDocumentBuilder : ICanonicalDocumentBuilder, ITransientDependency
{
    /// <summary>
    /// Current schema version. Increment when document structure changes significantly.
    /// </summary>
    public int CurrentSchemaVersion => 1;

    private readonly IRepository<Category, Guid> _categoryRepository;
    private readonly IRepository<Shop, Guid> _shopRepository;
    private readonly IRepository<ProductAttribute, Guid> _attributeRepository;
    private readonly IDataFilter _dataFilter;

    public CanonicalDocumentBuilder(
        IRepository<Category, Guid> categoryRepository,
        IRepository<Shop, Guid> shopRepository,
        IRepository<ProductAttribute, Guid> attributeRepository,
        IDataFilter dataFilter)
    {
        _categoryRepository = categoryRepository;
        _shopRepository = shopRepository;
        _attributeRepository = attributeRepository;
        _dataFilter = dataFilter;
    }

    /// <inheritdoc />
    public async Task<CanonicalProductDocument> BuildAsync(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        var document = new CanonicalProductDocument
        {
            SchemaVersion = CurrentSchemaVersion,
            ProductId = product.Id,
            ShopId = product.ShopId,
            TenantId = product.TenantId,
            Name = product.Name,
            Description = product.Description,
            SKU = product.SKU,
            Price = product.Price,
            IsInStock = product.IsInStock(),
            IsOnSale = product.IsOnSale(),
            GeneratedAt = DateTime.UtcNow
        };

        // Load related data (may need to disable tenant filter for cross-tenant operations)
        using (_dataFilter.Disable<IMultiTenant>())
        {
            // Get category name
            if (product.CategoryId.HasValue)
            {
                var category = await _categoryRepository.FindAsync(product.CategoryId.Value);
                document.CategoryName = category?.Name;
            }

            // Get shop name
            var shop = await _shopRepository.FindAsync(product.ShopId);
            document.ShopName = shop?.Name;

            // Get and normalize attributes
            document.Attributes = await BuildNormalizedAttributesAsync(product);
        }

        return document;
    }

    /// <summary>
    /// Builds normalized attributes from the product's dynamic attribute values
    /// combined with attribute definitions for semantic labels and priorities.
    /// </summary>
    private async Task<Dictionary<string, CanonicalAttribute>> BuildNormalizedAttributesAsync(Product product)
    {
        var result = new Dictionary<string, CanonicalAttribute>();

        // Parse product attribute values
        if (string.IsNullOrWhiteSpace(product.Attributes))
        {
            return result;
        }

        Dictionary<string, object>? attributeValues;
        try
        {
            attributeValues = JsonSerializer.Deserialize<Dictionary<string, object>>(product.Attributes);
        }
        catch
        {
            return result;
        }

        if (attributeValues == null || attributeValues.Count == 0)
        {
            return result;
        }

        // Load attribute definitions for this shop (only those included in embeddings)
        var attributeDefinitions = await _attributeRepository
            .GetListAsync(a => a.ShopId == product.ShopId && a.IncludeInEmbedding);

        var definitionsLookup = attributeDefinitions.ToDictionary(
            a => a.Name,
            a => a,
            StringComparer.OrdinalIgnoreCase);

        // Build canonical attributes
        foreach (var kvp in attributeValues)
        {
            var value = kvp.Value?.ToString();
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var canonicalAttr = new CanonicalAttribute
            {
                Value = value,
                DataType = ProductAttributeDataType.Text,
                Priority = 0
            };

            // Enhance with definition metadata if available
            if (definitionsLookup.TryGetValue(kvp.Key, out var definition))
            {
                canonicalAttr.DataType = definition.DataType;
                canonicalAttr.Priority = definition.EmbeddingPriority;
                canonicalAttr.SemanticLabel = definition.SemanticLabel ?? definition.DisplayName;
            }
            else
            {
                // Use the key as semantic label for undefined attributes
                canonicalAttr.SemanticLabel = kvp.Key;
            }

            result[kvp.Key] = canonicalAttr;
        }

        return result;
    }
}



