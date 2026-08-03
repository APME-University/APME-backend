using System;
using System.Collections.Generic;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace APME.PricingAdvisor;

/// <summary>
/// The advisor's core aggregate: a guarded, explainable price recommendation for a product, with the
/// evaluated candidates and a human-approval lifecycle.
/// </summary>
public class PricingRecommendation : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    public Guid ShopId { get; private set; }
    public Guid ProductId { get; private set; }

    public PricingScenario Scenario { get; set; }
    public AdvisorMode Mode { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal RecommendedPrice { get; set; }
    public RecommendationAction Action { get; set; }

    public decimal ExpectedDemand { get; set; }
    public decimal ExpectedRevenue { get; set; }
    public decimal ExpectedProfit { get; set; } // contribution
    public decimal ExpectedMargin { get; set; }
    public ConfidenceLevel Confidence { get; set; }

    public RecommendationStatus Status { get; private set; }
    public string? ReasonCodes { get; set; }  // jsonb
    public string? Explanation { get; set; }

    public Guid? DecidedBy { get; private set; }
    public DateTime? DecidedAt { get; private set; }
    public DateTime? AppliedAt { get; private set; }
    public string? DecisionNote { get; private set; }

    public virtual ICollection<PriceCandidate> Candidates { get; private set; }

    protected PricingRecommendation()
    {
        Candidates = new List<PriceCandidate>();
    }

    public PricingRecommendation(
        Guid id,
        Guid? tenantId,
        Guid shopId,
        Guid productId,
        PricingScenario scenario,
        AdvisorMode mode,
        decimal currentPrice) : base(id)
    {
        TenantId = tenantId;
        ShopId = shopId;
        ProductId = productId;
        Scenario = scenario;
        Mode = mode;
        CurrentPrice = currentPrice;
        RecommendedPrice = currentPrice;
        Action = RecommendationAction.Keep;
        Confidence = ConfidenceLevel.Low;
        Status = RecommendationStatus.Pending;
        Candidates = new List<PriceCandidate>();
    }

    public PriceCandidate AddCandidate(
        Guid id, decimal price, decimal pctChange, decimal predictedDemand,
        decimal expectedRevenue, decimal expectedProfit, decimal margin,
        GuardrailStatus guardrailStatus, string? guardrailsJson, bool isRecommended)
    {
        var candidate = new PriceCandidate(id, Id, price, pctChange, predictedDemand,
            expectedRevenue, expectedProfit, margin, guardrailStatus, guardrailsJson, isRecommended);
        Candidates.Add(candidate);
        return candidate;
    }

    public void SetOutcome(
        decimal recommendedPrice, RecommendationAction action, decimal expectedDemand,
        decimal expectedRevenue, decimal expectedProfit, decimal expectedMargin,
        ConfidenceLevel confidence, string? reasonCodes, string? explanation)
    {
        RecommendedPrice = recommendedPrice;
        Action = action;
        ExpectedDemand = expectedDemand;
        ExpectedRevenue = expectedRevenue;
        ExpectedProfit = expectedProfit;
        ExpectedMargin = expectedMargin;
        Confidence = confidence;
        ReasonCodes = reasonCodes;
        Explanation = explanation;
    }

    public void Approve(Guid userId, DateTime occurredAt)
    {
        EnsureDecidable();
        Status = RecommendationStatus.Approved;
        DecidedBy = userId;
        DecidedAt = occurredAt;
    }

    public void Reject(Guid userId, DateTime occurredAt, string? note = null)
    {
        EnsureDecidable();
        Status = RecommendationStatus.Rejected;
        DecidedBy = userId;
        DecidedAt = occurredAt;
        DecisionNote = note;
    }

    public void SendToManualReview()
    {
        Status = RecommendationStatus.ManualReview;
    }

    public void MarkApplied(DateTime occurredAt)
    {
        if (Status != RecommendationStatus.Approved)
        {
            throw new BusinessException("APME:PricingRecommendation.NotApproved")
                .WithData("Status", Status);
        }
        Status = RecommendationStatus.Applied;
        AppliedAt = occurredAt;
    }

    private void EnsureDecidable()
    {
        if (Status != RecommendationStatus.Pending && Status != RecommendationStatus.ManualReview)
        {
            throw new BusinessException("APME:PricingRecommendation.NotPending")
                .WithData("Status", Status);
        }
    }
}
