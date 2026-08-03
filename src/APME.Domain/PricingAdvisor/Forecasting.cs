using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APME.PricingAdvisor;

/// <summary>One period of demand history (monthly): units sold and the average selling price.</summary>
public record DemandHistoryPoint(DateTime Period, decimal Units, decimal Price);

public class ForecastRequest
{
    public Guid ProductId { get; set; }
    public Guid? CategoryId { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal CurrentPrice { get; set; }
    public IReadOnlyList<DemandHistoryPoint> History { get; set; } = new List<DemandHistoryPoint>();
    public IReadOnlyList<decimal> CandidatePrices { get; set; } = new List<decimal>();
    public AdvisorMode Mode { get; set; }
}

public record CandidateForecast(decimal Price, decimal PredictedDemand, ConfidenceLevel Confidence);

public class ForecastResponse
{
    public AdvisorMode Mode { get; set; }
    public List<CandidateForecast> PerCandidate { get; set; } = new();
}

/// <summary>
/// Port to the demand oracle. The domain depends on this interface; the adapter (rule-based stub in M5,
/// FastAPI GRU client in M8) lives in the Application layer.
/// </summary>
public interface IPricingAdvisorClient
{
    Task<ForecastResponse> ForecastAsync(ForecastRequest request);
}
