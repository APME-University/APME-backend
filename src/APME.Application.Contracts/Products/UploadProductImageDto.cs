using System;
using System.ComponentModel.DataAnnotations;

namespace APME.Products;

public class UploadProductImageDto
{
    [Required]
    public Guid ProductId { get; set; }

    public bool IsPrimary { get; set; }
}




