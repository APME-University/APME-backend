using System;
using Volo.Abp.Domain.Entities;

namespace APME.Orders;

/// <summary>
/// Represents an item in an order
/// Immutable after creation - serves as a historical snapshot of the purchase
/// </summary>
public class OrderItem : Entity<Guid>
{
    public Guid OrderId { get; private set; }

    /// <summary>
    /// The shop this item belongs to (supports multi-shop orders)
    /// </summary>
    public Guid ShopId { get; private set; }

    /// <summary>
    /// Reference to the product (may be deleted in future)
    /// </summary>
    public Guid ProductId { get; private set; }

    /// <summary>
    /// Product name at time of order (snapshot)
    /// </summary>
    public string ProductName { get; private set; }

    /// <summary>
    /// Product SKU at time of order (snapshot)
    /// </summary>
    public string ProductSku { get; private set; }

    /// <summary>
    /// Product image URL at time of order (snapshot)
    /// </summary>
    public string? ProductImageUrl { get; private set; }

    /// <summary>
    /// Quantity ordered
    /// </summary>
    public int Quantity { get; private set; }

    /// <summary>
    /// Unit price at time of order
    /// </summary>
    public decimal UnitPrice { get; private set; }

    /// <summary>
    /// Discount amount applied to this item
    /// </summary>
    public decimal DiscountAmount { get; private set; }

    /// <summary>
    /// Tax amount for this item
    /// </summary>
    public decimal TaxAmount { get; private set; }

    /// <summary>
    /// Total price for this line item (quantity * unitPrice - discount + tax)
    /// </summary>
    public decimal LineTotal { get; private set; }

    protected OrderItem()
    {
        // Required by EF Core
    }

    public OrderItem(
        Guid id,
        Guid orderId,
        Guid shopId,
        Guid productId,
        string productName,
        string productSku,
        int quantity,
        decimal unitPrice,
        decimal discountAmount = 0,
        decimal taxAmount = 0,
        string? productImageUrl = null) : base(id)
    {
        if (shopId == Guid.Empty)
            throw new ArgumentException("Shop ID is required", nameof(shopId));
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));
        if (unitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative", nameof(unitPrice));
        if (discountAmount < 0)
            throw new ArgumentException("Discount cannot be negative", nameof(discountAmount));
        if (taxAmount < 0)
            throw new ArgumentException("Tax cannot be negative", nameof(taxAmount));
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("Product name is required", nameof(productName));
        if (string.IsNullOrWhiteSpace(productSku))
            throw new ArgumentException("Product SKU is required", nameof(productSku));

        OrderId = orderId;
        ShopId = shopId;
        ProductId = productId;
        ProductName = productName.Trim();
        ProductSku = productSku.Trim();
        ProductImageUrl = productImageUrl?.Trim();
        Quantity = quantity;
        UnitPrice = unitPrice;
        DiscountAmount = discountAmount;
        TaxAmount = taxAmount;
        
        // Calculate line total
        LineTotal = CalculateLineTotal();
    }

    private decimal CalculateLineTotal()
    {
        return (UnitPrice * Quantity) - DiscountAmount + TaxAmount;
    }

    /// <summary>
    /// Gets the subtotal before discount and tax
    /// </summary>
    public decimal GetSubTotal()
    {
        return UnitPrice * Quantity;
    }
}

