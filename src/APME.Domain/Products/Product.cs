using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace APME.Products;

public class Product : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public Guid ShopId { get; set; } // FK to Shop

    public Guid? CategoryId { get; set; } // FK to Category

    public string Name { get; set; }

    public string Slug { get; set; }

    public string? Description { get; set; }

    public string SKU { get; set; }

    public decimal Price { get; set; }

    public decimal? CompareAtPrice { get; set; }

    public int StockQuantity { get; set; }

    public bool IsActive { get; set; }

    public bool IsPublished { get; set; }

    public string? Attributes { get; set; } // JSONB for dynamic attributes (EAV pattern)

    public string? PrimaryImageUrl { get; set; } // Main product image

    public string? ImageUrls { get; set; } // JSON array of image URLs for gallery

    protected Product()
    {
        // Required by EF Core
    }

    public Product(
        Guid id,
        Guid? tenantId,
        Guid shopId,
        string name,
        string slug,
        string sku,
        decimal price) : base(id)
    {
        TenantId = tenantId;
        ShopId = shopId;
        Name = name;
        Slug = slug;
        SKU = sku;
        Price = price;
        StockQuantity = 0;
        IsActive = true;
        IsPublished = false;
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be null or empty", nameof(name));
        }
        Name = name;
    }

    public void UpdateSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ArgumentException("Slug cannot be null or empty", nameof(slug));
        }
        Slug = slug;
    }

    public void UpdateSKU(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new ArgumentException("SKU cannot be null or empty", nameof(sku));
        }
        SKU = sku;
    }

    public void UpdatePrice(decimal price)
    {
        if (price < 0)
        {
            throw new ArgumentException("Price cannot be negative", nameof(price));
        }
        Price = price;
    }

    public void UpdateCompareAtPrice(decimal? compareAtPrice)
    {
        if (compareAtPrice.HasValue && compareAtPrice.Value < 0)
        {
            throw new ArgumentException("Compare at price cannot be negative", nameof(compareAtPrice));
        }
        CompareAtPrice = compareAtPrice;
    }

    public void UpdateStockQuantity(int stockQuantity)
    {
        if (stockQuantity < 0)
        {
            throw new ArgumentException("Stock quantity cannot be negative", nameof(stockQuantity));
        }
        StockQuantity = stockQuantity;
    }

    public void IncreaseStock(int quantity)
    {
        if (quantity < 0)
        {
            throw new ArgumentException("Quantity cannot be negative", nameof(quantity));
        }
        StockQuantity += quantity;
    }

    public void DecreaseStock(int quantity)
    {
        if (quantity < 0)
        {
            throw new ArgumentException("Quantity cannot be negative", nameof(quantity));
        }
        if (StockQuantity < quantity)
        {
            throw new InvalidOperationException("Insufficient stock");
        }
        StockQuantity -= quantity;
    }

    public void SetCategory(Guid? categoryId)
    {
        CategoryId = categoryId;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Publish()
    {
        IsPublished = true;
    }

    public void Unpublish()
    {
        IsPublished = false;
    }

    public bool IsInStock()
    {
        return StockQuantity > 0;
    }

    public bool IsOnSale()
    {
        return CompareAtPrice.HasValue && CompareAtPrice.Value > Price;
    }

    public void SetPrimaryImage(string imageUrl)
    {
        PrimaryImageUrl = imageUrl;
    }

    public void AddImage(string imageUrl)
    {
        var imageList = GetImageList();
        if (!imageList.Contains(imageUrl))
        {
            imageList.Add(imageUrl);
            ImageUrls = System.Text.Json.JsonSerializer.Serialize(imageList);
        }
    }

    public void RemoveImage(string imageUrl)
    {
        var imageList = GetImageList();
        imageList.Remove(imageUrl);
        ImageUrls = imageList.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(imageList) : null;
        
        // If removed image was primary, clear primary image
        if (PrimaryImageUrl == imageUrl)
        {
            PrimaryImageUrl = imageList.Count > 0 ? imageList[0] : null;
        }
    }

    public List<string> GetImageList()
    {
        if (string.IsNullOrWhiteSpace(ImageUrls))
        {
            return new List<string>();
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(ImageUrls) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}

