using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using APME.Costing;
using APME.Products;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;

namespace APME.PricingAdvisor;

public interface IPricingRecommendationManager : IDomainService
{
    Task<PricingRecommendation> GenerateAsync(Guid productId, Guid shopId);
    Task<PriceEvaluation> SimulateAsync(Guid productId, Guid shopId, decimal price);
}

/// <summary>
/// Orchestrates the pricing pipeline: signals → scenario → candidates → demand forecast → unit economics
/// → guardrails → select best safe candidate → build a PricingRecommendation.
/// </summary>
public class PricingRecommendationManager : DomainService, IPricingRecommendationManager
{
    private readonly IRepository<Product, Guid> _products;
    private readonly IRepository<PricingPolicy, Guid> _policies;
    private readonly IRepository<CompetitorPrice, Guid> _competitors;
    private readonly IRepository<PricingRecommendation, Guid> _recommendations;
    private readonly ICostingService _costing;
    private readonly IPricingAdvisorClient _client;
    private readonly IDemandFeatureBuilder _features;
    private readonly IScenarioClassifier _classifier;
    private readonly ICandidateGenerator _candidateGenerator;
    private readonly IProfitCalculator _profit;
    private readonly IGuardrailEngine _guardrails;
    private readonly IExplanationService _explanation;

    public PricingRecommendationManager(
        IRepository<Product, Guid> products,
        IRepository<PricingPolicy, Guid> policies,
        IRepository<CompetitorPrice, Guid> competitors,
        IRepository<PricingRecommendation, Guid> recommendations,
        ICostingService costing,
        IPricingAdvisorClient client,
        IDemandFeatureBuilder features,
        IScenarioClassifier classifier,
        ICandidateGenerator candidateGenerator,
        IProfitCalculator profit,
        IGuardrailEngine guardrails,
        IExplanationService explanation)
    {
        _products = products;
        _policies = policies;
        _competitors = competitors;
        _recommendations = recommendations;
        _costing = costing;
        _client = client;
        _features = features;
        _classifier = classifier;
        _candidateGenerator = candidateGenerator;
        _profit = profit;
        _guardrails = guardrails;
        _explanation = explanation;
    }

    public async Task<PricingRecommendation> GenerateAsync(Guid productId, Guid shopId)
    {
        var product = await _products.GetAsync(productId);
        var policy = await _policies.FirstOrDefaultAsync(x => x.ShopId == shopId);
        var demand = await _features.BuildAsync(productId, shopId);
        var competitorAvg = await GetCompetitorAvgAsync(productId);
        var landed = await _costing.GetLandedUnitCostAsync(productId);

        var signals = new PricingSignals
        {
            CurrentPrice = product.Price,
            LandedCost = landed,
            HasCost = landed > 0m,
            StockQuantity = product.StockQuantity,
            RecentUnits = demand.RecentUnits,
            AverageUnits = demand.AverageUnits,
            CompetitorAvg = competitorAvg,
            Mode = demand.Mode
        };

        var scenario = _classifier.Classify(signals);
        var candidatePrices = _candidateGenerator.Generate(product.Price, policy);

        var forecast = await _client.ForecastAsync(new ForecastRequest
        {
            ProductId = productId,
            CategoryId = product.CategoryId,
            Currency = product.Currency,
            CurrentPrice = product.Price,
            History = demand.History,
            CandidatePrices = candidatePrices,
            Mode = demand.Mode
        });
        var demandByPrice = forecast.PerCandidate
            .GroupBy(c => c.Price)
            .ToDictionary(g => g.Key, g => g.First().PredictedDemand);

        var confidence = ToConfidence(demand.Mode);
        var recommendation = new PricingRecommendation(
            GuidGenerator.Create(), CurrentTenant.Id, shopId, productId, scenario.Scenario, demand.Mode, product.Price);

        PriceCandidate? best = null;
        var bestProfit = decimal.MinValue;

        foreach (var price in candidatePrices)
        {
            var predicted = demandByPrice.TryGetValue(price, out var d) ? d : 0m;
            var econ = await _costing.GetUnitEconomicsAsync(productId, shopId, price);
            var profit = _profit.Compute(econ, predicted);
            var guard = _guardrails.Evaluate(econ, product.Price, policy);
            var pct = product.Price > 0m ? (price - product.Price) / product.Price : 0m;

            var candidate = recommendation.AddCandidate(
                GuidGenerator.Create(), price, pct, predicted,
                profit.Revenue, profit.Profit, profit.Margin,
                guard.Status, JsonSerializer.Serialize(guard.Evaluations), isRecommended: false);

            if (guard.Status != GuardrailStatus.Block && profit.Profit > bestProfit)
            {
                bestProfit = profit.Profit;
                best = candidate;
            }
        }

        var reasonCodesJson = JsonSerializer.Serialize(scenario.ReasonCodes);

        // Cold-start / early-sales: not enough history to justify a data-driven price change.
        if (demand.Mode is AdvisorMode.ColdStart or AdvisorMode.EarlySales)
        {
            recommendation.SetOutcome(product.Price, RecommendationAction.RequestData, 0m, 0m, 0m, 0m, confidence,
                reasonCodesJson,
                _explanation.Build(scenario.Scenario, RecommendationAction.RequestData, product.Price, product.Price, scenario.ReasonCodes));
            await _recommendations.InsertAsync(recommendation, autoSave: true);
            return recommendation;
        }

        if (best == null)
        {
            recommendation.SetOutcome(product.Price, RecommendationAction.Block, 0m, 0m, 0m, 0m, confidence,
                reasonCodesJson, _explanation.Build(scenario.Scenario, RecommendationAction.Block, product.Price, product.Price, scenario.ReasonCodes));
            recommendation.SendToManualReview();
        }
        else
        {
            best.IsRecommended = true;
            var action = DecideAction(best.Price, product.Price, scenario.Scenario);
            recommendation.SetOutcome(best.Price, action, best.PredictedDemand, best.ExpectedRevenue, best.ExpectedProfit, best.Margin,
                confidence, reasonCodesJson,
                _explanation.Build(scenario.Scenario, action, product.Price, best.Price, scenario.ReasonCodes));
        }

        await _recommendations.InsertAsync(recommendation, autoSave: true);
        return recommendation;
    }

    public async Task<PriceEvaluation> SimulateAsync(Guid productId, Guid shopId, decimal price)
    {
        var product = await _products.GetAsync(productId);
        var policy = await _policies.FirstOrDefaultAsync(x => x.ShopId == shopId);
        var demand = await _features.BuildAsync(productId, shopId);

        var forecast = await _client.ForecastAsync(new ForecastRequest
        {
            ProductId = productId,
            CategoryId = product.CategoryId,
            Currency = product.Currency,
            CurrentPrice = product.Price,
            History = demand.History,
            CandidatePrices = new List<decimal> { price },
            Mode = demand.Mode
        });

        var predicted = forecast.PerCandidate.FirstOrDefault()?.PredictedDemand ?? 0m;
        var econ = await _costing.GetUnitEconomicsAsync(productId, shopId, price);
        var profit = _profit.Compute(econ, predicted);
        var guard = _guardrails.Evaluate(econ, product.Price, policy);
        var pct = product.Price > 0m ? (price - product.Price) / product.Price : 0m;

        return new PriceEvaluation(price, pct, predicted, profit.Revenue, profit.Profit, profit.Margin, guard.Status, guard.Evaluations);
    }

    private async Task<decimal?> GetCompetitorAvgAsync(Guid productId)
    {
        var comps = await _competitors.GetListAsync(x => x.ProductId == productId && x.IsAvailable);
        return comps.Count == 0 ? null : comps.Average(c => c.Price);
    }

    private static ConfidenceLevel ToConfidence(AdvisorMode mode) => mode switch
    {
        AdvisorMode.FullPriceResponse => ConfidenceLevel.High,
        AdvisorMode.Standard => ConfidenceLevel.Medium,
        _ => ConfidenceLevel.Low
    };

    private static RecommendationAction DecideAction(decimal recommended, decimal current, PricingScenario scenario)
    {
        if (recommended > current)
        {
            return RecommendationAction.Raise;
        }
        if (recommended < current)
        {
            return scenario switch
            {
                PricingScenario.StaleInventory => RecommendationAction.ClearStock,
                PricingScenario.CompetitorUndercut => RecommendationAction.CompetitorMatch,
                _ => RecommendationAction.Discount
            };
        }
        return RecommendationAction.Keep;
    }
}
