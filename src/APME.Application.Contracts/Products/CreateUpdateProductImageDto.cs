using System;
using System.ComponentModel.DataAnnotations;

namespace APME.Products;

public class CreateUpdateProductImageDto
{
    [Required]
    public Guid ProductId { get; set; }

    public Guid? VariantId { get; set; }

    [Required]
    [StringLength(2048)]
    public string Url { get; set; }

    [StringLength(512)]
    public string? AltText { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsPrimary { get; set; }
}
