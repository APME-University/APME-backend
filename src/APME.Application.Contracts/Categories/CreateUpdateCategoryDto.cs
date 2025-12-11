using System;
using System.ComponentModel.DataAnnotations;

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

    [StringLength(512)]
    public string? ImageUrl { get; set; }
}

