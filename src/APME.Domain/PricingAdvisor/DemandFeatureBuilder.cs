using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APME.Orders;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;

namespace APME.PricingAdvisor;

public record DemandContext(
    IReadOnlyList<DemandHistoryPoint> History,
    AdvisorMode Mode,
    decimal TotalUnits,
    decimal RecentUnits,
    decimal AverageUnits,
    int PriceChangeCount);

public interface IDemandFeatureBuilder : IDomainService
{
    Task<DemandContext> BuildAsync(Guid productId, Guid shopId);
}

/// <summary>
/// Builds the monthly demand sequence for a product from existing OrderItem + Order (host-level),
/// and determines the data-sufficiency mode. Excludes cancelled / refunded / payment-failed orders.
/// </summary>
public class DemandFeatureBuilder : DomainService, IDemandFeatureBuilder
{
    private readonly IRepository<OrderItem, Guid> _orderItems;
    private readonly IRepository<Order, Guid> _orders;

    public DemandFeatureBuilder(IRepository<OrderItem, Guid> orderItems, IRepository<Order, Guid> orders)
    {
        _orderItems = orderItems;
        _orders = orders;
    }

    public async Task<DemandContext> BuildAsync(Guid productId, Guid shopId)
    {
        var empty = new DemandContext(new List<DemandHistoryPoint>(), AdvisorMode.ColdStart, 0m, 0m, 0m, 0);

        var items = await _orderItems.GetListAsync(x => x.ProductId == productId && x.ShopId == shopId);
        if (items.Count == 0)
        {
            return empty;
        }

        var orderIds = items.Select(i => i.OrderId).Distinct().ToList();
        var orders = await _orders.GetListAsync(o => orderIds.Contains(o.Id));
        var orderById = orders.ToDictionary(o => o.Id);

        static bool Valid(OrderStatus s) =>
            s != OrderStatus.Cancelled && s != OrderStatus.Refunded && s != OrderStatus.PaymentFailed;

        var points = items
            .Where(i => orderById.ContainsKey(i.OrderId) && Valid(orderById[i.OrderId].Status))
            .Select(i => new { orderById[i.OrderId].CreationTime, i.Quantity, i.UnitPrice })
            .GroupBy(x => new DateTime(x.CreationTime.Year, x.CreationTime.Month, 1))
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var units = g.Sum(x => x.Quantity);
                var revenue = g.Sum(x => x.Quantity * x.UnitPrice);
                var price = units > 0 ? revenue / units : 0m;
                return new DemandHistoryPoint(g.Key, units, price);
            })
            .ToList();

        if (points.Count == 0)
        {
            return empty;
        }

        var total = points.Sum(p => p.Units);
        var recent = points[^1].Units;
        var avg = points.Average(p => p.Units);

        var priceChanges = 0;
        for (var i = 1; i < points.Count; i++)
        {
            var prev = points[i - 1].Price;
            if (prev > 0m && Math.Abs((points[i].Price - prev) / prev) > 0.01m)
            {
                priceChanges++;
            }
        }

        return new DemandContext(points, DetermineMode((int)total, points.Count, priceChanges), total, recent, avg, priceChanges);
    }

    private static AdvisorMode DetermineMode(int totalUnits, int months, int priceChanges)
    {
        if (totalUnits == 0) return AdvisorMode.ColdStart;
        if (totalUnits < 20) return AdvisorMode.EarlySales;
        if (totalUnits >= 100 && priceChanges >= 2) return AdvisorMode.FullPriceResponse;
        if (totalUnits >= 50 && months >= 6) return AdvisorMode.Standard;
        return AdvisorMode.EarlySales;
    }
}
