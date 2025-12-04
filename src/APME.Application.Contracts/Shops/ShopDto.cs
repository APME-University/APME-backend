using System;
using Volo.Abp.Application.Dtos;

namespace APME.Shops;

public class ShopDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; }

    public string? Description { get; set; }

    public string Slug { get; set; }

    public bool IsActive { get; set; }

    public string? LogoUrl { get; set; }

    public string? Settings { get; set; }

    public Guid? TenantId { get; set; }

    public string? TenantName { get; set; }
}

