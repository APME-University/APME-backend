using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace APME.Shops;

public class Shop : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public string Name { get; set; }

    public string? Description { get; set; }

    public string Slug { get; set; }

    public bool IsActive { get; set; }

    public string? LogoUrl { get; set; }

    public string? Settings { get; set; } // JSONB for flexible shop settings

    protected Shop()
    {
        // Required by EF Core
    }

    public Shop(
        Guid id,
        Guid? tenantId,
        string name,
        string slug) : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Slug = slug;
        IsActive = true;
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

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}

