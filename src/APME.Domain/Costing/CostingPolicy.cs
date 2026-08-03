using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace APME.Costing;

/// <summary>
/// Per-shop costing configuration: the default costing method, the margin basis, and the
/// operating-plane rates (variable selling costs) that apply to all of the shop's products.
/// Set once by the accountant; per-product overrides go on <see cref="CostComponentEntry"/>.
/// Percentages are fractions (0.15 = 15%).
/// </summary>
public class CostingPolicy : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public Guid ShopId { get; set; }

    public CostingMethod DefaultMethod { get; set; }

    public MarginBasis MarginBasis { get; set; }

    public string Currency { get; set; }

    // Operating-plane rates (variable selling cost)
    public decimal MarketplaceFeePct { get; set; }
    public decimal PaymentFeePct { get; set; }
    public decimal PaymentFixedPerUnit { get; set; }
    public decimal FulfillmentPerUnit { get; set; }
    public decimal StoragePerUnitMonth { get; set; }
    public decimal ReturnsReservePct { get; set; }
    public decimal AdPerUnit { get; set; }

    public bool IsActive { get; set; }

    protected CostingPolicy()
    {
        Currency = "USD";
    }

    public CostingPolicy(Guid id, Guid? tenantId, Guid shopId, string currency = "USD") : base(id)
    {
        TenantId = tenantId;
        ShopId = shopId;
        Currency = string.IsNullOrWhiteSpace(currency) ? "USD" : currency;
        DefaultMethod = CostingMethod.MovingAverage;
        MarginBasis = MarginBasis.Contribution;
        IsActive = true;
    }

    /// <summary>Total per-unit operating cost (fixed money that scales per sale, not with price).</summary>
    public decimal PerUnitOperatingCost => FulfillmentPerUnit + AdPerUnit + PaymentFixedPerUnit;

    /// <summary>Total operating fee fraction applied to the selling price.</summary>
    public decimal FeePct => MarketplaceFeePct + PaymentFeePct + ReturnsReservePct;
}
