using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APME.Costing;
using APME.Orders;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Timing;
using Volo.Abp.Uow;

namespace APME.PricingAdvisor;

/// <summary>
/// Measures the actual sales outcome in the window after an applied recommendation — the closed-loop
/// feedback the paper's evaluation uses. Scheduled (delayed) by the RecommendationApplied handler.
/// </summary>
public class OutcomeTrackingWorker : ITransientDependency
{
    private readonly IRepository<PricingRecommendation, Guid> _recommendations;
    private readonly IRepository<RecommendationOutcome, Guid> _outcomes;
    private readonly IRepository<OrderItem, Guid> _orderItems;
    private readonly IRepository<Order, Guid> _orders;
    private readonly ICostingService _costing;
    private readonly ICurrentTenant _currentTenant;
    private readonly IGuidGenerator _guid;
    private readonly IClock _clock;
    private readonly ILogger<OutcomeTrackingWorker> _logger;

    public OutcomeTrackingWorker(
        IRepository<PricingRecommendation, Guid> recommendations,
        IRepository<RecommendationOutcome, Guid> outcomes,
        IRepository<OrderItem, Guid> orderItems,
        IRepository<Order, Guid> orders,
        ICostingService costing,
        ICurrentTenant currentTenant,
        IGuidGenerator guid,
        IClock clock,
        ILogger<OutcomeTrackingWorker> logger)
    {
        _recommendations = recommendations;
        _outcomes = outcomes;
        _orderItems = orderItems;
        _orders = orders;
        _costing = costing;
        _currentTenant = currentTenant;
        _guid = guid;
        _clock = clock;
        _logger = logger;
    }

    [UnitOfWork]
    public virtual async Task MeasureAsync(Guid? tenantId, Guid recommendationId)
    {
        using (_currentTenant.Change(tenantId))
        {
            var rec = await _recommendations.FindAsync(recommendationId);
            if (rec?.AppliedAt == null)
            {
                return;
            }

            var start = rec.AppliedAt.Value;
            var end = start.AddDays(30);

            var items = await _orderItems.GetListAsync(x => x.ProductId == rec.ProductId && x.ShopId == rec.ShopId);
            var orderIds = items.Select(i => i.OrderId).Distinct().ToList();
            var orders = await _orders.GetListAsync(o =>
                orderIds.Contains(o.Id) && o.CreationTime >= start && o.CreationTime < end);

            var validIds = orders
                .Where(o => o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Refunded && o.Status != OrderStatus.PaymentFailed)
                .Select(o => o.Id)
                .ToHashSet();

            var sold = items.Where(i => validIds.Contains(i.OrderId)).ToList();
            var units = sold.Sum(i => i.Quantity);
            var revenue = sold.Sum(i => i.Quantity * i.UnitPrice);
            var landed = await _costing.GetLandedUnitCostAsync(rec.ProductId);

            var outcome = new RecommendationOutcome(_guid.Create(), tenantId, recommendationId, rec.ProductId, rec.ShopId, start, end)
            {
                ActualUnits = units,
                ActualRevenue = revenue,
                ActualProfit = revenue - units * landed,
                MeasuredAt = _clock.Now
            };

            await _outcomes.InsertAsync(outcome, autoSave: true);
            _logger.LogInformation("Recorded outcome for recommendation {Id}: units={Units} revenue={Revenue}", recommendationId, units, revenue);
        }
    }
}
