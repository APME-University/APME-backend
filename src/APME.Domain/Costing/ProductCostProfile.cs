using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace APME.Costing;

/// <summary>
/// Per-product costing root. Holds the current landed unit cost (the value the pricing advisor
/// reads) and the costing method. Cost history lives in <see cref="CostComponentEntry"/> rows.
/// </summary>
public class ProductCostProfile : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public Guid ProductId { get; private set; }

    public Guid ShopId { get; private set; }

    public CostingMethod Method { get; set; }

    public string Currency { get; set; }

    /// <summary>The authoritative landed unit cost used as the COGS basis and guardrail floor input.</summary>
    public decimal CurrentLandedUnitCost { get; private set; }

    public int QtyOnHand { get; private set; }

    public DateTime? LastCostedAt { get; private set; }

    protected ProductCostProfile()
    {
        Currency = "USD";
    }

    public ProductCostProfile(
        Guid id,
        Guid? tenantId,
        Guid productId,
        Guid shopId,
        string currency = "USD",
        CostingMethod method = CostingMethod.MovingAverage) : base(id)
    {
        TenantId = tenantId;
        ProductId = productId;
        ShopId = shopId;
        Currency = string.IsNullOrWhiteSpace(currency) ? "USD" : currency;
        Method = method;
        CurrentLandedUnitCost = 0m;
        QtyOnHand = 0;
    }

    /// <summary>Moving-average (perpetual AVCO) update on a purchase receipt.</summary>
    public void ApplyReceipt(int qty, decimal unitLandedCost, DateTime occurredAt)
    {
        if (qty <= 0)
        {
            throw new ArgumentException("Receipt quantity must be positive", nameof(qty));
        }
        if (unitLandedCost < 0)
        {
            throw new ArgumentException("Unit landed cost cannot be negative", nameof(unitLandedCost));
        }

        CurrentLandedUnitCost = UnitEconomics.MovingAverage(QtyOnHand, CurrentLandedUnitCost, qty, unitLandedCost);
        QtyOnHand += qty;
        LastCostedAt = occurredAt;
    }

    /// <summary>Directly set the landed cost (Standard / manual / recompute-from-components).</summary>
    public void SetLandedUnitCost(decimal landedUnitCost, DateTime occurredAt)
    {
        if (landedUnitCost < 0)
        {
            throw new ArgumentException("Landed unit cost cannot be negative", nameof(landedUnitCost));
        }
        CurrentLandedUnitCost = landedUnitCost;
        LastCostedAt = occurredAt;
    }

    public void SetQtyOnHand(int qty)
    {
        QtyOnHand = qty < 0 ? 0 : qty;
    }

    public bool HasCost => CurrentLandedUnitCost > 0m || LastCostedAt.HasValue;
}
