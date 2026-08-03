using System;
using Volo.Abp.Domain.Entities;

namespace APME.PricingAdvisor;

/// <summary>A single evaluated candidate price within a recommendation (child of PricingRecommendation).</summary>
public class PriceCandidate : Entity<Guid>
{
    public Guid RecommendationId { get; private set; }
    public decimal Price { get; set; }
    public decimal PctChange { get; set; }
    public decimal PredictedDemand { get; set; }
    public decimal ExpectedRevenue { get; set; }
    public decimal ExpectedProfit { get; set; } // contribution
    public decimal Margin { get; set; }
    public GuardrailStatus GuardrailStatus { get; set; }
    public string? GuardrailsJson { get; set; } // jsonb serialized List<GuardrailEvaluation>
    public bool IsRecommended { get; set; }

    protected PriceCandidate() { }

    public PriceCandidate(
        Guid id,
        Guid recommendationId,
        decimal price,
        decimal pctChange,
        decimal predictedDemand,
        decimal expectedRevenue,
        decimal expectedProfit,
        decimal margin,
        GuardrailStatus guardrailStatus,
        string? guardrailsJson,
        bool isRecommended) : base(id)
    {
        RecommendationId = recommendationId;
        Price = price;
        PctChange = pctChange;
        PredictedDemand = predictedDemand;
        ExpectedRevenue = expectedRevenue;
        ExpectedProfit = expectedProfit;
        Margin = margin;
        GuardrailStatus = guardrailStatus;
        GuardrailsJson = guardrailsJson;
        IsRecommended = isRecommended;
    }
}
