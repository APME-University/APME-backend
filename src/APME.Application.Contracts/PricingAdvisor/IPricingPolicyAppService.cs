using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace APME.PricingAdvisor;

public interface IPricingPolicyAppService : IApplicationService
{
    Task<PricingPolicyDto?> GetByShopAsync(Guid shopId);
    Task<PricingPolicyDto> UpsertAsync(Guid shopId, CreateUpdatePricingPolicyDto input);
}
