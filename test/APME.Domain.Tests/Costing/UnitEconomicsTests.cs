using System;
using Shouldly;
using Xunit;

namespace APME.Costing;

/// <summary>
/// Pure-math tests for the costing engine. No DI / DB — <see cref="UnitEconomics"/> is a static class.
/// </summary>
public class UnitEconomicsTests
{
    [Fact]
    public void MovingAverage_weights_by_quantity()
    {
        UnitEconomics.MovingAverage(0, 0m, 100, 10m).ShouldBe(10m);   // first receipt
        UnitEconomics.MovingAverage(100, 10m, 100, 12m).ShouldBe(11m); // (100*10 + 100*12)/200
        UnitEconomics.MovingAverage(0, 5m, 0, 8m).ShouldBe(8m);        // empty denominator -> unitIn
    }

    [Fact]
    public void Compute_returns_correct_unit_economics()
    {
        var r = UnitEconomics.Compute(price: 100m, landedCost: 40m, perUnitOperating: 5m, feePct: 0.15m);

        r.VariableSellingCost.ShouldBe(20m);   // 5 + 0.15*100
        r.GrossProfit.ShouldBe(60m);
        r.GrossMargin.ShouldBe(0.6m);
        r.ContributionMargin.ShouldBe(40m);    // 100 - 40 - 20
        r.ContributionRatio.ShouldBe(0.4m);
        r.Markup.ShouldBe(1.5m);               // 60/40
        Math.Round(r.BreakEvenFloor, 4).ShouldBe(52.9412m); // (40+5)/(1-0.15)
    }

    [Fact]
    public void BreakEvenFloor_gives_zero_contribution_at_the_floor()
    {
        var floor = UnitEconomics.Compute(100m, 40m, 5m, 0.15m).BreakEvenFloor;
        var atFloor = UnitEconomics.Compute(floor, 40m, 5m, 0.15m);
        Math.Round(atFloor.ContributionMargin, 6).ShouldBe(0m);
    }

    [Fact]
    public void BreakEvenFloor_is_fee_aware_and_exceeds_naive_cost()
    {
        // naive floor (landed + per-unit) = 45; a %-fee raises the true floor above it.
        UnitEconomics.Compute(100m, 40m, 5m, 0.15m).BreakEvenFloor.ShouldBeGreaterThan(45m);
    }

    [Fact]
    public void TargetPriceGross_hits_the_target_margin()
    {
        var price = UnitEconomics.TargetPriceGross(landedCost: 40m, targetMargin: 0.6m);
        price.ShouldBe(100m);
        UnitEconomics.Compute(price, 40m, 0m, 0m).GrossMargin.ShouldBe(0.6m);
    }

    [Fact]
    public void TargetPriceContribution_hits_the_target_contribution_margin()
    {
        var price = UnitEconomics.TargetPriceContribution(
            landedCost: 40m, perUnitOperating: 5m, feePct: 0.15m, targetContributionMargin: 0.4m);
        price.ShouldBe(100m);
        UnitEconomics.Compute(price, 40m, 5m, 0.15m).ContributionRatio.ShouldBe(0.4m);
    }

    [Fact]
    public void Markup_and_margin_convert_both_ways()
    {
        UnitEconomics.MarkupFromMargin(0.6m).ShouldBe(1.5m);
        UnitEconomics.MarginFromMarkup(1.5m).ShouldBe(0.6m);
    }

    [Fact]
    public void Guards_zero_price_zero_cost_and_full_fees()
    {
        var r = UnitEconomics.Compute(0m, 0m, 0m, 0m);
        r.GrossMargin.ShouldBe(0m);
        r.Markup.ShouldBe(0m);
        r.ContributionRatio.ShouldBe(0m);

        UnitEconomics.Compute(100m, 40m, 5m, 1.0m).BreakEvenFloor.ShouldBe(decimal.MaxValue);
    }
}
