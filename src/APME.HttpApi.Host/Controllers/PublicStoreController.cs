using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APME.BlobStorage;
using APME.Categories;
using APME.Products;
using APME.PublicStore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Volo.Abp.Application.Dtos;
using Volo.Abp.BlobStoring;
using Volo.Abp.BlobStoring.FileSystem;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;

namespace APME.Controllers;

/// <summary>
/// Public Store API Controller
/// Provides read-optimized, customer-facing APIs for the storefront
/// No authentication required for browsing products and categories
/// </summary>
[ApiController]
[Route("api/public-store")]
[AllowAnonymous]
public class PublicStoreController : APMEController
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<Category, Guid> _categoryRepository;
    private readonly IConfiguration _configuration;
    private readonly DefaultBlobContainerConfigurationProvider _blobContainerConfigurationProvider;
    private readonly DefaultBlobFilePathCalculator _defaultBlobFilePathCalculator;
    private readonly IDataFilter _filter;

    public PublicStoreController(
        IRepository<Product, Guid> productRepository,
        IRepository<Category, Guid> categoryRepository,
        IConfiguration configuration,
        DefaultBlobContainerConfigurationProvider blobContainerConfigurationProvider,
        DefaultBlobFilePathCalculator defaultBlobFilePathCalculator,
        IDataFilter filter)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _configuration = configuration;
        _blobContainerConfigurationProvider = blobContainerConfigurationProvider;
        _defaultBlobFilePathCalculator = defaultBlobFilePathCalculator;
        _filter = filter;
    }

    #region Homepage APIs

    /// <summary>
    /// Get homepage data including featured products, categories, and sales
    /// </summary>
    [HttpGet("homepage")]
    public async Task<ActionResult<StoreHomepageDto>> GetHomepage(int featuredCount = 8, int saleCount = 6, int latestCount = 8)
    {
        using (_filter.Disable<IMultiTenant>()) { 
            var products = await _productRepository.GetListAsync(p => p.IsActive && p.IsPublished);
        var categories = await _categoryRepository.GetListAsync(c => c.IsActive);

        var homepage = new StoreHomepageDto
        {
            FeaturedProducts = products
                .OrderByDescending(p => p.StockQuantity > 0)
                .ThenBy(p => Guid.NewGuid()) // Random for variety
                .Take(featuredCount)
                .Select(MapToListItem)
                .ToList(),
                
            OnSaleProducts = products
                .Where(p => p.CompareAtPrice.HasValue && p.CompareAtPrice > p.Price)
                .OrderByDescending(p => (p.CompareAtPrice!.Value - p.Price) / p.CompareAtPrice.Value)
                .Take(saleCount)
                .Select(MapToListItem)
                .ToList(),
                
            LatestProducts = products
                .OrderByDescending(p => p.CreationTime)
                .Take(latestCount)
                .Select(MapToListItem)
                .ToList(),
                
            FeaturedCategories = categories
                .Where(c => c.ParentId == null)
                .OrderBy(c => c.DisplayOrder)
                .Take(6)
                .Select(c => new StoreFeaturedCategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    ImageUrl = GetFullImageUrl(c.ImageUrl),
                    ProductCount = products.Count(p => p.CategoryId == c.Id),
                    FeaturedProducts = products
                        .Where(p => p.CategoryId == c.Id)
                        .Take(4)
                        .Select(MapToListItem)
                        .ToList()
                })
                .ToList()
        };

        return Ok(homepage);
        }
    }

    /// <summary>
    /// Get featured products for homepage carousel/grid
    /// </summary>
    [HttpGet("products/featured")]
    public async Task<ActionResult<List<StoreProductListItemDto>>> GetFeaturedProducts(int maxCount = 8)
    {
        using (_filter.Disable<IMultiTenant>())
        {

            var products = await _productRepository.GetListAsync(p => p.IsActive && p.IsPublished);

            var featured = products
                .OrderByDescending(p => p.StockQuantity > 0)
                .ThenBy(p => Guid.NewGuid())
                .Take(maxCount)
                .Select(MapToListItem)
                .ToList();

            return Ok(featured);
        }
    }

    /// <summary>
    /// Get products on sale with discounts
    /// </summary>
    [HttpGet("products/on-sale")]
    public async Task<ActionResult<List<StoreProductListItemDto>>> GetOnSaleProducts(int maxCount = 12)
    {
        using (_filter.Disable<IMultiTenant>())
        {

            var products = await _productRepository.GetListAsync(p =>
            p.IsActive && p.IsPublished &&
            p.CompareAtPrice.HasValue && p.CompareAtPrice > p.Price);

            var onSale = products
                .OrderByDescending(p => (p.CompareAtPrice!.Value - p.Price) / p.CompareAtPrice.Value)
                .Take(maxCount)
                .Select(MapToListItem)
                .ToList();

            return Ok(onSale);
        }
    }

    /// <summary>
    /// Get latest/newest products
    /// </summary>
    [HttpGet("products/latest")]
    public async Task<ActionResult<List<StoreProductListItemDto>>> GetLatestProducts(int maxCount = 12)
    {
        using (_filter.Disable<IMultiTenant>())
        {

            var products = await _productRepository.GetListAsync(p => p.IsActive && p.IsPublished);

            var latest = products
                .OrderByDescending(p => p.CreationTime)
                .Take(maxCount)
                .Select(MapToListItem)
                .ToList();

            return Ok(latest);
        }
    }

    #endregion

    #region Product APIs

    /// <summary>
    /// Search and filter products with pagination
    /// </summary>
    [HttpGet("products")]
    public async Task<ActionResult<StoreProductListResultDto>> SearchProducts([FromQuery] StoreProductSearchInput input)
    {
        using (_filter.Disable<IMultiTenant>())
        {

            var allProducts = await _productRepository.GetListAsync(p => p.IsActive && p.IsPublished);
            var categories = await _categoryRepository.GetListAsync(c => c.IsActive);

            // Apply filters
            var query = allProducts.AsQueryable();

            if (!string.IsNullOrWhiteSpace(input.SearchTerm))
            {
                var term = input.SearchTerm.ToLower();
                query = query.Where(p =>
                    (p.Name != null && p.Name.ToLower().Contains(term)) ||
                    (p.Description != null && p.Description.ToLower().Contains(term)) ||
                    (p.SKU != null && p.SKU.ToLower().Contains(term)));
            }

            if (input.CategoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == input.CategoryId.Value);
            }

            if (input.MinPrice.HasValue)
            {
                query = query.Where(p => p.Price >= input.MinPrice.Value);
            }

            if (input.MaxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= input.MaxPrice.Value);
            }

            if (input.InStockOnly == true)
            {
                query = query.Where(p => p.StockQuantity > 0);
            }

            if (input.OnSaleOnly == true)
            {
                query = query.Where(p => p.CompareAtPrice.HasValue && p.CompareAtPrice > p.Price);
            }

            // Apply sorting
            query = input.SortBy?.ToLower() switch
            {
                "name-asc" => query.OrderBy(p => p.Name),
                "name-desc" => query.OrderByDescending(p => p.Name),
                "price-asc" => query.OrderBy(p => p.Price),
                "price-desc" => query.OrderByDescending(p => p.Price),
                "newest" => query.OrderByDescending(p => p.CreationTime),
                _ => query.OrderByDescending(p => p.CreationTime)
            };

            var totalCount = query.Count();
            var totalPages = (int)Math.Ceiling((double)totalCount / input.MaxResultCount);
            var currentPage = (input.SkipCount / input.MaxResultCount) + 1;

            var items = query
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount)
                .Select(p => MapToListItemWithCategory(p, categories))
                .ToList();

            // Calculate price range for filters
            var priceRange = new StorePriceRangeDto
            {
                Min = allProducts.Any() ? allProducts.Min(p => p.Price) : 0,
                Max = allProducts.Any() ? allProducts.Max(p => p.Price) : 0
            };

            // Calculate category counts for filters
            var categoryFilters = categories
                .Where(c => c.ParentId == null)
                .Select(c => new StoreCategoryFilterDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    ProductCount = allProducts.Count(p => p.CategoryId == c.Id)
                })
                .Where(c => c.ProductCount > 0)
                .OrderByDescending(c => c.ProductCount)
                .ToList();

            return Ok(new StoreProductListResultDto
            {
                Items = items,
                TotalCount = totalCount,
                CurrentPage = currentPage,
                TotalPages = totalPages,
                PriceRange = priceRange,
                CategoryFilters = categoryFilters
            });
        }
    }

    /// <summary>
    /// Get product detail by ID
    /// </summary>
    [HttpGet("products/{id:guid}")]
    public async Task<ActionResult<StoreProductDetailDto>> GetProductById(Guid id)
    {
        using (_filter.Disable<IMultiTenant>())
        {

            var product = await _productRepository.FindAsync(id);

            if (product == null || !product.IsActive || !product.IsPublished)
            {
                return NotFound(new { error = "Product not found" });
            }

            return Ok(await MapToDetailDto(product));
        }
    }

    /// <summary>
    /// Get product detail by slug
    /// </summary>
    [HttpGet("products/by-slug/{slug}")]
    public async Task<ActionResult<StoreProductDetailDto>> GetProductBySlug(string slug)
    {
        using (_filter.Disable<IMultiTenant>())
        {

            var products = await _productRepository.GetListAsync(p =>
            p.Slug == slug && p.IsActive && p.IsPublished);

            var product = products.FirstOrDefault();

            if (product == null)
            {
                return NotFound(new { error = "Product not found" });
            }

            return Ok(await MapToDetailDto(product));
        }
    }

    /// <summary>
    /// Get products by category ID
    /// </summary>
    [HttpGet("categories/{categoryId:guid}/products")]
    public async Task<ActionResult<StoreProductListResultDto>> GetProductsByCategory(
        Guid categoryId, 
        [FromQuery] StoreProductSearchInput input)
    {
        using (_filter.Disable<IMultiTenant>())
        {

            input.CategoryId = categoryId;
            return await SearchProducts(input);
        }
    }

    #endregion

    #region Category APIs

    /// <summary>
    /// Get all active categories for navigation
    /// </summary>
    [HttpGet("categories")]
    public async Task<ActionResult<List<StoreCategoryNavDto>>> GetCategories()
    {
        using (_filter.Disable<IMultiTenant>())
        {

            var categories = await _categoryRepository.GetListAsync(c => c.IsActive);
            var products = await _productRepository.GetListAsync(p => p.IsActive && p.IsPublished);

            var rootCategories = categories
                .Where(c => c.ParentId == null)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => MapToCategoryNav(c, categories, products))
                .ToList();

            return Ok(rootCategories);
        }
    }

    /// <summary>
    /// Get top/featured categories with product counts
    /// </summary>
    [HttpGet("categories/top")]
    public async Task<ActionResult<List<StoreCategoryNavDto>>> GetTopCategories(int maxCount = 6)
    {
        using (_filter.Disable<IMultiTenant>())
        {

            var categories = await _categoryRepository.GetListAsync(c => c.IsActive && c.ParentId == null);
            var products = await _productRepository.GetListAsync(p => p.IsActive && p.IsPublished);

            var topCategories = categories
                .OrderBy(c => c.DisplayOrder)
                .Take(maxCount)
                .Select(c => new StoreCategoryNavDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    ImageUrl = GetFullImageUrl(c.ImageUrl),
                    ProductCount = products.Count(p => p.CategoryId == c.Id)
                })
                .ToList();

            return Ok(topCategories);
        }
    }

    /// <summary>
    /// Get category with products by ID
    /// </summary>
    [HttpGet("categories/{id:guid}")]
    public async Task<ActionResult<StoreCategoryWithProductsDto>> GetCategoryById(
        Guid id, 
        [FromQuery] StoreProductSearchInput? productInput = null)
    {
        using (_filter.Disable<IMultiTenant>())
        {

            var category = await _categoryRepository.FindAsync(id);

            if (category == null || !category.IsActive)
            {
                return NotFound(new { error = "Category not found" });
            }

            return Ok(await MapToCategoryWithProducts(category, productInput ?? new StoreProductSearchInput()));
        }
    }

    /// <summary>
    /// Get category with products by slug
    /// </summary>
    [HttpGet("categories/by-slug/{slug}")]
    public async Task<ActionResult<StoreCategoryWithProductsDto>> GetCategoryBySlug(
        string slug,
        [FromQuery] StoreProductSearchInput? productInput = null)
    {
        using (_filter.Disable<IMultiTenant>())
        {

            var categories = await _categoryRepository.GetListAsync(c => c.Slug == slug && c.IsActive);
            var category = categories.FirstOrDefault();

            if (category == null)
            {
                return NotFound(new { error = "Category not found" });
            }

            return Ok(await MapToCategoryWithProducts(category, productInput ?? new StoreProductSearchInput()));
        }
    }

    /// <summary>
    /// Get category lookup for filters and dropdowns
    /// </summary>
    [HttpGet("categories/lookup")]
    public async Task<ActionResult<List<StoreCategoryFilterDto>>> GetCategoryLookup()
    {
        using (_filter.Disable<IMultiTenant>())
        {

            var categories = await _categoryRepository.GetListAsync(c => c.IsActive);
            var products = await _productRepository.GetListAsync(p => p.IsActive && p.IsPublished);

            var lookup = categories
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new StoreCategoryFilterDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    ProductCount = products.Count(p => p.CategoryId == c.Id)
                })
                .ToList();

            return Ok(lookup);
        }
    }

    #endregion

    #region Private Mapping Methods

    private StoreProductListItemDto MapToListItem(Product product)
    {
        using (_filter.Disable<IMultiTenant>())
        {

            return new StoreProductListItemDto
            {
                Id = product.Id,
                Name = product.Name,
                Slug = product.Slug,
                ShortDescription = TruncateDescription(product.Description, 150),
                Price = product.Price,
                CompareAtPrice = product.CompareAtPrice,
                PrimaryImageUrl = GetFullImageUrl(product.PrimaryImageUrl),
                IsInStock = product.StockQuantity > 0,
                StockQuantity = product.StockQuantity,
                Rating = 0, // TODO: Implement ratings
                RatingCount = 0,
                CategoryId = product.CategoryId
            };
        }
    }

    private StoreProductListItemDto MapToListItemWithCategory(Product product, List<Category> categories)
    {
        using (_filter.Disable<IMultiTenant>())
        {

            var item = MapToListItem(product);
            var category = categories.FirstOrDefault(c => c.Id == product.CategoryId);
            item.CategoryName = category?.Name;
            return item;
        }
    }

    private async Task<StoreProductDetailDto> MapToDetailDto(Product product)
    {
        using (_filter.Disable<IMultiTenant>())
        {

            var categories = await _categoryRepository.GetListAsync(c => c.IsActive);
            var category = categories.FirstOrDefault(c => c.Id == product.CategoryId);

            // Get related products from same category
            var relatedProducts = (await _productRepository.GetListAsync(p =>
                p.IsActive && p.IsPublished &&
                p.CategoryId == product.CategoryId &&
                p.Id != product.Id))
                .Take(4)
                .Select(MapToListItem)
                .ToList();

            // Build breadcrumbs
            var breadcrumbs = new List<BreadcrumbItemDto>
        {
            new() { Name = "Home", Url = "/store", IsActive = false },
            new() { Name = "Products", Url = "/store/products", IsActive = false }
        };

            if (category != null)
            {
                breadcrumbs.Add(new BreadcrumbItemDto
                {
                    Name = category.Name,
                    Slug = category.Slug,
                    Url = $"/store/category/{category.Id}",
                    IsActive = false
                });
            }

            breadcrumbs.Add(new BreadcrumbItemDto
            {
                Name = product.Name,
                IsActive = true
            });

            return new StoreProductDetailDto
            {
                Id = product.Id,
                Name = product.Name,
                Slug = product.Slug,
                Description = product.Description,
                ShortDescription = TruncateDescription(product.Description, 150),
                SKU = product.SKU,
                Price = product.Price,
                CompareAtPrice = product.CompareAtPrice,
                StockQuantity = product.StockQuantity,
                PrimaryImageUrl = GetFullImageUrl(product.PrimaryImageUrl),
                ImageUrls = GetImageUrlList(product),
                Attributes = product.Attributes,
                Rating = 0, // TODO: Implement ratings
                RatingCount = 0,
                CategoryId = product.CategoryId,
                CategoryName = category?.Name,
                CategorySlug = category?.Slug,
                Breadcrumbs = breadcrumbs,
                RelatedProducts = relatedProducts
            };
        }
    }

    private StoreCategoryNavDto MapToCategoryNav(Category category, List<Category> allCategories, List<Product> allProducts)
    {
        using (_filter.Disable<IMultiTenant>())
        {

            return new StoreCategoryNavDto
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                ImageUrl = GetFullImageUrl(category.ImageUrl),
                ProductCount = allProducts.Count(p => p.CategoryId == category.Id),
                Children = allCategories
                .Where(c => c.ParentId == category.Id)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => MapToCategoryNav(c, allCategories, allProducts))
                .ToList()
            };
        }
    }

    private async Task<StoreCategoryWithProductsDto> MapToCategoryWithProducts(Category category, StoreProductSearchInput input)
    {
        using (_filter.Disable<IMultiTenant>())
        {

            var categories = await _categoryRepository.GetListAsync(c => c.IsActive);
            var allProducts = await _productRepository.GetListAsync(p => p.IsActive && p.IsPublished);

            var parent = category.ParentId.HasValue
                ? categories.FirstOrDefault(c => c.Id == category.ParentId)
                : null;

            // Build breadcrumbs
            var breadcrumbs = new List<BreadcrumbItemDto>
        {
            new() { Name = "Home", Url = "/store", IsActive = false },
            new() { Name = "Products", Url = "/store/products", IsActive = false }
        };

            if (parent != null)
            {
                breadcrumbs.Add(new BreadcrumbItemDto
                {
                    Name = parent.Name,
                    Slug = parent.Slug,
                    Url = $"/store/category/{parent.Id}",
                    IsActive = false
                });
            }

            breadcrumbs.Add(new BreadcrumbItemDto
            {
                Name = category.Name,
                Slug = category.Slug,
                IsActive = true
            });

            // Get products for this category
            input.CategoryId = category.Id;
            var productsResult = await GetProductsInternal(input, allProducts, categories);

            return new StoreCategoryWithProductsDto
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                Description = category.Description,
                ImageUrl = GetFullImageUrl(category.ImageUrl),
                ParentId = parent?.Id,
                ParentName = parent?.Name,
                ParentSlug = parent?.Slug,
                SubCategories = categories
                    .Where(c => c.ParentId == category.Id)
                    .OrderBy(c => c.DisplayOrder)
                    .Select(c => new StoreCategoryNavDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Slug = c.Slug,
                        ImageUrl = GetFullImageUrl(c.ImageUrl),
                        ProductCount = allProducts.Count(p => p.CategoryId == c.Id)
                    })
                    .ToList(),
                Breadcrumbs = breadcrumbs,
                Products = productsResult
            };
        }
    }

    private Task<StoreProductListResultDto> GetProductsInternal(
        StoreProductSearchInput input, 
        List<Product> allProducts, 
        List<Category> categories)
    {
        using (_filter.Disable<IMultiTenant>())
        {

            var query = allProducts.AsQueryable();

            if (input.CategoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == input.CategoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(input.SearchTerm))
            {
                var term = input.SearchTerm.ToLower();
                query = query.Where(p =>
                    (p.Name != null && p.Name.ToLower().Contains(term)) ||
                    (p.Description != null && p.Description.ToLower().Contains(term)));
            }

            if (input.MinPrice.HasValue)
            {
                query = query.Where(p => p.Price >= input.MinPrice.Value);
            }

            if (input.MaxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= input.MaxPrice.Value);
            }

            if (input.InStockOnly == true)
            {
                query = query.Where(p => p.StockQuantity > 0);
            }

            if (input.OnSaleOnly == true)
            {
                query = query.Where(p => p.CompareAtPrice.HasValue && p.CompareAtPrice > p.Price);
            }

            // Apply sorting
            query = input.SortBy?.ToLower() switch
            {
                "name-asc" => query.OrderBy(p => p.Name),
                "name-desc" => query.OrderByDescending(p => p.Name),
                "price-asc" => query.OrderBy(p => p.Price),
                "price-desc" => query.OrderByDescending(p => p.Price),
                "newest" => query.OrderByDescending(p => p.CreationTime),
                _ => query.OrderByDescending(p => p.CreationTime)
            };

            var totalCount = query.Count();
            var totalPages = (int)Math.Ceiling((double)totalCount / input.MaxResultCount);
            var currentPage = (input.SkipCount / input.MaxResultCount) + 1;

            var items = query
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount)
                .Select(p => MapToListItemWithCategory(p, categories))
                .ToList();

            return Task.FromResult(new StoreProductListResultDto
            {
                Items = items,
                TotalCount = totalCount,
                CurrentPage = currentPage,
                TotalPages = totalPages
            });
        }
    }

    private string? GetFullImageUrl(string? blobName)
    {
        using (_filter.Disable<IMultiTenant>())
        {

            if (string.IsNullOrWhiteSpace(blobName))
            {
                return null;
            }

            // If already a full URL, return as-is
            if (blobName.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return blobName;
            }

            try
            {
                using (CurrentTenant.Change(null))
                {
                    var containerName = BlobContainerNameAttribute.GetContainerName<ImageContainer>();
                    var blobContainerConfiguration = _blobContainerConfigurationProvider.Get(containerName);
                    var physicalImagePath = _defaultBlobFilePathCalculator.Calculate(
                        new BlobProviderGetArgs(containerName, blobContainerConfiguration, blobName));
                    int wwwrootIndex = physicalImagePath.IndexOf("wwwroot", StringComparison.OrdinalIgnoreCase);
                    string relativePath = physicalImagePath.Substring(wwwrootIndex + "wwwroot".Length);
                    return $"{_configuration.GetSection("App")["SelfUrl"]}{relativePath}";
                }
            }
            catch
            {
                return null;
            }
        }
    }

    private List<string> GetImageUrlList(Product product)
    {
        using (_filter.Disable<IMultiTenant>())
        {

            var imageList = product.GetImageList();
            return imageList
                .Select(GetFullImageUrl)
                .Where(url => url != null)
                .Select(url => url!)
                .ToList();
        }
    }

    private static string? TruncateDescription(string? description, int maxLength)
    {

        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        if (description.Length <= maxLength)
        {
            return description;
        }

        return description.Substring(0, maxLength).TrimEnd() + "...";
    }

    #endregion
}

