using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp;
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

    /// <summary>
    /// Low stock threshold for inventory alerts (FR14.5)
    /// </summary>
    public int LowStockThreshold { get; set; } = 10;

    /// <summary>
    /// Concurrency stamp for optimistic locking during stock updates
    /// </summary>
    [ConcurrencyCheck]
    public string StockConcurrencyStamp { get; protected set; } = Guid.NewGuid().ToString("N");

    #region AI/Embedding Support (RAG Architecture)

    /// <summary>
    /// Canonical, flattened JSON document for AI embedding generation.
    /// Built from product data + normalized dynamic attributes.
    /// SRS Reference: AI Chatbot - Canonical Product Document
    /// </summary>
    public string? CanonicalDocument { get; private set; }

    /// <summary>
    /// Schema version of the canonical document for forward compatibility.
    /// </summary>
    public int CanonicalDocumentVersion { get; private set; }

    /// <summary>
    /// Timestamp when the canonical document was last updated.
    /// </summary>
    public DateTime? CanonicalDocumentUpdatedAt { get; private set; }

    /// <summary>
    /// Whether an embedding has been generated for the current canonical document.
    /// Reset to false when document changes, set to true after embedding generation.
    /// </summary>
    public bool EmbeddingGenerated { get; private set; }

    #endregion

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
        UpdateStockConcurrencyStamp();
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
        UpdateStockConcurrencyStamp();
    }

    /// <summary>
    /// Atomically deducts stock with optimistic concurrency control
    /// Throws BusinessException if insufficient stock
    /// Updates ConcurrencyStamp to detect concurrent modifications
    /// </summary>
    public void DeductStockAtomic(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be positive", nameof(quantity));
        }

        if (StockQuantity < quantity)
        {
            throw new BusinessException("APME:InsufficientStock")
                .WithData("ProductId", Id)
                .WithData("ProductName", Name)
                .WithData("Available", StockQuantity)
                .WithData("Requested", quantity);
        }

        StockQuantity -= quantity;
        UpdateStockConcurrencyStamp();
    }

    /// <summary>
    /// Restores stock (e.g., when order is cancelled)
    /// </summary>
    public void RestoreStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be positive", nameof(quantity));
        }

        StockQuantity += quantity;
        UpdateStockConcurrencyStamp();
    }

    /// <summary>
    /// Checks if stock is below the low threshold
    /// </summary>
    public bool IsLowStock()
    {
        return StockQuantity <= LowStockThreshold && StockQuantity > 0;
    }

    /// <summary>
    /// Sets the low stock threshold
    /// </summary>
    public void SetLowStockThreshold(int threshold)
    {
        if (threshold < 0)
        {
            throw new ArgumentException("Threshold cannot be negative", nameof(threshold));
        }
        LowStockThreshold = threshold;
    }

    private void UpdateStockConcurrencyStamp()
    {
        StockConcurrencyStamp = Guid.NewGuid().ToString("N");
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

    #region Canonical Document Methods

    /// <summary>
    /// Updates the canonical document for AI embedding generation.
    /// Should be called after any product data change that affects embeddings.
    /// </summary>
    /// <param name="document">The built canonical document.</param>
    public void UpdateCanonicalDocument(CanonicalProductDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        CanonicalDocument = document.ToJson();
        CanonicalDocumentVersion = document.SchemaVersion;
        CanonicalDocumentUpdatedAt = DateTime.UtcNow;
        EmbeddingGenerated = false; // Mark for re-embedding
    }

    /// <summary>
    /// Gets the canonical document as a parsed object.
    /// Returns null if no document exists or parsing fails.
    /// </summary>
    public CanonicalProductDocument? GetCanonicalDocument()
    {
        return CanonicalProductDocument.FromJson(CanonicalDocument);
    }

    /// <summary>
    /// Marks that embedding has been generated for the current canonical document.
    /// Called by the embedding pipeline after successful embedding generation.
    /// </summary>
    public void MarkEmbeddingGenerated()
    {
        EmbeddingGenerated = true;
    }

    /// <summary>
    /// Checks if the canonical document needs to be regenerated based on schema version.
    /// </summary>
    /// <param name="currentSchemaVersion">The current schema version.</param>
    /// <returns>True if document needs regeneration.</returns>
    public bool NeedsCanonicalDocumentUpdate(int currentSchemaVersion)
    {
        return string.IsNullOrWhiteSpace(CanonicalDocument) || 
               CanonicalDocumentVersion < currentSchemaVersion;
    }

    #endregion
}

