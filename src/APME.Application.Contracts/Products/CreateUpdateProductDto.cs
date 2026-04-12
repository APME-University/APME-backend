using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Content;

namespace APME.Products;

public class CreateUpdateProductDto
{
    [Required]
    public Guid ShopId { get; set; }

    public Guid? CategoryId { get; set; }

    public Guid? BrandId { get; set; }

    [Required]
    [StringLength(256)]
    public string Name { get; set; }

    [Required]
    [StringLength(256)]
    public string Slug { get; set; }

    [StringLength(512)]
    public string? ShortDescription { get; set; }

    [StringLength(4000)]
    public string? Description { get; set; }

    [Required]
    [StringLength(128)]
    public string SKU { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? CompareAtPrice { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? SalePrice { get; set; }

    [StringLength(3)]
    public string Currency { get; set; } = "USD";

    public StockStatus StockStatus { get; set; } = StockStatus.InStock;

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    public bool IsActive { get; set; }

    public bool IsPublished { get; set; }

    public bool IsFeatured { get; set; }

    public string? Attributes { get; set; }

    [StringLength(1000)]
    public string? SearchHints { get; set; }

    // Support both single image (backward compatibility) and multiple images (Court pattern)
    public IRemoteStreamContent? Image { get; set; }
    public IList<IRemoteStreamContent>? Images { get; set; }
}

