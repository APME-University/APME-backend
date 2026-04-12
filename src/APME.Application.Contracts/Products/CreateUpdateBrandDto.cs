using System;
using System.ComponentModel.DataAnnotations;

namespace APME.Products;

public class CreateUpdateBrandDto
{
    [Required]
    [StringLength(256)]
    public string Name { get; set; }

    [Required]
    [StringLength(256)]
    public string Slug { get; set; }

    [StringLength(4000)]
    public string? Description { get; set; }

    [StringLength(1024)]
    public string? LogoUrl { get; set; }

    public bool IsActive { get; set; } = true;
}
