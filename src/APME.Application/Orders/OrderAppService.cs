using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APME.Checkout;
using APME.Customers;
using APME.Events;
using APME.Products;
using APME.Shops;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Users;

namespace APME.Orders;

/// <summary>
/// Application service for order operations
/// Orders are host-level and support multi-shop (ShopId is on OrderItem)
/// </summary>
[Authorize]
public class OrderAppService : ApplicationService, IOrderAppService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly IRepository<Shop, Guid> _shopRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IDistributedEventBus _eventBus;
    private readonly ICurrentUser _currentUser;
    private readonly IDataFilter _dataFilter;

    public OrderAppService(
        IOrderRepository orderRepository,
        IRepository<Customer, Guid> customerRepository,
        IRepository<Shop, Guid> shopRepository,
        IRepository<Product, Guid> productRepository,
        IDistributedEventBus eventBus,
        ICurrentUser currentUser,
        IDataFilter dataFilter)
    {
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _shopRepository = shopRepository;
        _productRepository = productRepository;
        _eventBus = eventBus;
        _currentUser = currentUser;
        _dataFilter = dataFilter;
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<OrderListDto>> GetListAsync(GetOrderListInput input)
    {
        var customerId = await GetCurrentCustomerIdAsync();

        var orders = await _orderRepository.GetCustomerOrdersAsync(
            customerId,
            input.SkipCount,
            input.MaxResultCount,
            input.Sorting);

        var totalCount = await _orderRepository.GetCustomerOrderCountAsync(customerId);

        // Get shop names from all order items (multi-shop support)
        var shopIds = orders.SelectMany(o => o.Items.Select(i => i.ShopId)).Distinct().ToList();
        List<Shop> shops;
        using (_dataFilter.Disable<IMultiTenant>())
        {
            shops = shopIds.Any()
                ? await _shopRepository.GetListAsync(s => shopIds.Contains(s.Id))
                : new List<Shop>();
        }
        var shopMap = shops.ToDictionary(s => s.Id, s => s.Name);

        var dtos = orders.Select(order =>
        {
            var orderShopIds = order.Items.Select(i => i.ShopId).Distinct().ToList();
            var orderShopNames = orderShopIds
                .Select(id => shopMap.GetValueOrDefault(id, "Unknown Shop"))
                .ToList();

            return new OrderListDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                Status = order.Status,
                StatusDisplayName = GetStatusDisplayName(order.Status),
                CreationTime = order.CreationTime,
                TotalAmount = order.TotalAmount,
                Currency = order.Currency,
                ItemCount = order.GetTotalItemCount(),
                FirstProductImageUrl = order.Items.FirstOrDefault()?.ProductImageUrl,
                ShopCount = orderShopIds.Count,
                ShopNames = orderShopNames
            };
        }).ToList();

        return new PagedResultDto<OrderListDto>(totalCount, dtos);
    }

    /// <inheritdoc />
    public async Task<OrderDetailsDto> GetAsync(Guid id)
    {
        var customerId = await GetCurrentCustomerIdAsync();
        var order = await _orderRepository.GetWithItemsAsync(id);

        if (order == null || order.CustomerId != customerId)
        {
            throw new BusinessException("APME:OrderNotFound")
                .WithData("OrderId", id);
        }

        return await MapToOrderDetailsDtoAsync(order);
    }

    /// <inheritdoc />
    public async Task<OrderDetailsDto> GetByOrderNumberAsync(string orderNumber)
    {
        var customerId = await GetCurrentCustomerIdAsync();
        var order = await _orderRepository.GetByOrderNumberAsync(orderNumber);

        if (order == null || order.CustomerId != customerId)
        {
            throw new BusinessException("APME:OrderNotFound")
                .WithData("OrderNumber", orderNumber);
        }

        return await MapToOrderDetailsDtoAsync(order);
    }

    /// <inheritdoc />
    public async Task<OrderDetailsDto> CancelAsync(Guid id, string reason)
    {
        var customerId = await GetCurrentCustomerIdAsync();
        var order = await _orderRepository.GetWithItemsAsync(id);

        if (order == null || order.CustomerId != customerId)
        {
            throw new BusinessException("APME:OrderNotFound")
                .WithData("OrderId", id);
        }

        if (!order.CanBeCancelled())
        {
            throw new BusinessException("APME:CannotCancelOrder")
                .WithData("OrderId", id)
                .WithData("Status", order.Status);
        }

        // Cancel the order
        order.Cancel(reason);

        // Restore stock for each item
        foreach (var item in order.Items)
        {
            Product? product;
            using (_dataFilter.Disable<IMultiTenant>())
            {
                product = await _productRepository.FindAsync(item.ProductId);
            }

            if (product != null)
            {
                var oldQuantity = product.StockQuantity;
                product.RestoreStock(item.Quantity);

                using (_dataFilter.Disable<IMultiTenant>())
                {
                    await _productRepository.UpdateAsync(product);
                }

                // Publish stock updated event with item's ShopId
                await _eventBus.PublishAsync(new StockUpdatedEto
                {
                    ProductId = product.Id,
                    ShopId = item.ShopId, // Use item's ShopId (multi-shop support)
                    TenantId = null, // Host-level
                    ProductName = product.Name,
                    ProductSku = product.SKU,
                    OldQuantity = oldQuantity,
                    NewQuantity = product.StockQuantity,
                    Reason = StockUpdateReason.OrderCancelled,
                    ReferenceId = order.Id,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        await _orderRepository.UpdateAsync(order);

        return await MapToOrderDetailsDtoAsync(order);
    }

    /// <inheritdoc />
    public async Task<int> GetCountAsync(Guid? shopId = null)
    {
        var customerId = await GetCurrentCustomerIdAsync();
        var count = await _orderRepository.GetCustomerOrderCountAsync(customerId);
        return (int)count;
    }

    #region Helper Methods

    private async Task<Guid> GetCurrentCustomerIdAsync()
    {
        if (!_currentUser.Id.HasValue)
        {
            throw new BusinessException("APME:NotAuthenticated");
        }

        // In this system, the Customer ID is the same as the ABP User ID
        var customer = await _customerRepository.FindAsync(_currentUser.Id.Value);

        if (customer == null)
        {
            throw new BusinessException("APME:CustomerNotFound");
        }

        return customer.Id;
    }

    private async Task<OrderDetailsDto> MapToOrderDetailsDtoAsync(Order order)
    {
        // Get shop names for all items (multi-shop support)
        var shopIds = order.Items.Select(i => i.ShopId).Distinct().ToList();
        List<Shop> shops;
        using (_dataFilter.Disable<IMultiTenant>())
        {
            shops = shopIds.Any()
                ? await _shopRepository.GetListAsync(s => shopIds.Contains(s.Id))
                : new List<Shop>();
        }
        var shopMap = shops.ToDictionary(s => s.Id, s => s.Name);

        // Map items with shop info
        var itemDtos = order.Items.Select(item => new OrderItemDto
        {
            Id = item.Id,
            ShopId = item.ShopId,
            ShopName = shopMap.GetValueOrDefault(item.ShopId, "Unknown Shop"),
            ProductId = item.ProductId,
            ProductName = item.ProductName,
            ProductSku = item.ProductSku,
            ProductImageUrl = item.ProductImageUrl,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            DiscountAmount = item.DiscountAmount,
            TaxAmount = item.TaxAmount,
            LineTotal = item.LineTotal
        }).ToList();

        // Group items by shop
        var shopGroups = itemDtos
            .GroupBy(i => i.ShopId)
            .Select(g => new OrderShopGroupDto
            {
                ShopId = g.Key,
                ShopName = g.First().ShopName,
                Items = g.ToList(),
                SubTotal = g.Sum(i => i.LineTotal)
            })
            .ToList();

        return new OrderDetailsDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status,
            StatusDisplayName = GetStatusDisplayName(order.Status),
            CreationTime = order.CreationTime,
            Items = itemDtos,
            ShopGroups = shopGroups,
            ShopCount = shopIds.Count,
            ShippingAddress = MapToAddressDto(order.ShippingAddress),
            BillingAddress = order.BillingAddress != null ? MapToAddressDto(order.BillingAddress) : null,
            Payment = new PaymentInfoDto
            {
                Method = order.Payment.Method.ToString(),
                CardLast4 = order.Payment.CardLast4,
                CardBrand = order.Payment.CardBrand,
                Status = order.Payment.Status.ToString(),
                Amount = order.Payment.Amount,
                ProcessedAt = order.Payment.ProcessedAt
            },
            SubTotal = order.SubTotal,
            TaxAmount = order.TaxAmount,
            ShippingAmount = order.ShippingAmount,
            DiscountAmount = order.DiscountAmount,
            TotalAmount = order.TotalAmount,
            Currency = order.Currency,
            CustomerNotes = order.CustomerNotes,
            TrackingNumber = order.TrackingNumber,
            ShippingCarrier = order.ShippingCarrier,
            ShippedAt = order.ShippedAt,
            DeliveredAt = order.DeliveredAt,
            CanBeCancelled = order.CanBeCancelled()
        };
    }

    private AddressDto MapToAddressDto(Address address)
    {
        return new AddressDto
        {
            FullName = address.FullName,
            Street = address.Street,
            Street2 = address.Street2,
            City = address.City,
            State = address.State,
            PostalCode = address.PostalCode,
            Country = address.Country,
            Phone = address.Phone
        };
    }

    private string GetStatusDisplayName(OrderStatus status)
    {
        return status switch
        {
            OrderStatus.Pending => "Pending",
            OrderStatus.PaymentConfirmed => "Payment Confirmed",
            OrderStatus.Processing => "Processing",
            OrderStatus.Shipped => "Shipped",
            OrderStatus.Delivered => "Delivered",
            OrderStatus.Cancelled => "Cancelled",
            OrderStatus.Refunded => "Refunded",
            OrderStatus.PaymentFailed => "Payment Failed",
            _ => status.ToString()
        };
    }

    #endregion
}

