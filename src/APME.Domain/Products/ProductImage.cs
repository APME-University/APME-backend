using System;
using Volo.Abp.Domain.Entities;
using Volo.Abp.MultiTenancy;

namespace APME.Products;

public class ProductImage : Entity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public Guid ProductId { get; private set; }

    public Guid? VariantId { get; set; }

    public string Url { get; set; }

    public string? AltText { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsPrimary { get; set; }

    public Guid? ImageEmbeddingId { get; set; }

    // Navigation properties
    public virtual Product Product { get; set; } = null!;
    public virtual ProductVariant? Variant { get; set; }

    protected ProductImage()
    {
    }

    public ProductImage(
        Guid id,
        Guid? tenantId,
        Guid productId,
        string url,
        bool isPrimary = false) : base(id)
    {
        TenantId = tenantId;
        ProductId = productId;
        Url = url;
        IsPrimary = isPrimary;
    }
}
