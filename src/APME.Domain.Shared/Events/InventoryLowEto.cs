using System;
using Volo.Abp.EventBus;

namespace APME.Events;

/// <summary>
/// Event Transfer Object published when stock falls below threshold (FR14.5)
/// Used for: admin notifications, automated reorder triggers
/// </summary>
[EventName("APME.Inventory.Low")]
public class InventoryLowEto
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
    /// Current stock quantity
    /// </summary>
    public int CurrentQuantity { get; set; }

    /// <summary>
    /// The threshold that was crossed
    /// </summary>
    public int ThresholdQuantity { get; set; }

    /// <summary>
    /// Severity level based on how low the stock is
    /// </summary>
    public InventoryAlertSeverity Severity { get; set; }

    /// <summary>
    /// When the alert was triggered
    /// </summary>
    public DateTime AlertedAt { get; set; }

    /// <summary>
    /// Product image URL for notifications
    /// </summary>
    public string? ProductImageUrl { get; set; }
}

/// <summary>
/// Severity level for inventory alerts
/// </summary>
public enum InventoryAlertSeverity
{
    /// <summary>
    /// Stock is low but not critical
    /// </summary>
    Low = 0,

    /// <summary>
    /// Stock is critically low
    /// </summary>
    Critical = 1,

    /// <summary>
    /// Product is out of stock
    /// </summary>
    OutOfStock = 2
}

