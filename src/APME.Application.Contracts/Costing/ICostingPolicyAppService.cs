using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace APME.Costing;

public interface ICostingPolicyAppService : IApplicationService
{
    Task<CostingPolicyDto?> GetByShopAsync(Guid shopId);

    Task<CostingPolicyDto> UpsertAsync(Guid shopId, CreateUpdateCostingPolicyDto input);
}
