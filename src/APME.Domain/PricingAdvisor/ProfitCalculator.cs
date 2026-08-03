using APME.Costing;
using Volo.Abp.DependencyInjection;

namespace APME.PricingAdvisor;

public record ProfitResult(decimal Revenue, decimal Profit, decimal Margin);

public interface IProfitCalculator
{
    ProfitResult Compute(UnitEconomicsResult econ, decimal predictedDemand);
}

/// <summary>Turns per-unit economics + predicted demand into expected revenue and contribution profit.</summary>
public class ProfitCalculator : IProfitCalculator, ITransientDependency
{
    public ProfitResult Compute(UnitEconomicsResult econ, decimal predictedDemand)
    {
        var revenue = econ.Price * predictedDemand;
        var profit = econ.ContributionMargin * predictedDemand; // contribution per unit * units
        return new ProfitResult(revenue, profit, econ.ContributionRatio);
    }
}
