using System;
using Volo.Abp.Domain.Entities;
using Volo.Abp.MultiTenancy;

namespace APME.Products;

/// <summary>
/// EAV value row — stores the actual attribute value for a specific product.
/// </summary>
public class ProductAttributeValue : Entity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public Guid ProductId { get; private set; }

    public Guid ProductAttributeId { get; private set; }

    public string? TextValue { get; set; }

    public decimal? NumericValue { get; set; }

    public bool? BoolValue { get; set; }

    /// <summary>
    /// Always populated — the chatbot reads this field directly.
    /// Example: "16 GB", "10 hours", "4K UHD (3840 × 2160)"
    /// </summary>
    public string? DisplayValue { get; set; }

    // Navigation properties
    public virtual Product Product { get; set; } = null!;
    public virtual ProductAttribute AttributeDefinition { get; set; } = null!;

    protected ProductAttributeValue()
    {
    }

    public ProductAttributeValue(
        Guid id,
        Guid? tenantId,
        Guid productId,
        Guid productAttributeId,
        string? displayValue = null) : base(id)
    {
        TenantId = tenantId;
        ProductId = productId;
        ProductAttributeId = productAttributeId;
        DisplayValue = displayValue;
    }
}
