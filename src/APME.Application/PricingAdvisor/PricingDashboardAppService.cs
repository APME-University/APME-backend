using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APME.Permissions;
using APME.Products;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace APME.PricingAdvisor;

public class PricingDashboardAppService : ApplicationService, IPricingDashboardAppService
{
    private readonly IRepository<PricingAttentionItem, Guid> _attention;
    private readonly IRepository<PricingRecommendation, Guid> _recommendations;
    private readonly IRepository<Product, Guid> _products;
    private readonly AttentionDetectionWorker _attentionWorker;

    public PricingDashboardAppService(
        IRepository<PricingAttentionItem, Guid> attention,
        IRepository<PricingRecommendation, Guid> recommendations,
        IRepository<Product, Guid> products,
        AttentionDetectionWorker attentionWorker)
    {
        _attention = attention;
        _recommendations = recommendations;
        _products = products;
        _attentionWorker = attentionWorker;
    }

    [Authorize(APMEPermissions.PricingAdvisor.Generate)]
    public async Task<AttentionListDto> RefreshAttentionAsync(Guid shopId)
    {
        await _attentionWorker.RunForShopAsync(CurrentTenant.Id, shopId);
        return await GetAttentionAsync(shopId);
    }

    [Authorize(APMEPermissions.PricingAdvisor.View)]
    public async Task<AttentionListDto> GetAttentionAsync(Guid shopId)
    {
        var items = await _attention.GetListAsync(x => x.ShopId == shopId && !x.IsResolved);
        var ordered = items.OrderByDescending(x => x.Priority).ToList();

        var pending = await _recommendations.CountAsync(x =>
            x.ShopId == shopId &&
            (x.Status == RecommendationStatus.Pending || x.Status == RecommendationStatus.ManualReview));

        var dtos = ObjectMapper.Map<List<PricingAttentionItem>, List<AttentionItemDto>>(ordered);

        // Enrich with product name / SKU / current price (read-time join, no schema change).
        var productIds = dtos.Select(d => d.ProductId).Distinct().ToList();
        if (productIds.Count > 0)
        {
            var products = await _products.GetListAsync(p => productIds.Contains(p.Id));
            var byId = products.ToDictionary(p => p.Id);
            foreach (var d in dtos)
            {
                if (byId.TryGetValue(d.ProductId, out var p))
                {
                    d.ProductName = p.Name;
                    d.ProductSku = p.SKU;
                    d.CurrentPrice = p.Price;
                }
            }
        }

        return new AttentionListDto
        {
            TotalOpportunity = ordered.Sum(x => x.EstimatedOpportunity),
            AttentionCount = ordered.Count,
            PendingApprovals = (int)pending,
            Items = dtos
        };
    }
}
