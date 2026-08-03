using System.Collections.Generic;
using Volo.Abp.DependencyInjection;

namespace APME.PricingAdvisor;

public record ScenarioResult(PricingScenario Scenario, List<string> ReasonCodes);

public interface IScenarioClassifier
{
    ScenarioResult Classify(PricingSignals signals);
}

/// <summary>Rule-based scenario classifier (v1). The GRU only refines demand, not the scenario label.</summary>
public class ScenarioClassifier : IScenarioClassifier, ITransientDependency
{
    private const decimal SurgeRatio = 1.25m;
    private const decimal CompetitorGapThreshold = 0.05m;
    private const decimal StaleCoverageMultiple = 8m;

    public ScenarioResult Classify(PricingSignals s)
    {
        var reasons = new List<string>();

        if (s.Mode == AdvisorMode.ColdStart || s.Mode == AdvisorMode.EarlySales || !s.HasCost)
        {
            reasons.Add(s.HasCost ? "insufficient-history" : "missing-cost");
            return new ScenarioResult(PricingScenario.LowData, reasons);
        }

        if (s.CompetitorAvg is > 0)
        {
            var gap = (s.CurrentPrice - s.CompetitorAvg.Value) / s.CompetitorAvg.Value;
            if (gap > CompetitorGapThreshold && s.RecentUnits < s.AverageUnits)
            {
                reasons.Add("priced-above-competitors");
                reasons.Add("demand-softening");
                return new ScenarioResult(PricingScenario.CompetitorUndercut, reasons);
            }
        }

        if (s.AverageUnits > 0 && s.RecentUnits >= s.AverageUnits * SurgeRatio)
        {
            reasons.Add("demand-surge");
            return new ScenarioResult(PricingScenario.DemandSurge, reasons);
        }

        if (s.AverageUnits > 0 && s.StockQuantity > s.AverageUnits * StaleCoverageMultiple)
        {
            reasons.Add("high-stock-coverage");
            return new ScenarioResult(PricingScenario.StaleInventory, reasons);
        }

        if (s.CompetitorAvg is > 0 && s.CurrentPrice < s.CompetitorAvg.Value)
        {
            reasons.Add("priced-below-competitors");
            return new ScenarioResult(PricingScenario.PriceChangeOpportunity, reasons);
        }

        reasons.Add("stable");
        return new ScenarioResult(PricingScenario.StableMarket, reasons);
    }
}
