using System;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;

namespace APME.Costing;

public class CostingService : DomainService, ICostingService
{
    private readonly IRepository<ProductCostProfile, Guid> _profiles;
    private readonly IRepository<CostComponentEntry, Guid> _entries;
    private readonly IRepository<CostingPolicy, Guid> _policies;

    public CostingService(
        IRepository<ProductCostProfile, Guid> profiles,
        IRepository<CostComponentEntry, Guid> entries,
        IRepository<CostingPolicy, Guid> policies)
    {
        _profiles = profiles;
        _entries = entries;
        _policies = policies;
    }

    public async Task<decimal> GetLandedUnitCostAsync(Guid productId)
    {
        var profile = await _profiles.FirstOrDefaultAsync(x => x.ProductId == productId);
        return profile?.CurrentLandedUnitCost ?? 0m;
    }

    public async Task<UnitEconomicsResult> GetUnitEconomicsAsync(Guid productId, Guid shopId, decimal price)
    {
        var landed = await GetLandedUnitCostAsync(productId);
        var policy = await _policies.FirstOrDefaultAsync(x => x.ShopId == shopId);

        var perUnit = policy?.PerUnitOperatingCost ?? 0m;
        var feePct = policy?.FeePct ?? 0m;

        return UnitEconomics.Compute(price, landed, perUnit, feePct);
    }

    public async Task ApplyPurchaseReceiptAsync(Guid productId, Guid shopId, int qty, decimal unitCost, decimal extraLandedPerUnit, string? reference = null)
    {
        var profile = await EnsureProfileAsync(productId, shopId);
        var occurredAt = Clock.Now;
        var unitLanded = unitCost + extraLandedPerUnit;

        profile.ApplyReceipt(qty, unitLanded, occurredAt);
        await _profiles.UpdateAsync(profile);

        await _entries.InsertAsync(new CostComponentEntry(
            GuidGenerator.Create(), CurrentTenant.Id, productId, shopId,
            CostComponentType.SupplierPrice, CostValueType.PerUnit, unitCost, occurredAt,
            CostSource.PurchaseReceipt, profile.Currency) { Reference = reference });

        if (extraLandedPerUnit > 0m)
        {
            await _entries.InsertAsync(new CostComponentEntry(
                GuidGenerator.Create(), CurrentTenant.Id, productId, shopId,
                CostComponentType.OtherInventory, CostValueType.PerUnit, extraLandedPerUnit, occurredAt,
                CostSource.PurchaseReceipt, profile.Currency) { Reference = reference, Note = "Allocated landed extras" });
        }
    }

    public async Task<decimal> RecomputeLandedFromComponentsAsync(Guid productId, Guid shopId)
    {
        var active = await _entries.GetListAsync(x =>
            x.ProductId == productId &&
            x.Plane == CostPlane.Inventory &&
            x.ValueType == CostValueType.PerUnit &&
            x.EffectiveTo == null);

        var landed = active.Sum(x => x.Amount);

        var profile = await EnsureProfileAsync(productId, shopId);
        profile.SetLandedUnitCost(landed, Clock.Now);
        await _profiles.UpdateAsync(profile);
        return landed;
    }

    public async Task<ProductCostProfile> EnsureProfileAsync(Guid productId, Guid shopId, string currency = "USD")
    {
        var profile = await _profiles.FirstOrDefaultAsync(x => x.ProductId == productId);
        if (profile != null)
        {
            return profile;
        }

        profile = new ProductCostProfile(GuidGenerator.Create(), CurrentTenant.Id, productId, shopId, currency);
        return await _profiles.InsertAsync(profile, autoSave: true);
    }
}
