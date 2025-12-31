using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Content;

namespace APME.Categories;

public class CreateUpdateCategoryDto
{
    [Required]
    public Guid ShopId { get; set; }

    [Required]
    [StringLength(256)]
    public string Name { get; set; }

    [Required]
    [StringLength(256)]
    public string Slug { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    public Guid? ParentId { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public IRemoteStreamContent? Image { get; set; }
}

