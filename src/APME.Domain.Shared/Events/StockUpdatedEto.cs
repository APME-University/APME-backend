using System;
using Volo.Abp.EventBus;

namespace APME.Events;

/// <summary>
/// Event Transfer Object published when product stock is updated
/// Used for: inventory alerts, cache invalidation
/// </summary>
[EventName("APME.Stock.Updated")]
public class StockUpdatedEto
{
    /// <summary>
    /// The product ID
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// The shop the product belongs to
    /// </summary>
    public Guid ShopId { get; set; }

    /// <summary>
    /// Tenant ID for multi-tenancy
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Product name for notifications
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Product SKU
    /// </summary>
    public string ProductSku { get; set; } = string.Empty;

    /// <summary>
    /// Stock quantity before the update
    /// </summary>
    public int OldQuantity { get; set; }

    /// <summary>
    /// Stock quantity after the update
    /// </summary>
    public int NewQuantity { get; set; }

    /// <summary>
    /// The change in quantity (can be negative for deductions)
    /// </summary>
    public int QuantityChange => NewQuantity - OldQuantity;

    /// <summary>
    /// Reason for the stock update
    /// </summary>
    public StockUpdateReason Reason { get; set; }

    /// <summary>
    /// Reference ID (e.g., Order ID if updated due to order)
    /// </summary>
    public Guid? ReferenceId { get; set; }

    /// <summary>
    /// When the stock was updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Reason for stock update
/// </summary>
public enum StockUpdateReason
{
    /// <summary>
    /// Stock was manually adjusted
    /// </summary>
    ManualAdjustment = 0,

    /// <summary>
    /// Stock was deducted due to order placement
    /// </summary>
    OrderPlaced = 1,

    /// <summary>
    /// Stock was restored due to order cancellation
    /// </summary>
    OrderCancelled = 2,

    /// <summary>
    /// Stock was added from inventory receipt
    /// </summary>
    InventoryReceived = 3,

    /// <summary>
    /// Stock was adjusted due to inventory count
    /// </summary>
    InventoryCount = 4
}

