using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities;
using Volo.Abp.MultiTenancy;

namespace APME.Products;

public class ProductVariant : Entity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public Guid ProductId { get; private set; }

    public string SKU { get; set; }

    public string Name { get; set; }

    public decimal? Price { get; set; }

    public decimal? SalePrice { get; set; }

    public int StockQuantity { get; set; }

    public StockStatus StockStatus { get; set; } = StockStatus.InStock;

    public bool IsDefault { get; set; }

    /// <summary>
    /// JSON object: {"color": "Red", "size": "L"}
    /// </summary>
    public string? VariantAttributesJson { get; set; }

    // Navigation properties
    public virtual Product Product { get; set; } = null!;
    public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

    protected ProductVariant()
    {
    }

    public ProductVariant(
        Guid id,
        Guid? tenantId,
        Guid productId,
        string sku,
        string name) : base(id)
    {
        TenantId = tenantId;
        ProductId = productId;
        SKU = sku;
        Name = name;
    }
}
