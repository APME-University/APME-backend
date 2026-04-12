using System;
using System.Collections.Generic;
using APME.Products;

namespace APME.PublicStore;

/// <summary>
/// Lightweight product DTO for list views (grid, search results)
/// Optimized for frontend rendering performance
/// </summary>
public class StoreProductListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public decimal? SalePrice { get; set; }
    public string Currency { get; set; } = "USD";
    public string? PrimaryImageUrl { get; set; }
    public bool IsInStock { get; set; }
    public StockStatus StockStatus { get; set; } = StockStatus.InStock;
    public int StockQuantity { get; set; }
    public double Rating { get; set; }
    public int RatingCount { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? BrandName { get; set; }
    public bool IsFeatured { get; set; }
    public List<string>? Tags { get; set; }
    
    // Computed properties for UI
    public bool IsOnSale => (CompareAtPrice.HasValue && CompareAtPrice.Value > Price) || SalePrice.HasValue;
    public decimal? DiscountPercent => CompareAtPrice.HasValue && CompareAtPrice.Value > Price ? Math.Round((1 - (Price / CompareAtPrice!.Value)) * 100, 0) : null;
    public decimal? SavingsAmount => CompareAtPrice.HasValue && CompareAtPrice.Value > Price ? CompareAtPrice!.Value - Price : null;
    public string StockStatusLabel => StockStatus.ToString();
}

/// <summary>
/// Full product DTO for detail page
/// Contains all information needed for product page rendering
/// </summary>
public class StoreProductDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public string SKU { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public decimal? SalePrice { get; set; }
    public string Currency { get; set; } = "USD";
    public StockStatus StockStatus { get; set; } = StockStatus.InStock;
    public int StockQuantity { get; set; }
    public string? PrimaryImageUrl { get; set; }
    public List<string> ImageUrls { get; set; } = new();
    public string? Attributes { get; set; }
    public double Rating { get; set; }
    public int RatingCount { get; set; }
    
    // Brand info
    public Guid? BrandId { get; set; }
    public string? BrandName { get; set; }
    
    // Category info
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? CategorySlug { get; set; }
    
    // Featured & Tags
    public bool IsFeatured { get; set; }
    public List<string>? Tags { get; set; }
    
    // Variants & structured attribute values
    public List<StoreProductVariantDto> Variants { get; set; } = new();
    public List<StoreProductAttributeValueDto> AttributeValues { get; set; } = new();
    
    // Breadcrumb path
    public List<BreadcrumbItemDto> Breadcrumbs { get; set; } = new();
    
    // Related products
    public List<StoreProductListItemDto> RelatedProducts { get; set; } = new();
    
    // Computed properties
    public bool IsInStock => StockStatus == StockStatus.InStock || StockStatus == StockStatus.LowStock;
    public bool IsOnSale => (CompareAtPrice.HasValue && CompareAtPrice.Value > Price) || SalePrice.HasValue;
    public decimal? DiscountPercent => CompareAtPrice.HasValue && CompareAtPrice.Value > Price ? Math.Round((1 - (Price / CompareAtPrice!.Value)) * 100, 0) : null;
    public decimal? SavingsAmount => CompareAtPrice.HasValue && CompareAtPrice.Value > Price ? CompareAtPrice!.Value - Price : null;
    public string StockStatusLabel => StockStatus.ToString();
}

/// <summary>
/// Breadcrumb item for navigation
/// </summary>
public class BreadcrumbItemDto
{
    public string Name { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Url { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Input for product search and filtering
/// </summary>
public class StoreProductSearchInput
{
    public string? SearchTerm { get; set; }
    public Guid? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool? InStockOnly { get; set; }
    public bool? OnSaleOnly { get; set; }
    public string? SortBy { get; set; } // name-asc, name-desc, price-asc, price-desc, newest, rating
    public int SkipCount { get; set; } = 0;
    public int MaxResultCount { get; set; } = 24;
}

/// <summary>
/// Paginated result for product listing
/// </summary>
public class StoreProductListResultDto
{
    public List<StoreProductListItemDto> Items { get; set; } = new();
    public long TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
    
    // Filter summary for UI
    public StorePriceRangeDto? PriceRange { get; set; }
    public List<StoreCategoryFilterDto>? CategoryFilters { get; set; }
}

/// <summary>
/// Price range for filter UI
/// </summary>
public class StorePriceRangeDto
{
    public decimal Min { get; set; }
    public decimal Max { get; set; }
}

/// <summary>
/// Category filter item with product count
/// </summary>
public class StoreCategoryFilterDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int ProductCount { get; set; }
}

/// <summary>
/// Product variant for detail page (size, color, etc.)
/// </summary>
public class StoreProductVariantDto
{
    public Guid Id { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? SalePrice { get; set; }
    public int StockQuantity { get; set; }
    public StockStatus StockStatus { get; set; } = StockStatus.InStock;
    public bool IsDefault { get; set; }
    public string? VariantAttributesJson { get; set; }
}

/// <summary>
/// Structured attribute value for product detail page (EAV)
/// </summary>
public class StoreProductAttributeValueDto
{
    public string AttributeName { get; set; } = string.Empty;
    public string? AttributeDisplayName { get; set; }
    public string? TextValue { get; set; }
    public double? NumericValue { get; set; }
    public bool? BoolValue { get; set; }
    public string? DisplayValue { get; set; }
}
