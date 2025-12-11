using System;
using Volo.Abp.Application.Dtos;

namespace APME.Categories;

public class CategoryDto : FullAuditedEntityDto<Guid>
{
    public Guid ShopId { get; set; }

    public string Name { get; set; }

    public string Slug { get; set; }

    public string? Description { get; set; }

    public Guid? ParentId { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public string? ImageUrl { get; set; }
}

