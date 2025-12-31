using System;
using APME.Orders;
using Volo.Abp.Application.Dtos;

namespace APME.Orders;

/// <summary>
/// Input for getting customer order list with pagination
/// </summary>
public class GetOrderListInput : PagedAndSortedResultRequestDto
{
    /// <summary>
    /// Filter by shop (optional)
    /// </summary>
    public Guid? ShopId { get; set; }

    /// <summary>
    /// Filter by status (optional)
    /// </summary>
    public OrderStatus? Status { get; set; }

    /// <summary>
    /// Filter by date range - from (optional)
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Filter by date range - to (optional)
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Search by order number (optional)
    /// </summary>
    public string? OrderNumber { get; set; }
}

