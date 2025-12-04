using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace APME.Categories;

public class Category : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public Guid ShopId { get; set; } // FK to Shop

    public string Name { get; set; }

    public string Slug { get; set; }

    public string? Description { get; set; }

    public Guid? ParentId { get; set; } // For hierarchical categories

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public string? ImageUrl { get; set; }

    // Navigation properties
    public virtual Category? Parent { get; set; }

    public virtual ICollection<Category> Children { get; set; }

    protected Category()
    {
        Children = new List<Category>();
    }

    public Category(
        Guid id,
        Guid? tenantId,
        Guid shopId,
        string name,
        string slug) : base(id)
    {
        TenantId = tenantId;
        ShopId = shopId;
        Name = name;
        Slug = slug;
        DisplayOrder = 0;
        IsActive = true;
        Children = new List<Category>();
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be null or empty", nameof(name));
        }
        Name = name;
    }

    public void UpdateSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ArgumentException("Slug cannot be null or empty", nameof(slug));
        }
        Slug = slug;
    }

    public void SetParent(Guid? parentId)
    {
        if (parentId.HasValue && parentId.Value == Id)
        {
            throw new InvalidOperationException("Category cannot be its own parent");
        }
        ParentId = parentId;
    }

    public void UpdateDisplayOrder(int displayOrder)
    {
        DisplayOrder = displayOrder;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}

