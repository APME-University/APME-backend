using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APME.Costing;
using APME.Products;
using APME.Shops;
using Hangfire;
using Microsoft.Extensions.Logging;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Timing;
using Volo.Abp.Uow;

namespace APME.PricingAdvisor;

/// <summary>
/// Scans each shop's products and upserts <see cref="PricingAttentionItem"/> snapshots that back the
/// Attention Center. Runs weekly (see the recurring registration in the Host module).
/// </summary>
public class AttentionDetectionWorker : ITransientDependency
{
    private readonly IRepository<Product, Guid> _products;
    private readonly IRepository<PricingAttentionItem, Guid> _attention;
    private readonly IRepository<Shop, Guid> _shops;
    private readonly IRepository<CompetitorPrice, Guid> _competitors;
    private readonly IDemandFeatureBuilder _features;
    private readonly IScenarioClassifier _classifier;
    private readonly ICostingService _costing;
    private readonly ICurrentTenant _currentTenant;
    private readonly IDataFilter _dataFilter;
    private readonly IGuidGenerator _guid;
    private readonly IClock _clock;
    private readonly ILogger<AttentionDetectionWorker> _logger;

    public AttentionDetectionWorker(
        IRepository<Product, Guid> products,
        IRepository<PricingAttentionItem, Guid> attention,
        IRepository<Shop, Guid> shops,
        IRepository<CompetitorPrice, Guid> competitors,
        IDemandFeatureBuilder features,
        IScenarioClassifier classifier,
        ICostingService costing,
        ICurrentTenant currentTenant,
        IDataFilter dataFilter,
        IGuidGenerator guid,
        IClock clock,
        ILogger<AttentionDetectionWorker> logger)
    {
        _products = products;
        _attention = attention;
        _shops = shops;
        _competitors = competitors;
        _features = features;
        _classifier = classifier;
        _costing = costing;
        _currentTenant = currentTenant;
        _dataFilter = dataFilter;
        _guid = guid;
        _clock = clock;
        _logger = logger;
    }

    /// <summary>Recurring entry point: fan out one job per active shop across all tenants.</summary>
    [UnitOfWork]
    public virtual async Task RunAllAsync()
    {
        List<Shop> shops;
        using (_dataFilter.Disable<IMultiTenant>())
        {
            shops = await _shops.GetListAsync(s => s.IsActive);
        }

        foreach (var shop in shops)
        {
            BackgroundJob.Enqueue<AttentionDetectionWorker>(w => w.RunForShopAsync(shop.TenantId, shop.Id));
        }

        _logger.LogInformation("Pricing attention detection dispatched for {Count} shops", shops.Count);
    }

    [UnitOfWork]
    public virtual async Task RunForShopAsync(Guid? tenantId, Guid shopId)
    {
        using (_currentTenant.Change(tenantId))
        {
            var products = await _products.GetListAsync(p => p.ShopId == shopId && p.IsActive);

            foreach (var product in products)
            {
                var demand = await _features.BuildAsync(product.Id, shopId);
                var landed = await _costing.GetLandedUnitCostAsync(product.Id);
                var competitorAvg = await CompetitorAvgAsync(product.Id);

                var scenario = _classifier.Classify(new PricingSignals
                {
                    CurrentPrice = product.Price,
                    LandedCost = landed,
                    HasCost = landed > 0m,
                    StockQuantity = product.StockQuantity,
                    RecentUnits = demand.RecentUnits,
                    AverageUnits = demand.AverageUnits,
                    CompetitorAvg = competitorAvg,
                    Mode = demand.Mode
                });

                var existing = await _attention.FirstOrDefaultAsync(a =>
                    a.ShopId == shopId && a.ProductId == product.Id && !a.IsResolved);

                if (scenario.Scenario == PricingScenario.StableMarket)
                {
                    if (existing != null)
                    {
                        existing.IsResolved = true;
                        await _attention.UpdateAsync(existing, autoSave: true);
                    }
                    continue;
                }

                var (priority, opportunity) = Score(scenario.Scenario, product.Price, demand.RecentUnits);
                var reason = string.Join("; ", scenario.ReasonCodes);

                if (existing == null)
                {
                    await _attention.InsertAsync(new PricingAttentionItem(
                        _guid.Create(), tenantId, shopId, product.Id, scenario.Scenario,
                        priority, opportunity, _clock.Now, reason), autoSave: true);
                }
                else
                {
                    existing.Scenario = scenario.Scenario;
                    existing.Priority = priority;
                    existing.EstimatedOpportunity = opportunity;
                    existing.ReasonSummary = reason;
                    existing.DetectedAt = _clock.Now;
                    await _attention.UpdateAsync(existing, autoSave: true);
                }
            }
        }
    }

    private async Task<decimal?> CompetitorAvgAsync(Guid productId)
    {
        var comps = await _competitors.GetListAsync(x => x.ProductId == productId && x.IsAvailable);
        return comps.Count == 0 ? null : comps.Average(c => c.Price);
    }

    private static (double priority, decimal opportunity) Score(PricingScenario scenario, decimal price, decimal recentUnits)
    {
        var severity = scenario switch
        {
            PricingScenario.MarginRisk => 1.0,
            PricingScenario.CompetitorUndercut => 0.9,
            PricingScenario.NearExpiry => 0.9,
            PricingScenario.StaleInventory => 0.8,
            PricingScenario.DemandSurge => 0.7,
            PricingScenario.PriceChangeOpportunity => 0.6,
            PricingScenario.LowData => 0.3,
            _ => 0.2
        };

        var monthlyRevenue = price * recentUnits;
        var opportunity = Math.Round(monthlyRevenue * (decimal)severity * 0.1m, 2);
        var priority = severity * (1.0 + (double)monthlyRevenue / 1000.0);
        return (priority, opportunity);
    }
}
