using System;
using Volo.Abp.Application.Dtos;

namespace APME.Products;

public class BrandDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; }

    public string Slug { get; set; }

    public string? Description { get; set; }

    public string? LogoUrl { get; set; }

    public bool IsActive { get; set; }

    public int ProductCount { get; set; }
}
