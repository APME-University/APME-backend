using System.Collections.Generic;
using System.Linq;
using Volo.Abp.DependencyInjection;

namespace APME.PricingAdvisor;

public interface IExplanationService
{
    string Build(PricingScenario scenario, RecommendationAction action, decimal currentPrice, decimal recommendedPrice, IEnumerable<string> reasonCodes);
}

/// <summary>Business-language explanation (rule-based v1; can be replaced by the RAG/LLM later).</summary>
public class ExplanationService : IExplanationService, ITransientDependency
{
    public string Build(PricingScenario scenario, RecommendationAction action, decimal currentPrice, decimal recommendedPrice, IEnumerable<string> reasonCodes)
    {
        var verb = action switch
        {
            RecommendationAction.Raise => $"Raise price to {recommendedPrice:0.00}",
            RecommendationAction.Discount => $"Discount price to {recommendedPrice:0.00}",
            RecommendationAction.ClearStock => $"Clearance discount to {recommendedPrice:0.00}",
            RecommendationAction.NearExpiryDiscount => $"Near-expiry discount to {recommendedPrice:0.00}",
            RecommendationAction.CompetitorMatch => $"Adjust price to {recommendedPrice:0.00} to close the competitor gap",
            RecommendationAction.Block => "No financially safe price change is available",
            RecommendationAction.RequestData => "Not enough reliable data for a confident recommendation",
            _ => $"Keep the current price {currentPrice:0.00}"
        };

        var reasons = string.Join("; ", reasonCodes.Select(Humanize));
        return string.IsNullOrWhiteSpace(reasons) ? verb + "." : $"{verb}. Why: {reasons}.";
    }

    private static string Humanize(string code) => code switch
    {
        "priced-above-competitors" => "priced above competitors",
        "priced-below-competitors" => "priced below competitors",
        "demand-softening" => "demand softening",
        "demand-surge" => "demand up vs recent average",
        "high-stock-coverage" => "high stock coverage",
        "insufficient-history" => "limited sales history",
        "missing-cost" => "unit cost not set",
        "stable" => "stable demand and price",
        _ => code
    };
}
