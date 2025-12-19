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

    [Required]
    [StringLength(256)]
    public string Name { get; set; }

    [Required]
    [StringLength(256)]
    public string Slug { get; set; }

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

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    public bool IsActive { get; set; }

    public bool IsPublished { get; set; }

    public string? Attributes { get; set; }

    // Support both single image (backward compatibility) and multiple images (Court pattern)
    public IRemoteStreamContent? Image { get; set; }
    public IList<IRemoteStreamContent>? Images { get; set; }
}

