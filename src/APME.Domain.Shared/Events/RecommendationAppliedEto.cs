using System;

namespace APME.PricingAdvisor;

/// <summary>Published when an approved recommendation's price is applied. Triggers outcome tracking (M6).</summary>
[Serializable]
public class RecommendationAppliedEto
{
    public Guid RecommendationId { get; set; }
    public Guid ProductId { get; set; }
    public Guid ShopId { get; set; }
    public Guid? TenantId { get; set; }
    public decimal OldPrice { get; set; }
    public decimal NewPrice { get; set; }
    public DateTime AppliedAt { get; set; }
}
