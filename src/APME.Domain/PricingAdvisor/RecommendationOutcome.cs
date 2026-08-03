using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace APME.PricingAdvisor;

/// <summary>Measured actual outcome of an applied recommendation (closed-loop learning / evaluation).</summary>
public class RecommendationOutcome : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public Guid RecommendationId { get; set; }
    public Guid ProductId { get; set; }
    public Guid ShopId { get; set; }

    public decimal ActualUnits { get; set; }
    public decimal ActualRevenue { get; set; }
    public decimal ActualProfit { get; set; }
    public DateTime WindowStart { get; set; }
    public DateTime WindowEnd { get; set; }
    public DateTime MeasuredAt { get; set; }

    protected RecommendationOutcome() { }

    public RecommendationOutcome(
        Guid id, Guid? tenantId, Guid recommendationId, Guid productId, Guid shopId,
        DateTime windowStart, DateTime windowEnd) : base(id)
    {
        TenantId = tenantId;
        RecommendationId = recommendationId;
        ProductId = productId;
        ShopId = shopId;
        WindowStart = windowStart;
        WindowEnd = windowEnd;
    }
}
