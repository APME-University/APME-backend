using System;
using Volo.Abp.Application.Dtos;

namespace APME.PricingAdvisor;

public class PricingPolicyDto : FullAuditedEntityDto<Guid>
{
    public Guid ShopId { get; set; }
    public decimal MinMarginPct { get; set; }
    public decimal MaxDiscountPct { get; set; }
    public decimal MaxIncreasePct { get; set; }
    public string CandidateGridJson { get; set; } = "[-0.10,-0.05,0,0.05,0.10]";
    public bool AutoApplyEnabled { get; set; }
    public bool IsActive { get; set; }
}

public class CreateUpdatePricingPolicyDto
{
    public decimal MinMarginPct { get; set; } = 0.20m;
    public decimal MaxDiscountPct { get; set; } = 0.15m;
    public decimal MaxIncreasePct { get; set; } = 0.15m;
    public string CandidateGridJson { get; set; } = "[-0.10,-0.05,0,0.05,0.10]";
    public bool AutoApplyEnabled { get; set; }
    public bool IsActive { get; set; } = true;
}
