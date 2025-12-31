using System;
using System.Collections.Generic;

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
    public string? PrimaryImageUrl { get; set; }
    public bool IsInStock { get; set; }
    public int StockQuantity { get; set; }
    public double Rating { get; set; }
    public int RatingCount { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    
    // Computed properties for UI
    public bool IsOnSale => CompareAtPrice.HasValue && CompareAtPrice.Value > Price;
    public decimal? DiscountPercent => IsOnSale ? Math.Round((1 - (Price / CompareAtPrice!.Value)) * 100, 0) : null;
    public decimal? SavingsAmount => IsOnSale ? CompareAtPrice!.Value - Price : null;
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
    public int StockQuantity { get; set; }
    public string? PrimaryImageUrl { get; set; }
    public List<string> ImageUrls { get; set; } = new();
    public string? Attributes { get; set; }
    public double Rating { get; set; }
    public int RatingCount { get; set; }
    
    // Category info
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? CategorySlug { get; set; }
    
    // Breadcrumb path
    public List<BreadcrumbItemDto> Breadcrumbs { get; set; } = new();
    
    // Related products
    public List<StoreProductListItemDto> RelatedProducts { get; set; } = new();
    
    // Computed properties
    public bool IsInStock => StockQuantity > 0;
    public bool IsOnSale => CompareAtPrice.HasValue && CompareAtPrice.Value > Price;
    public decimal? DiscountPercent => IsOnSale ? Math.Round((1 - (Price / CompareAtPrice!.Value)) * 100, 0) : null;
    public decimal? SavingsAmount => IsOnSale ? CompareAtPrice!.Value - Price : null;
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

