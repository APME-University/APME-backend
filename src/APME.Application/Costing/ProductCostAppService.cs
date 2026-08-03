using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using APME.Permissions;
using APME.Products;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace APME.Costing;

public class ProductCostAppService : ApplicationService, IProductCostAppService
{
    private readonly ICostingService _costing;
    private readonly IRepository<ProductCostProfile, Guid> _profiles;
    private readonly IRepository<CostComponentEntry, Guid> _entries;
    private readonly IRepository<Product, Guid> _products;

    public ProductCostAppService(
        ICostingService costing,
        IRepository<ProductCostProfile, Guid> profiles,
        IRepository<CostComponentEntry, Guid> entries,
        IRepository<Product, Guid> products)
    {
        _costing = costing;
        _profiles = profiles;
        _entries = entries;
        _products = products;
    }

    [Authorize(APMEPermissions.Costing.View)]
    public async Task<ProductCostDto> GetByProductAsync(Guid productId)
    {
        var profile = await _profiles.FirstOrDefaultAsync(x => x.ProductId == productId);
        var components = await _entries.GetListAsync(x => x.ProductId == productId);

        return new ProductCostDto
        {
            ProductId = productId,
            ShopId = profile?.ShopId ?? Guid.Empty,
            Method = profile?.Method ?? CostingMethod.MovingAverage,
            Currency = profile?.Currency ?? "USD",
            CurrentLandedUnitCost = profile?.CurrentLandedUnitCost ?? 0m,
            QtyOnHand = profile?.QtyOnHand ?? 0,
            LastCostedAt = profile?.LastCostedAt,
            Components = ObjectMapper.Map<List<CostComponentEntry>, List<CostComponentDto>>(components)
        };
    }

    [Authorize(APMEPermissions.Costing.ManageCost)]
    public async Task<ProductCostDto> SetCostAsync(SetProductCostInput input)
    {
        var profile = await _costing.EnsureProfileAsync(input.ProductId, input.ShopId, input.Currency);
        profile.Method = input.Method;
        await _profiles.UpdateAsync(profile);

        // Replace the product's manual inventory components, then recompute the landed cost.
        await _entries.DeleteAsync(
            x => x.ProductId == input.ProductId && x.Source == CostSource.Manual && x.Plane == CostPlane.Inventory,
            autoSave: true);

        if (input.Components != null && input.Components.Any())
        {
            foreach (var c in input.Components)
            {
                await _entries.InsertAsync(new CostComponentEntry(
                    GuidGenerator.Create(), CurrentTenant.Id, input.ProductId, input.ShopId,
                    c.ComponentType, c.ValueType, c.Amount, Clock.Now, CostSource.Manual, input.Currency),
                    autoSave: true);
            }
        }
        else if (input.SimpleUnitCost.HasValue)
        {
            await _entries.InsertAsync(new CostComponentEntry(
                GuidGenerator.Create(), CurrentTenant.Id, input.ProductId, input.ShopId,
                CostComponentType.SupplierPrice, CostValueType.PerUnit, input.SimpleUnitCost.Value,
                Clock.Now, CostSource.Manual, input.Currency),
                autoSave: true);
        }

        await _costing.RecomputeLandedFromComponentsAsync(input.ProductId, input.ShopId);
        return await GetByProductAsync(input.ProductId);
    }

    [Authorize(APMEPermissions.Costing.ManageCost)]
    public Task RecordReceiptAsync(RecordReceiptInput input)
        => _costing.ApplyPurchaseReceiptAsync(
            input.ProductId, input.ShopId, input.Quantity, input.UnitCost, input.ExtraLandedPerUnit, input.Reference);

    [Authorize(APMEPermissions.Costing.View)]
    public async Task<UnitEconomicsDto> GetUnitEconomicsAsync(Guid productId, Guid shopId, decimal price)
    {
        var r = await _costing.GetUnitEconomicsAsync(productId, shopId, price);
        return new UnitEconomicsDto
        {
            Price = r.Price,
            LandedCost = r.LandedCost,
            VariableSellingCost = r.VariableSellingCost,
            GrossProfit = r.GrossProfit,
            GrossMargin = r.GrossMargin,
            ContributionMargin = r.ContributionMargin,
            ContributionRatio = r.ContributionRatio,
            Markup = r.Markup,
            BreakEvenFloor = r.BreakEvenFloor
        };
    }

    [Authorize(APMEPermissions.Costing.ManageCost)]
    public async Task<CostImportResultDto> ImportCostsAsync(List<CostImportRow> rows)
    {
        var result = new CostImportResultDto();

        foreach (var row in rows ?? new List<CostImportRow>())
        {
            if (string.IsNullOrWhiteSpace(row.Sku))
            {
                result.Skipped++;
                result.Messages.Add("Empty SKU skipped");
                continue;
            }

            var product = await _products.FirstOrDefaultAsync(x => x.ShopId == row.ShopId && x.SKU == row.Sku);
            if (product == null)
            {
                result.Skipped++;
                result.Messages.Add($"SKU '{row.Sku}' not found in shop");
                continue;
            }

            await SetCostAsync(new SetProductCostInput
            {
                ProductId = product.Id,
                ShopId = row.ShopId,
                Currency = row.Currency,
                SimpleUnitCost = row.UnitCost
            });
            result.Imported++;
        }

        return result;
    }
}
