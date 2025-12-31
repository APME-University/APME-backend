using System;
using System.Collections.Generic;
using APME.Checkout;
using APME.Orders;
using Volo.Abp.Application.Dtos;

namespace APME.Orders;

/// <summary>
/// DTO for order details display
/// FR3.3 - View order details
/// </summary>
public class OrderDetailsDto : EntityDto<Guid>
{
    /// <summary>
    /// Human-readable order number
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the order
    /// </summary>
    public OrderStatus Status { get; set; }

    /// <summary>
    /// Status display name
    /// </summary>
    public string StatusDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// When the order was placed
    /// </summary>
    public DateTime CreationTime { get; set; }

    /// <summary>
    /// Order line items
    /// </summary>
    public List<OrderItemDto> Items { get; set; } = new();

    /// <summary>
    /// Shipping address
    /// </summary>
    public AddressDto ShippingAddress { get; set; } = null!;

    /// <summary>
    /// Billing address (null if same as shipping)
    /// </summary>
    public AddressDto? BillingAddress { get; set; }

    /// <summary>
    /// Payment information
    /// </summary>
    public PaymentInfoDto Payment { get; set; } = null!;

    /// <summary>
    /// Subtotal before tax and shipping
    /// </summary>
    public decimal SubTotal { get; set; }

    /// <summary>
    /// Tax amount
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Shipping amount
    /// </summary>
    public decimal ShippingAmount { get; set; }

    /// <summary>
    /// Discount amount
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// Total amount
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Currency code
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Customer notes
    /// </summary>
    public string? CustomerNotes { get; set; }

    /// <summary>
    /// Tracking number for shipment
    /// </summary>
    public string? TrackingNumber { get; set; }

    /// <summary>
    /// Shipping carrier
    /// </summary>
    public string? ShippingCarrier { get; set; }

    /// <summary>
    /// When the order was shipped
    /// </summary>
    public DateTime? ShippedAt { get; set; }

    /// <summary>
    /// When the order was delivered
    /// </summary>
    public DateTime? DeliveredAt { get; set; }

    /// <summary>
    /// Whether the order can be cancelled
    /// </summary>
    public bool CanBeCancelled { get; set; }

    /// <summary>
    /// Number of distinct shops in this order
    /// </summary>
    public int ShopCount { get; set; }

    /// <summary>
    /// Items grouped by shop for display
    /// </summary>
    public List<OrderShopGroupDto> ShopGroups { get; set; } = new();
}

/// <summary>
/// DTO for grouping order items by shop
/// </summary>
public class OrderShopGroupDto
{
    /// <summary>
    /// Shop ID
    /// </summary>
    public Guid ShopId { get; set; }

    /// <summary>
    /// Shop name
    /// </summary>
    public string ShopName { get; set; } = string.Empty;

    /// <summary>
    /// Items from this shop
    /// </summary>
    public List<OrderItemDto> Items { get; set; } = new();

    /// <summary>
    /// Subtotal for this shop's items
    /// </summary>
    public decimal SubTotal { get; set; }
}

/// <summary>
/// DTO for order item display
/// </summary>
public class OrderItemDto : EntityDto<Guid>
{
    /// <summary>
    /// Shop ID this item belongs to
    /// </summary>
    public Guid ShopId { get; set; }

    /// <summary>
    /// Shop name
    /// </summary>
    public string ShopName { get; set; } = string.Empty;

    /// <summary>
    /// Product ID
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Product name at time of order
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Product SKU at time of order
    /// </summary>
    public string ProductSku { get; set; } = string.Empty;

    /// <summary>
    /// Product image URL
    /// </summary>
    public string? ProductImageUrl { get; set; }

    /// <summary>
    /// Quantity ordered
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Unit price at time of order
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Discount applied to this item
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// Tax for this item
    /// </summary>
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Line total
    /// </summary>
    public decimal LineTotal { get; set; }
}

/// <summary>
/// DTO for payment information display
/// </summary>
public class PaymentInfoDto
{
    /// <summary>
    /// Payment method used
    /// </summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Last 4 digits of card
    /// </summary>
    public string? CardLast4 { get; set; }

    /// <summary>
    /// Card brand
    /// </summary>
    public string? CardBrand { get; set; }

    /// <summary>
    /// Payment status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Amount charged
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// When the payment was processed
    /// </summary>
    public DateTime ProcessedAt { get; set; }
}

