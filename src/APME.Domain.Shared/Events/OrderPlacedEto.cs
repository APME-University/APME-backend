using System;
using System.Collections.Generic;
using Volo.Abp.EventBus;

namespace APME.Events;

/// <summary>
/// Event Transfer Object published when an order is successfully placed
/// Used for: notifications, analytics, cache invalidation
/// Orders are now host-level and can contain items from multiple shops
/// </summary>
[EventName("APME.Order.Placed")]
public class OrderPlacedEto
{
    /// <summary>
    /// The order ID
    /// </summary>
    public Guid OrderId { get; set; }

    /// <summary>
    /// The customer who placed the order
    /// </summary>
    public Guid CustomerId { get; set; }

    /// <summary>
    /// The shops involved in this order (multi-shop support)
    /// </summary>
    public List<Guid> ShopIds { get; set; } = new();

    /// <summary>
    /// Human-readable order number
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// Total order amount
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Currency code
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// When the order was placed
    /// </summary>
    public DateTime PlacedAt { get; set; }

    /// <summary>
    /// Customer email for notifications
    /// </summary>
    public string? CustomerEmail { get; set; }

    /// <summary>
    /// Customer name for notifications
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// Items in the order
    /// </summary>
    public List<OrderItemEto> Items { get; set; } = new();
}

/// <summary>
/// Order item information for event
/// </summary>
public class OrderItemEto
{
    /// <summary>
    /// The shop this item belongs to
    /// </summary>
    public Guid ShopId { get; set; }

    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

