using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace APME.Carts;

/// <summary>
/// DTO for displaying cart information to customers
/// Cart is at host level - one per customer, can contain items from multiple shops
/// </summary>
public class CartViewDto : EntityDto<Guid>
{
    /// <summary>
    /// Items in the cart (can be from multiple shops)
    /// </summary>
    public List<CartItemViewDto> Items { get; set; } = new();

    /// <summary>
    /// Items grouped by shop for display purposes
    /// </summary>
    public List<CartShopGroupDto> ShopGroups { get; set; } = new();

    /// <summary>
    /// Total number of individual items (sum of quantities)
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Number of unique products in cart
    /// </summary>
    public int UniqueItemCount { get; set; }

    /// <summary>
    /// Number of unique shops in cart
    /// </summary>
    public int ShopCount { get; set; }

    /// <summary>
    /// Subtotal before tax and shipping
    /// </summary>
    public decimal SubTotal { get; set; }

    /// <summary>
    /// Whether any items are out of stock or have insufficient quantity
    /// </summary>
    public bool HasOutOfStockItems { get; set; }

    /// <summary>
    /// Whether the cart is empty
    /// </summary>
    public bool IsEmpty { get; set; }

    /// <summary>
    /// Customer notes for the order
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// When the cart was last modified
    /// </summary>
    public DateTime? LastModificationTime { get; set; }
}

/// <summary>
/// DTO for grouping cart items by shop
/// </summary>
public class CartShopGroupDto
{
    /// <summary>
    /// The shop ID
    /// </summary>
    public Guid ShopId { get; set; }

    /// <summary>
    /// The shop name
    /// </summary>
    public string ShopName { get; set; } = string.Empty;

    /// <summary>
    /// Items from this shop
    /// </summary>
    public List<CartItemViewDto> Items { get; set; } = new();

    /// <summary>
    /// Subtotal for this shop's items
    /// </summary>
    public decimal SubTotal { get; set; }

    /// <summary>
    /// Total item count from this shop
    /// </summary>
    public int ItemCount { get; set; }
}

/// <summary>
/// DTO for displaying a cart item to customers
/// </summary>
public class CartItemViewDto : EntityDto<Guid>
{
    /// <summary>
    /// The shop this item belongs to
    /// </summary>
    public Guid ShopId { get; set; }

    /// <summary>
    /// The shop name for display
    /// </summary>
    public string ShopName { get; set; } = string.Empty;

    /// <summary>
    /// Reference to the product
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Product name
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Product SKU
    /// </summary>
    public string ProductSku { get; set; } = string.Empty;

    /// <summary>
    /// Product image URL
    /// </summary>
    public string? ProductImageUrl { get; set; }

    /// <summary>
    /// Product slug for navigation
    /// </summary>
    public string? ProductSlug { get; set; }

    /// <summary>
    /// Unit price
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Current price from product (may differ from cart price if product price changed)
    /// </summary>
    public decimal CurrentPrice { get; set; }

    /// <summary>
    /// Quantity in cart
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Line total (quantity * unitPrice)
    /// </summary>
    public decimal LineTotal { get; set; }

    /// <summary>
    /// Available stock quantity
    /// </summary>
    public int AvailableStock { get; set; }

    /// <summary>
    /// Whether the product is in stock
    /// </summary>
    public bool IsInStock { get; set; }

    /// <summary>
    /// Whether the requested quantity exceeds available stock
    /// </summary>
    public bool ExceedsStock { get; set; }

    /// <summary>
    /// Whether the price has changed since item was added
    /// </summary>
    public bool PriceChanged { get; set; }

    /// <summary>
    /// Whether the product is still active and published
    /// </summary>
    public bool IsProductAvailable { get; set; }
}
