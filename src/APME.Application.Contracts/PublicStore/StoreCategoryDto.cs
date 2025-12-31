using System;
using System.Collections.Generic;

namespace APME.PublicStore;

/// <summary>
/// Category DTO for navigation (lightweight)
/// </summary>
public class StoreCategoryNavDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int ProductCount { get; set; }
    public List<StoreCategoryNavDto> Children { get; set; } = new();
}

/// <summary>
/// Category DTO for listing page with products
/// </summary>
public class StoreCategoryWithProductsDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    
    // Parent category for breadcrumbs
    public Guid? ParentId { get; set; }
    public string? ParentName { get; set; }
    public string? ParentSlug { get; set; }
    
    // Subcategories
    public List<StoreCategoryNavDto> SubCategories { get; set; } = new();
    
    // Breadcrumb path
    public List<BreadcrumbItemDto> Breadcrumbs { get; set; } = new();
    
    // Product list result
    public StoreProductListResultDto Products { get; set; } = new();
}

/// <summary>
/// Featured category for homepage display
/// </summary>
public class StoreFeaturedCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int ProductCount { get; set; }
    public List<StoreProductListItemDto> FeaturedProducts { get; set; } = new();
}

/// <summary>
/// Homepage data aggregate
/// </summary>
public class StoreHomepageDto
{
    public List<StoreFeaturedCategoryDto> FeaturedCategories { get; set; } = new();
    public List<StoreProductListItemDto> FeaturedProducts { get; set; } = new();
    public List<StoreProductListItemDto> LatestProducts { get; set; } = new();
    public List<StoreProductListItemDto> OnSaleProducts { get; set; } = new();
    public List<StoreProductListItemDto> BestSellers { get; set; } = new();
}

