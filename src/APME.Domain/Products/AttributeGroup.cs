using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace APME.Products;

public class AttributeGroup : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public string Name { get; set; }

    /// <summary>
    /// null = global attribute group (applies to all categories).
    /// </summary>
    public Guid? CategoryId { get; set; }

    public int DisplayOrder { get; set; }

    // Navigation properties
    public virtual APME.Categories.Category? Category { get; set; }
    public virtual ICollection<ProductAttribute> Definitions { get; set; } = new List<ProductAttribute>();

    protected AttributeGroup()
    {
    }

    public AttributeGroup(
        Guid id,
        Guid? tenantId,
        string name,
        Guid? categoryId = null) : base(id)
    {
        TenantId = tenantId;
        Name = name;
        CategoryId = categoryId;
    }
}
