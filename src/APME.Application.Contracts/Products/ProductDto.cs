using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace APME.Products;

public class ProductDto : FullAuditedEntityDto<Guid>
{
    public Guid ShopId { get; set; }

    public Guid? CategoryId { get; set; }

    public Guid? BrandId { get; set; }

    public string Name { get; set; }

    public string Slug { get; set; }

    public string? ShortDescription { get; set; }

    public string? Description { get; set; }

    public string SKU { get; set; }

    public decimal Price { get; set; }

    public decimal? CompareAtPrice { get; set; }

    public decimal? SalePrice { get; set; }

    public string Currency { get; set; } = "USD";

    public StockStatus StockStatus { get; set; } = StockStatus.InStock;

    public int StockQuantity { get; set; }

    public bool IsActive { get; set; }

    public bool IsPublished { get; set; }

    public bool IsFeatured { get; set; }

    public string? Attributes { get; set; }

    public string? PrimaryImageUrl { get; set; }

    public List<string>? ImageUrls { get; set; }

    public string? SearchableText { get; set; }

    public string? SearchHints { get; set; }

    // Navigation display
    public string? BrandName { get; set; }

    public List<string>? Tags { get; set; }

    // Computed
    public bool IsInStock => StockStatus == StockStatus.InStock || StockStatus == StockStatus.LowStock;

    public bool IsOnSale => (CompareAtPrice.HasValue && CompareAtPrice.Value > Price) || SalePrice.HasValue;

    public bool HasImages => ImageUrls != null && ImageUrls.Count > 0;

    public int ImageCount => ImageUrls?.Count ?? 0;

    public string StockStatusLabel => StockStatus.ToString();
}

