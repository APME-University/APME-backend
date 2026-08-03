using System;
using System.Threading.Tasks;
using APME.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace APME.Costing;

public class CostingPolicyAppService : ApplicationService, ICostingPolicyAppService
{
    private readonly IRepository<CostingPolicy, Guid> _policies;

    public CostingPolicyAppService(IRepository<CostingPolicy, Guid> policies)
    {
        _policies = policies;
    }

    [Authorize(APMEPermissions.Costing.View)]
    public async Task<CostingPolicyDto?> GetByShopAsync(Guid shopId)
    {
        var policy = await _policies.FirstOrDefaultAsync(x => x.ShopId == shopId);
        return policy == null ? null : ObjectMapper.Map<CostingPolicy, CostingPolicyDto>(policy);
    }

    [Authorize(APMEPermissions.Costing.ManagePolicy)]
    public async Task<CostingPolicyDto> UpsertAsync(Guid shopId, CreateUpdateCostingPolicyDto input)
    {
        var policy = await _policies.FirstOrDefaultAsync(x => x.ShopId == shopId);
        var isNew = policy == null;

        policy ??= new CostingPolicy(GuidGenerator.Create(), CurrentTenant.Id, shopId, input.Currency);

        policy.DefaultMethod = input.DefaultMethod;
        policy.MarginBasis = input.MarginBasis;
        policy.Currency = input.Currency;
        policy.MarketplaceFeePct = input.MarketplaceFeePct;
        policy.PaymentFeePct = input.PaymentFeePct;
        policy.PaymentFixedPerUnit = input.PaymentFixedPerUnit;
        policy.FulfillmentPerUnit = input.FulfillmentPerUnit;
        policy.StoragePerUnitMonth = input.StoragePerUnitMonth;
        policy.ReturnsReservePct = input.ReturnsReservePct;
        policy.AdPerUnit = input.AdPerUnit;
        policy.IsActive = input.IsActive;

        if (isNew)
        {
            await _policies.InsertAsync(policy, autoSave: true);
        }
        else
        {
            await _policies.UpdateAsync(policy, autoSave: true);
        }

        return ObjectMapper.Map<CostingPolicy, CostingPolicyDto>(policy);
    }
}
