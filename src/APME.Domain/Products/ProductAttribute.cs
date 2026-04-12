using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace APME.Products;

public class ProductAttribute : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public Guid ShopId { get; set; } // FK to Shop

    public string Name { get; set; }

    public string DisplayName { get; set; }

    public string? Slug { get; set; }

    public Guid? AttributeGroupId { get; set; }

    public ProductAttributeDataType DataType { get; set; }

    public bool IsRequired { get; set; }

    public int DisplayOrder { get; set; }

    /// <summary>
    /// Include in SearchableText → embedding.
    /// </summary>
    public bool IsSearchable { get; set; } = true;

    /// <summary>
    /// Expose in filter sidebar.
    /// </summary>
    public bool IsFilterable { get; set; }

    /// <summary>
    /// Include in comparison table response.
    /// </summary>
    public bool IsComparable { get; set; }

    /// <summary>
    /// JSON: {"min": 0, "max": 100} or {"maxLength": 500}
    /// </summary>
    public string? ValidationRulesJson { get; set; }

    /// <summary>
    /// Whether this attribute should be included in AI embeddings.
    /// High-signal attributes improve semantic search quality.
    /// </summary>
    public bool IncludeInEmbedding { get; set; } = true;

    /// <summary>
    /// Priority for embedding inclusion. Higher values = more important.
    /// Used for ranking when building canonical product documents.
    /// </summary>
    public int EmbeddingPriority { get; set; } = 0;

    /// <summary>
    /// Human-readable semantic label for embedding context.
    /// Example: "Color" -> "product color", "Size" -> "available size"
    /// </summary>
    public string? SemanticLabel { get; set; }

    protected ProductAttribute()
    {
        // Required by EF Core
    }

    // Navigation properties
    public virtual AttributeGroup? Group { get; set; }
    public virtual ICollection<AttributeOption> Options { get; set; } = new List<AttributeOption>();
    public virtual ICollection<ProductAttributeValue> ProductAttributeValues { get; set; } = new List<ProductAttributeValue>();

    public ProductAttribute(
        Guid id,
        Guid? tenantId,
        Guid shopId,
        string name,
        string displayName,
        ProductAttributeDataType dataType) : base(id)
    {
        TenantId = tenantId;
        ShopId = shopId;
        Name = name;
        DisplayName = displayName;
        DataType = dataType;
        IsRequired = false;
        DisplayOrder = 0;
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be null or empty", nameof(name));
        }
        Name = name;
    }

    public void UpdateDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Display name cannot be null or empty", nameof(displayName));
        }
        DisplayName = displayName;
    }

    public void SetRequired(bool isRequired)
    {
        IsRequired = isRequired;
    }

    public void UpdateDisplayOrder(int displayOrder)
    {
        DisplayOrder = displayOrder;
    }

    /// <summary>
    /// Configure embedding behavior for this attribute
    /// </summary>
    public void ConfigureEmbedding(bool includeInEmbedding, int priority = 0, string? semanticLabel = null)
    {
        IncludeInEmbedding = includeInEmbedding;
        EmbeddingPriority = priority;
        SemanticLabel = semanticLabel;
    }
}

