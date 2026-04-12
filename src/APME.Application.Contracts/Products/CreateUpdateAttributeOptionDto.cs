using System;
using System.ComponentModel.DataAnnotations;

namespace APME.Products;

public class CreateUpdateAttributeOptionDto
{
    [Required]
    public Guid ProductAttributeId { get; set; }

    [Required]
    [StringLength(256)]
    public string Value { get; set; }

    [Required]
    [StringLength(256)]
    public string DisplayValue { get; set; }

    public int DisplayOrder { get; set; }
}
