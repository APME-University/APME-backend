using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace APME.PricingAdvisor;

/// <summary>
/// Adapter that fulfils the domain <see cref="IPricingAdvisorClient"/> port by calling the
/// Python FastAPI GRU oracle over HTTP/JSON. Registered as a typed <see cref="HttpClient"/> in
/// <c>APMEApplicationModule</c> when <c>PricingAdvisor:UseStub = false</c>. The GRU + all
/// preprocessing stay in Python; this only ships candidates and reads back demand.
/// </summary>
public class HttpPricingAdvisorClient : IPricingAdvisorClient
{
    private readonly HttpClient _httpClient;

    public HttpPricingAdvisorClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ForecastResponse> ForecastAsync(ForecastRequest request)
    {
        var payload = new
        {
            productId = request.ProductId,
            categoryId = request.CategoryId,
            categoryName = (string?)null,
            currency = request.Currency,
            currentPrice = request.CurrentPrice,
            history = request.History
                .Select(h => new { period = h.Period, units = h.Units, price = h.Price })
                .ToList(),
            candidatePrices = request.CandidatePrices,
            mode = (int)request.Mode
        };

        using var response = await _httpClient.PostAsJsonAsync("/forecast", payload);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<ForecastResponseDto>()
                  ?? throw new InvalidOperationException("Demand oracle returned an empty forecast.");

        return new ForecastResponse
        {
            Mode = (AdvisorMode)dto.Mode,
            PerCandidate = dto.PerCandidate
                .Select(c => new CandidateForecast(c.Price, c.PredictedDemand, (ConfidenceLevel)c.Confidence))
                .ToList()
        };
    }

    // Wire DTOs — camelCase JSON maps via System.Net.Http.Json web defaults.
    private sealed class ForecastResponseDto
    {
        public int Mode { get; set; }
        public List<CandidateDto> PerCandidate { get; set; } = new();
    }

    private sealed class CandidateDto
    {
        public decimal Price { get; set; }
        public decimal PredictedDemand { get; set; }
        public int Confidence { get; set; }
    }
}
