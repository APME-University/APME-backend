using System;
using Volo.Abp.Domain.Entities;
using Volo.Abp.MultiTenancy;

namespace APME.Products;

public class ProductTag : Entity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public Guid ProductId { get; private set; }

    public string Tag { get; set; }

    // Navigation properties
    public virtual Product Product { get; set; } = null!;

    protected ProductTag()
    {
    }

    public ProductTag(
        Guid id,
        Guid? tenantId,
        Guid productId,
        string tag) : base(id)
    {
        TenantId = tenantId;
        ProductId = productId;
        Tag = tag;
    }
}
