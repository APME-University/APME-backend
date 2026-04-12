using System;
using System.ComponentModel.DataAnnotations;

namespace APME.Products;

public class CreateUpdateProductVariantDto
{
    [Required]
    public Guid ProductId { get; set; }

    [Required]
    [StringLength(128)]
    public string SKU { get; set; }

    [Required]
    [StringLength(256)]
    public string Name { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? Price { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? SalePrice { get; set; }

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    public StockStatus StockStatus { get; set; } = StockStatus.InStock;

    public bool IsDefault { get; set; }

    [StringLength(4000)]
    public string? VariantAttributesJson { get; set; }
}
