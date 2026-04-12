using System;
using System.ComponentModel.DataAnnotations;

namespace APME.Products;

public class CreateUpdateProductTagDto
{
    [Required]
    public Guid ProductId { get; set; }

    [Required]
    [StringLength(128)]
    public string Tag { get; set; }
}
