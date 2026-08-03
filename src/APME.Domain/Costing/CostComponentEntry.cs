using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace APME.Costing;

/// <summary>
/// A single dated cost component for a product, on either plane. Multiple entries per product/type
/// are allowed over time; the newest effective entry per type is the current one.
/// </summary>
public class CostComponentEntry : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }

    public Guid ProductId { get; set; }

    public Guid ShopId { get; set; }

    public CostComponentType ComponentType { get; set; }

    public CostPlane Plane { get; set; }

    public CostValueType ValueType { get; set; }

    /// <summary>Per-unit money, a fraction of price (0.15 = 15%), or a lump sum — per <see cref="ValueType"/>.</summary>
    public decimal Amount { get; set; }

    public string Currency { get; set; }

    public DateTime EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public CostSource Source { get; set; }

    public string? Reference { get; set; }

    public string? Note { get; set; }

    protected CostComponentEntry()
    {
        Currency = "USD";
    }

    public CostComponentEntry(
        Guid id,
        Guid? tenantId,
        Guid productId,
        Guid shopId,
        CostComponentType componentType,
        CostValueType valueType,
        decimal amount,
        DateTime effectiveFrom,
        CostSource source,
        string currency = "USD") : base(id)
    {
        TenantId = tenantId;
        ProductId = productId;
        ShopId = shopId;
        ComponentType = componentType;
        Plane = componentType.GetPlane();
        ValueType = valueType;
        Amount = amount;
        EffectiveFrom = effectiveFrom;
        Source = source;
        Currency = string.IsNullOrWhiteSpace(currency) ? "USD" : currency;
    }
}
