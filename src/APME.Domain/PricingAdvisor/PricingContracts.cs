using System.Collections.Generic;

namespace APME.PricingAdvisor;

/// <summary>Signals fed to the scenario classifier.</summary>
public class PricingSignals
{
    public decimal CurrentPrice { get; set; }
    public decimal LandedCost { get; set; }
    public bool HasCost { get; set; }
    public int StockQuantity { get; set; }
    public decimal RecentUnits { get; set; }
    public decimal AverageUnits { get; set; }
    public decimal? CompetitorAvg { get; set; }
    public AdvisorMode Mode { get; set; }
}

public record GuardrailOutcome(GuardrailStatus Status, IReadOnlyList<GuardrailEvaluation> Evaluations);

/// <summary>Result of evaluating a single candidate price (used by simulate).</summary>
public record PriceEvaluation(
    decimal Price,
    decimal PctChange,
    decimal PredictedDemand,
    decimal ExpectedRevenue,
    decimal ExpectedProfit,
    decimal Margin,
    GuardrailStatus GuardrailStatus,
    IReadOnlyList<GuardrailEvaluation> Guardrails);
