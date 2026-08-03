using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace APME.PricingAdvisor;

/// <summary>Append-only log of a product's own-price changes (the price-change history the advisor lacks otherwise).</summary>
public class PriceChangeLog : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public Guid ProductId { get; set; }
    public Guid ShopId { get; set; }

    public decimal OldPrice { get; set; }
    public decimal NewPrice { get; set; }
    public PriceChangeSource Source { get; set; }
    public Guid? ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; }

    protected PriceChangeLog() { }

    public PriceChangeLog(
        Guid id, Guid? tenantId, Guid productId, Guid shopId,
        decimal oldPrice, decimal newPrice, PriceChangeSource source,
        DateTime changedAt, Guid? changedBy = null) : base(id)
    {
        TenantId = tenantId;
        ProductId = productId;
        ShopId = shopId;
        OldPrice = oldPrice;
        NewPrice = newPrice;
        Source = source;
        ChangedAt = changedAt;
        ChangedBy = changedBy;
    }
}
