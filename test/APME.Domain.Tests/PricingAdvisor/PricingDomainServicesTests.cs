using System;
using System.Linq;
using APME.Costing;
using Shouldly;
using Xunit;

namespace APME.PricingAdvisor;

/// <summary>Pure-logic tests for the M2 domain services (no DI/DB).</summary>
public class PricingDomainServicesTests
{
    private static PricingPolicy Policy(decimal minMargin = 0.20m, decimal maxDisc = 0.15m, decimal maxInc = 0.15m)
    {
        var p = new PricingPolicy(Guid.NewGuid(), null, Guid.NewGuid());
        p.MinMarginPct = minMargin;
        p.MaxDiscountPct = maxDisc;
        p.MaxIncreasePct = maxInc;
        return p;
    }

    // ---------- CandidateGenerator ----------
    [Fact]
    public void CandidateGenerator_builds_grid_and_includes_current()
    {
        var prices = new CandidateGenerator().Generate(100m, Policy());
        prices.ShouldContain(90m);
        prices.ShouldContain(100m);
        prices.ShouldContain(110m);
        prices.ShouldBeInOrder();
    }

    [Fact]
    public void CandidateGenerator_clamps_to_max_discount()
    {
        var prices = new CandidateGenerator().Generate(100m, Policy(maxDisc: 0.05m));
        prices.ShouldNotContain(90m);  // -10% clamped to -5% -> 95
        prices.ShouldContain(95m);
    }

    // ---------- ScenarioClassifier ----------
    [Fact]
    public void Classifier_cold_start_is_low_data()
    {
        var r = new ScenarioClassifier().Classify(new PricingSignals { Mode = AdvisorMode.ColdStart, HasCost = true });
        r.Scenario.ShouldBe(PricingScenario.LowData);
    }

    [Fact]
    public void Classifier_detects_surge_and_undercut_and_stable()
    {
        var c = new ScenarioClassifier();

        c.Classify(new PricingSignals { Mode = AdvisorMode.Standard, HasCost = true, AverageUnits = 100, RecentUnits = 140, StockQuantity = 50 })
            .Scenario.ShouldBe(PricingScenario.DemandSurge);

        c.Classify(new PricingSignals { Mode = AdvisorMode.Standard, HasCost = true, CurrentPrice = 100, CompetitorAvg = 80, AverageUnits = 100, RecentUnits = 50, StockQuantity = 50 })
            .Scenario.ShouldBe(PricingScenario.CompetitorUndercut);

        c.Classify(new PricingSignals { Mode = AdvisorMode.Standard, HasCost = true, CurrentPrice = 100, AverageUnits = 100, RecentUnits = 100, StockQuantity = 100 })
            .Scenario.ShouldBe(PricingScenario.StableMarket);
    }

    // ---------- GuardrailEngine ----------
    [Fact]
    public void Guardrails_block_below_fee_aware_floor_and_min_margin()
    {
        var econ = UnitEconomics.Compute(price: 50m, landedCost: 40m, perUnitOperating: 5m, feePct: 0.15m); // floor ~52.94
        var outcome = new GuardrailEngine().Evaluate(econ, currentPrice: 100m, Policy());
        outcome.Status.ShouldBe(GuardrailStatus.Block);
        outcome.Evaluations.ShouldContain(e => e.Rule == GuardrailRule.CostFloor && e.Status == GuardrailStatus.Block);
    }

    [Fact]
    public void Guardrails_cap_excessive_discount()
    {
        var econ = UnitEconomics.Compute(price: 70m, landedCost: 40m, perUnitOperating: 5m, feePct: 0.15m); // margin ~0.21, above floor
        var outcome = new GuardrailEngine().Evaluate(econ, currentPrice: 100m, Policy()); // -30% > 15% max discount
        outcome.Status.ShouldBe(GuardrailStatus.Cap);
        outcome.Evaluations.ShouldContain(e => e.Rule == GuardrailRule.MaxDiscount && e.Status == GuardrailStatus.Cap);
    }

    [Fact]
    public void Guardrails_pass_safe_price()
    {
        var econ = UnitEconomics.Compute(price: 100m, landedCost: 40m, perUnitOperating: 5m, feePct: 0.15m);
        var outcome = new GuardrailEngine().Evaluate(econ, currentPrice: 95m, Policy()); // +5.3% within limits
        outcome.Status.ShouldBe(GuardrailStatus.Pass);
    }

    // ---------- ProfitCalculator ----------
    [Fact]
    public void ProfitCalculator_uses_contribution_margin()
    {
        var econ = UnitEconomics.Compute(100m, 40m, 5m, 0.15m); // contribution/unit = 40
        var r = new ProfitCalculator().Compute(econ, predictedDemand: 10m);
        r.Revenue.ShouldBe(1000m);
        r.Profit.ShouldBe(400m);
        r.Margin.ShouldBe(0.4m);
    }
}
