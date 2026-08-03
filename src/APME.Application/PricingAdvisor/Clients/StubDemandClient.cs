using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace APME.PricingAdvisor;

/// <summary>
/// Rule-based demand stub (v1). Deterministic constant-elasticity response around the current price,
/// so the whole vertical slice runs with no external dependency. Selected when
/// <c>PricingAdvisor:UseStub = true</c>; the FastAPI GRU (<see cref="HttpPricingAdvisorClient"/>)
/// takes over when it's false. Registered explicitly in <c>APMEApplicationModule</c> (no auto-DI
/// marker) so exactly one <see cref="IPricingAdvisorClient"/> is ever bound.
/// </summary>
public class StubDemandClient : IPricingAdvisorClient
{
    private const decimal Elasticity = -1.2m; // illustrative; the GRU replaces this in M8

    public Task<ForecastResponse> ForecastAsync(ForecastRequest request)
    {
        var baseline = request.History.Count > 0 ? request.History[^1].Units : 0m;
        if (baseline <= 0m && request.History.Count > 0)
        {
            baseline = request.History.Average(h => h.Units);
        }

        var current = request.CurrentPrice > 0m
            ? request.CurrentPrice
            : (request.History.Count > 0 ? request.History[^1].Price : 1m);

        var confidence = request.Mode switch
        {
            AdvisorMode.FullPriceResponse => ConfidenceLevel.High,
            AdvisorMode.Standard => ConfidenceLevel.Medium,
            _ => ConfidenceLevel.Low
        };

        var perCandidate = new List<CandidateForecast>();
        foreach (var price in request.CandidatePrices)
        {
            decimal demand;
            if (baseline <= 0m || current <= 0m)
            {
                demand = baseline;
            }
            else
            {
                var ratio = (double)(price / current);
                demand = baseline * (decimal)Math.Pow(ratio, (double)Elasticity);
            }

            perCandidate.Add(new CandidateForecast(price, Math.Round(Math.Max(0m, demand), 2), confidence));
        }

        return Task.FromResult(new ForecastResponse { Mode = request.Mode, PerCandidate = perCandidate });
    }
}
