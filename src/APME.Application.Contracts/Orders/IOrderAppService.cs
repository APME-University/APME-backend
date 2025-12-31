using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace APME.Orders;

/// <summary>
/// Application service interface for order operations
/// All operations require authenticated customer
/// </summary>
public interface IOrderAppService : IApplicationService
{
    /// <summary>
    /// Gets the customer's order list with pagination
    /// FR3.1 - View order history
    /// </summary>
    Task<PagedResultDto<OrderListDto>> GetListAsync(GetOrderListInput input);

    /// <summary>
    /// Gets order details by ID
    /// FR3.3 - View order details
    /// </summary>
    Task<OrderDetailsDto> GetAsync(Guid id);

    /// <summary>
    /// Gets order details by order number
    /// FR3.3 - View order details
    /// </summary>
    Task<OrderDetailsDto> GetByOrderNumberAsync(string orderNumber);

    /// <summary>
    /// Cancels an order (if cancellable)
    /// </summary>
    Task<OrderDetailsDto> CancelAsync(Guid id, string reason);

    /// <summary>
    /// Gets the count of orders for the current customer
    /// </summary>
    Task<int> GetCountAsync(Guid? shopId = null);
}

