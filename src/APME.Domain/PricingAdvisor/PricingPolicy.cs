using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace APME.PricingAdvisor;

/// <summary>
/// Per-shop pricing guardrail policy (limits + candidate grid). Distinct from Costing's CostingPolicy.
/// Percentages are fractions (0.10 = 10%). AutoApply stays off in v1 (recommend-only).
/// </summary>
public class PricingPolicy : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public Guid ShopId { get; set; }

    public decimal MinMarginPct { get; set; }
    public decimal MaxDiscountPct { get; set; }
    public decimal MaxIncreasePct { get; set; }

    /// <summary>Candidate grid as jsonb, e.g. "[-0.10,-0.05,0,0.05,0.10]".</summary>
    public string CandidateGridJson { get; set; }

    public bool AutoApplyEnabled { get; set; }
    public bool IsActive { get; set; }

    protected PricingPolicy()
    {
        CandidateGridJson = "[-0.10,-0.05,0,0.05,0.10]";
    }

    public PricingPolicy(Guid id, Guid? tenantId, Guid shopId) : base(id)
    {
        TenantId = tenantId;
        ShopId = shopId;
        MinMarginPct = 0.20m;
        MaxDiscountPct = 0.15m;
        MaxIncreasePct = 0.15m;
        CandidateGridJson = "[-0.10,-0.05,0,0.05,0.10]";
        AutoApplyEnabled = false;
        IsActive = true;
    }
}
