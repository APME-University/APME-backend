using System;
using Volo.Abp.Domain.Entities;
using Volo.Abp.MultiTenancy;

namespace APME.Products;

public class AttributeOption : Entity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public Guid ProductAttributeId { get; private set; }

    public string Value { get; set; }

    public string DisplayValue { get; set; }

    public int DisplayOrder { get; set; }

    // Navigation properties
    public virtual ProductAttribute AttributeDefinition { get; set; } = null!;

    protected AttributeOption()
    {
    }

    public AttributeOption(
        Guid id,
        Guid? tenantId,
        Guid productAttributeId,
        string value,
        string displayValue) : base(id)
    {
        TenantId = tenantId;
        ProductAttributeId = productAttributeId;
        Value = value;
        DisplayValue = displayValue;
    }
}
