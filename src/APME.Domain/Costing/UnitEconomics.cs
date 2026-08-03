namespace APME.Costing;

/// <summary>Result of a unit-economics calculation for a product at a given price.</summary>
public record UnitEconomicsResult(
    decimal Price,
    decimal LandedCost,
    decimal VariableSellingCost,
    decimal GrossProfit,
    decimal GrossMargin,
    decimal ContributionMargin,
    decimal ContributionRatio,
    decimal Markup,
    decimal BreakEvenFloor);

/// <summary>
/// Pure cost-accounting math (no I/O). This is the "expert financial engine" the CostingService
/// wraps. Percentages are fractions (0.15 = 15%).
/// </summary>
public static class UnitEconomics
{
    /// <summary>Perpetual moving-average (AVCO) cost after receiving <paramref name="qtyIn"/> units.</summary>
    public static decimal MovingAverage(int onHand, decimal oldAvg, int qtyIn, decimal unitIn)
    {
        var denom = onHand + qtyIn;
        return denom <= 0 ? unitIn : (onHand * oldAvg + qtyIn * unitIn) / denom;
    }

    /// <summary>
    /// Full unit economics at <paramref name="price"/>. <paramref name="perUnitOperating"/> is the
    /// fixed per-unit variable selling cost; <paramref name="feePct"/> is the total %-of-price fees.
    /// The break-even floor is fee-aware: a naive "price &gt; cost" floor under-prices because %-fees
    /// rise with price.
    /// </summary>
    public static UnitEconomicsResult Compute(decimal price, decimal landedCost, decimal perUnitOperating, decimal feePct)
    {
        var variableSelling = perUnitOperating + feePct * price;
        var grossProfit = price - landedCost;
        var grossMargin = price > 0 ? grossProfit / price : 0m;
        var contribution = price - landedCost - variableSelling;
        var contributionRatio = price > 0 ? contribution / price : 0m;
        var markup = landedCost > 0 ? grossProfit / landedCost : 0m;
        var breakEven = feePct < 1m ? (landedCost + perUnitOperating) / (1m - feePct) : decimal.MaxValue;
        return new UnitEconomicsResult(price, landedCost, variableSelling, grossProfit, grossMargin,
            contribution, contributionRatio, markup, breakEven);
    }

    /// <summary>Price that yields a target GROSS margin m (fraction).</summary>
    public static decimal TargetPriceGross(decimal landedCost, decimal targetMargin)
        => targetMargin < 1m ? landedCost / (1m - targetMargin) : 0m;

    /// <summary>Price that yields a target CONTRIBUTION margin m (fraction), fee-aware.</summary>
    public static decimal TargetPriceContribution(decimal landedCost, decimal perUnitOperating, decimal feePct, decimal targetContributionMargin)
    {
        var denom = 1m - feePct - targetContributionMargin;
        return denom > 0m ? (landedCost + perUnitOperating) / denom : 0m;
    }

    public static decimal MarkupFromMargin(decimal margin) => margin < 1m ? margin / (1m - margin) : 0m;

    public static decimal MarginFromMarkup(decimal markup) => markup / (1m + markup);
}
