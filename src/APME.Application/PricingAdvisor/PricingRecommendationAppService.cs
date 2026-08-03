using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APME.Costing;
using APME.Permissions;
using APME.Products;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.EventBus.Distributed;

namespace APME.PricingAdvisor;

public class PricingRecommendationAppService : ApplicationService, IPricingRecommendationAppService
{
    private readonly IPricingRecommendationManager _manager;
    private readonly IRepository<PricingRecommendation, Guid> _recommendations;
    private readonly IRepository<Product, Guid> _products;
    private readonly IRepository<CompetitorPrice, Guid> _competitors;
    private readonly IRepository<PriceChangeLog, Guid> _priceLogs;
    private readonly ICostingService _costing;
    private readonly IDemandFeatureBuilder _demandFeatureBuilder;
    private readonly IDistributedEventBus _eventBus;

    public PricingRecommendationAppService(
        IPricingRecommendationManager manager,
        IRepository<PricingRecommendation, Guid> recommendations,
        IRepository<Product, Guid> products,
        IRepository<CompetitorPrice, Guid> competitors,
        IRepository<PriceChangeLog, Guid> priceLogs,
        ICostingService costing,
        IDemandFeatureBuilder demandFeatureBuilder,
        IDistributedEventBus eventBus)
    {
        _manager = manager;
        _recommendations = recommendations;
        _products = products;
        _competitors = competitors;
        _priceLogs = priceLogs;
        _costing = costing;
        _demandFeatureBuilder = demandFeatureBuilder;
        _eventBus = eventBus;
    }

    [Authorize(APMEPermissions.PricingAdvisor.Generate)]
    public async Task<RecommendationDto> GenerateAsync(GenerateRecommendationInput input)
    {
        var rec = await _manager.GenerateAsync(input.ProductId, input.ShopId);
        return ObjectMapper.Map<PricingRecommendation, RecommendationDto>(rec);
    }

    [Authorize(APMEPermissions.PricingAdvisor.View)]
    public async Task<SimulationResultDto> SimulateAsync(SimulatePriceInput input)
    {
        var eval = await _manager.SimulateAsync(input.ProductId, input.ShopId, input.Price);
        return new SimulationResultDto
        {
            Price = eval.Price,
            PctChange = eval.PctChange,
            PredictedDemand = eval.PredictedDemand,
            ExpectedRevenue = eval.ExpectedRevenue,
            ExpectedProfit = eval.ExpectedProfit,
            Margin = eval.Margin,
            GuardrailStatus = eval.GuardrailStatus,
            Guardrails = eval.Guardrails
                .Select(g => new GuardrailEvaluationDto { Rule = g.Rule, Status = g.Status, Message = g.Message })
                .ToList()
        };
    }

    [Authorize(APMEPermissions.PricingAdvisor.View)]
    public async Task<RecommendationDto> GetAsync(Guid id)
    {
        var rec = await GetWithCandidatesAsync(id);
        return ObjectMapper.Map<PricingRecommendation, RecommendationDto>(rec);
    }

    [Authorize(APMEPermissions.PricingAdvisor.View)]
    public async Task<PagedResultDto<RecommendationDto>> GetListAsync(GetRecommendationListInput input)
    {
        var query = await _recommendations.WithDetailsAsync(x => x.Candidates);

        if (input.ShopId.HasValue) query = query.Where(x => x.ShopId == input.ShopId.Value);
        if (input.ProductId.HasValue) query = query.Where(x => x.ProductId == input.ProductId.Value);
        if (input.Status.HasValue) query = query.Where(x => x.Status == input.Status.Value);

        var total = await AsyncExecuter.CountAsync(query);
        var items = await AsyncExecuter.ToListAsync(
            query.OrderByDescending(x => x.CreationTime).Skip(input.SkipCount).Take(input.MaxResultCount));

        var dtos = ObjectMapper.Map<List<PricingRecommendation>, List<RecommendationDto>>(items);

        // Enrich with product names (read-time join, no schema change).
        var productIds = dtos.Select(d => d.ProductId).Distinct().ToList();
        if (productIds.Count > 0)
        {
            var products = await _products.GetListAsync(p => productIds.Contains(p.Id));
            var byId = products.ToDictionary(p => p.Id, p => p.Name);
            foreach (var d in dtos)
            {
                if (byId.TryGetValue(d.ProductId, out var name)) d.ProductName = name;
            }
        }

        return new PagedResultDto<RecommendationDto>(total, dtos);
    }

    [Authorize(APMEPermissions.PricingAdvisor.Approve)]
    public async Task<RecommendationDto> ApproveAsync(Guid id, ApproveInput input)
    {
        var rec = await GetWithCandidatesAsync(id);
        rec.Approve(CurrentUser.Id ?? Guid.Empty, Clock.Now);

        var product = await _products.GetAsync(rec.ProductId);
        var oldPrice = product.Price;
        product.UpdatePrice(rec.RecommendedPrice);
        await _products.UpdateAsync(product);

        await _priceLogs.InsertAsync(new PriceChangeLog(
            GuidGenerator.Create(), CurrentTenant.Id, rec.ProductId, rec.ShopId,
            oldPrice, rec.RecommendedPrice, PriceChangeSource.AdvisorApproved, Clock.Now, CurrentUser.Id));

        rec.MarkApplied(Clock.Now);
        await _recommendations.UpdateAsync(rec);

        await _eventBus.PublishAsync(new RecommendationAppliedEto
        {
            RecommendationId = rec.Id,
            ProductId = rec.ProductId,
            ShopId = rec.ShopId,
            TenantId = CurrentTenant.Id,
            OldPrice = oldPrice,
            NewPrice = rec.RecommendedPrice,
            AppliedAt = Clock.Now
        });

        return ObjectMapper.Map<PricingRecommendation, RecommendationDto>(rec);
    }

    [Authorize(APMEPermissions.PricingAdvisor.Approve)]
    public async Task<RecommendationDto> RejectAsync(Guid id, ApproveInput input)
    {
        var rec = await GetWithCandidatesAsync(id);
        rec.Reject(CurrentUser.Id ?? Guid.Empty, Clock.Now, input.Note);
        await _recommendations.UpdateAsync(rec);
        return ObjectMapper.Map<PricingRecommendation, RecommendationDto>(rec);
    }

    [Authorize(APMEPermissions.PricingAdvisor.View)]
    public async Task<ProductPricingAnalysisDto> GetAnalysisAsync(Guid productId, Guid shopId)
    {
        var product = await _products.GetAsync(productId);
        var econ = await _costing.GetUnitEconomicsAsync(productId, shopId, product.Price);
        var comps = await _competitors.GetListAsync(x => x.ProductId == productId && x.IsAvailable);

        return new ProductPricingAnalysisDto
        {
            ProductId = productId,
            ShopId = shopId,
            CurrentPrice = product.Price,
            LandedCost = econ.LandedCost,
            ContributionMargin = econ.ContributionMargin,
            BreakEvenFloor = econ.BreakEvenFloor,
            StockQuantity = product.StockQuantity,
            CompetitorAvg = comps.Count > 0 ? comps.Average(c => c.Price) : null,
            CompetitorCount = comps.Count
        };
    }

    [Authorize(APMEPermissions.PricingAdvisor.View)]
    public async Task<ProductDemandHistoryDto> GetDemandHistoryAsync(Guid productId, Guid shopId)
    {
        var ctx = await _demandFeatureBuilder.BuildAsync(productId, shopId);
        return new ProductDemandHistoryDto
        {
            Mode = ctx.Mode,
            TotalUnits = ctx.TotalUnits,
            RecentUnits = ctx.RecentUnits,
            AverageUnits = ctx.AverageUnits,
            PriceChangeCount = ctx.PriceChangeCount,
            Points = ctx.History
                .Select(p => new DemandHistoryPointDto { Period = p.Period, Units = p.Units, Price = p.Price })
                .ToList()
        };
    }

    private async Task<PricingRecommendation> GetWithCandidatesAsync(Guid id)
    {
        var query = await _recommendations.WithDetailsAsync(x => x.Candidates);
        var rec = await AsyncExecuter.FirstOrDefaultAsync(query.Where(x => x.Id == id));
        if (rec == null)
        {
            throw new EntityNotFoundException(typeof(PricingRecommendation), id);
        }
        return rec;
    }
}
