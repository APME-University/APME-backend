using System;
using Volo.Abp.Domain.Entities;

namespace APME.Carts;

/// <summary>
/// Represents an item in a shopping cart
/// Each item belongs to a specific shop, allowing multi-shop cart
/// </summary>
public class CartItem : Entity<Guid>
{
    public Guid CartId { get; private set; }

    /// <summary>
    /// The shop this item belongs to
    /// Allows cart to contain items from multiple shops
    /// </summary>
    public Guid ShopId { get; private set; }

    public Guid ProductId { get; private set; }

    public int Quantity { get; private set; }

    /// <summary>
    /// Price at the time item was added (for display purposes)
    /// </summary>
    public decimal UnitPrice { get; private set; }

    /// <summary>
    /// Product name snapshot (for display when product might be unavailable)
    /// </summary>
    public string ProductName { get; private set; }

    /// <summary>
    /// Product SKU snapshot
    /// </summary>
    public string ProductSku { get; private set; }

    /// <summary>
    /// Product image URL snapshot
    /// </summary>
    public string? ProductImageUrl { get; private set; }

    protected CartItem()
    {
        // Required by EF Core
        ProductName = string.Empty;
        ProductSku = string.Empty;
    }

    internal CartItem(
        Guid id,
        Guid cartId,
        Guid shopId,
        Guid productId,
        string productName,
        string productSku,
        decimal unitPrice,
        int quantity,
        string? productImageUrl = null) : base(id)
    {
        CartId = cartId;
        ShopId = shopId;
        ProductId = productId;
        ProductName = productName;
        ProductSku = productSku;
        UnitPrice = unitPrice;
        ProductImageUrl = productImageUrl;
        SetQuantity(quantity);
    }

    /// <summary>
    /// Updates the quantity of the cart item
    /// </summary>
    public void SetQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));
        }

        Quantity = quantity;
    }

    /// <summary>
    /// Updates the unit price (when product price changes)
    /// </summary>
    internal void UpdatePrice(decimal unitPrice)
    {
        if (unitPrice < 0)
        {
            throw new ArgumentException("Unit price cannot be negative", nameof(unitPrice));
        }

        UnitPrice = unitPrice;
    }

    /// <summary>
    /// Updates product information (name, image)
    /// </summary>
    internal void UpdateProductInfo(string productName, string? productImageUrl)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            throw new ArgumentException("Product name cannot be empty", nameof(productName));
        }

        ProductName = productName;
        ProductImageUrl = productImageUrl;
    }

    /// <summary>
    /// Calculates the line total for this item
    /// </summary>
    public decimal GetLineTotal()
    {
        return UnitPrice * Quantity;
    }
}
