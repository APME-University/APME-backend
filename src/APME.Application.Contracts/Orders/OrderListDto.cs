using System;
using System.Collections.Generic;
using APME.Orders;
using Volo.Abp.Application.Dtos;

namespace APME.Orders;

/// <summary>
/// DTO for order list display
/// FR3.1 - View order history
/// </summary>
public class OrderListDto : EntityDto<Guid>
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
    /// Total amount
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Currency code
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Number of items in the order
    /// </summary>
    public int ItemCount { get; set; }

    /// <summary>
    /// First product image for display
    /// </summary>
    public string? FirstProductImageUrl { get; set; }

    /// <summary>
    /// Number of distinct shops in this order
    /// </summary>
    public int ShopCount { get; set; }

    /// <summary>
    /// Shop names in this order (for display)
    /// </summary>
    public List<string> ShopNames { get; set; } = new();
}

