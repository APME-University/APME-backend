using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace APME.Orders;

/// <summary>
/// Custom repository interface for Order aggregate
/// </summary>
public interface IOrderRepository : IRepository<Order, Guid>
{
    /// <summary>
    /// Gets an order by ID with all items included
    /// </summary>
    Task<Order?> GetWithItemsAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an order by order number
    /// </summary>
    Task<Order?> GetByOrderNumberAsync(
        string orderNumber,
        bool includeItems = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets orders for a customer with pagination
    /// </summary>
    Task<List<Order>> GetCustomerOrdersAsync(
        Guid customerId,
        int skipCount,
        int maxResultCount,
        string? sorting = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of orders for a customer
    /// </summary>
    Task<long> GetCustomerOrderCountAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the next order number (global sequence)
    /// Format: ORD-{YYYY}-{NNNNNN}
    /// </summary>
    Task<string> GetNextOrderNumberAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets orders containing items from a specific shop with filters and pagination
    /// </summary>
    Task<List<Order>> GetOrdersContainingShopAsync(
        Guid shopId,
        OrderStatus? status,
        DateTime? fromDate,
        DateTime? toDate,
        int skipCount,
        int maxResultCount,
        string? sorting = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of orders containing items from a specific shop with filters
    /// </summary>
    Task<long> GetOrderCountContainingShopAsync(
        Guid shopId,
        OrderStatus? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);
}

