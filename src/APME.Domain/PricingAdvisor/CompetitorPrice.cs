using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace APME.PricingAdvisor;

/// <summary>A dated competitor price snapshot for a product.</summary>
public class CompetitorPrice : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public Guid ProductId { get; set; }
    public Guid ShopId { get; set; }

    public string Competitor { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; }
    public string? Url { get; set; }
    public DateTime CapturedAt { get; set; }
    public double MatchConfidence { get; set; }
    public bool IsAvailable { get; set; }

    protected CompetitorPrice()
    {
        Competitor = string.Empty;
        Currency = "USD";
    }

    public CompetitorPrice(
        Guid id, Guid? tenantId, Guid productId, Guid shopId,
        string competitor, decimal price, DateTime capturedAt,
        double matchConfidence = 1.0, string currency = "USD") : base(id)
    {
        TenantId = tenantId;
        ProductId = productId;
        ShopId = shopId;
        Competitor = competitor;
        Price = price;
        Currency = string.IsNullOrWhiteSpace(currency) ? "USD" : currency;
        CapturedAt = capturedAt;
        MatchConfidence = matchConfidence;
        IsAvailable = true;
    }
}
