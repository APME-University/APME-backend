using System;
using System.Threading.Tasks;
using APME.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace APME.PricingAdvisor;

public class PricingPolicyAppService : ApplicationService, IPricingPolicyAppService
{
    private readonly IRepository<PricingPolicy, Guid> _policies;

    public PricingPolicyAppService(IRepository<PricingPolicy, Guid> policies)
    {
        _policies = policies;
    }

    [Authorize(APMEPermissions.PricingAdvisor.View)]
    public async Task<PricingPolicyDto?> GetByShopAsync(Guid shopId)
    {
        var policy = await _policies.FirstOrDefaultAsync(x => x.ShopId == shopId);
        return policy == null ? null : ObjectMapper.Map<PricingPolicy, PricingPolicyDto>(policy);
    }

    [Authorize(APMEPermissions.PricingAdvisor.ManagePolicy)]
    public async Task<PricingPolicyDto> UpsertAsync(Guid shopId, CreateUpdatePricingPolicyDto input)
    {
        var policy = await _policies.FirstOrDefaultAsync(x => x.ShopId == shopId);
        var isNew = policy == null;

        policy ??= new PricingPolicy(GuidGenerator.Create(), CurrentTenant.Id, shopId);

        policy.MinMarginPct = input.MinMarginPct;
        policy.MaxDiscountPct = input.MaxDiscountPct;
        policy.MaxIncreasePct = input.MaxIncreasePct;
        policy.CandidateGridJson = input.CandidateGridJson;
        policy.AutoApplyEnabled = input.AutoApplyEnabled;
        policy.IsActive = input.IsActive;

        if (isNew)
        {
            await _policies.InsertAsync(policy, autoSave: true);
        }
        else
        {
            await _policies.UpdateAsync(policy, autoSave: true);
        }

        return ObjectMapper.Map<PricingPolicy, PricingPolicyDto>(policy);
    }
}
