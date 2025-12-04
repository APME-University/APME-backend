using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace APME.Products;

public class ProductAttribute : FullAuditedEntity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public Guid ShopId { get; set; } // FK to Shop

    public string Name { get; set; }

    public string DisplayName { get; set; }

    public ProductAttributeDataType DataType { get; set; }

    public bool IsRequired { get; set; }

    public int DisplayOrder { get; set; }

    protected ProductAttribute()
    {
        // Required by EF Core
    }

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
}

