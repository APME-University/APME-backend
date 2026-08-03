using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace APME.PricingAdvisor;

/// <summary>A ranked "needs pricing attention" snapshot for a product, produced by the weekly batch.</summary>
public class PricingAttentionItem : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public Guid ShopId { get; set; }
    public Guid ProductId { get; set; }

    public PricingScenario Scenario { get; set; }
    public double Priority { get; set; }
    public decimal EstimatedOpportunity { get; set; }
    public string? ReasonSummary { get; set; }
    public bool IsResolved { get; set; }
    public DateTime DetectedAt { get; set; }

    protected PricingAttentionItem() { }

    public PricingAttentionItem(
        Guid id, Guid? tenantId, Guid shopId, Guid productId,
        PricingScenario scenario, double priority, decimal estimatedOpportunity,
        DateTime detectedAt, string? reasonSummary = null) : base(id)
    {
        TenantId = tenantId;
        ShopId = shopId;
        ProductId = productId;
        Scenario = scenario;
        Priority = priority;
        EstimatedOpportunity = estimatedOpportunity;
        DetectedAt = detectedAt;
        ReasonSummary = reasonSummary;
        IsResolved = false;
    }
}
