using System;
using System.Linq;
using System.Threading.Tasks;
using APME.Permissions;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace APME.PricingAdvisor;

public class CompetitorPriceAppService :
    CrudAppService<CompetitorPrice, CompetitorPriceDto, Guid, GetCompetitorPriceListInput, CreateUpdateCompetitorPriceDto>,
    ICompetitorPriceAppService
{
    public CompetitorPriceAppService(IRepository<CompetitorPrice, Guid> repository) : base(repository)
    {
        GetPolicyName = APMEPermissions.PricingAdvisor.View;
        GetListPolicyName = APMEPermissions.PricingAdvisor.View;
        CreatePolicyName = APMEPermissions.PricingAdvisor.ManageCompetitor;
        UpdatePolicyName = APMEPermissions.PricingAdvisor.ManageCompetitor;
        DeletePolicyName = APMEPermissions.PricingAdvisor.ManageCompetitor;
    }

    protected override async Task<IQueryable<CompetitorPrice>> CreateFilteredQueryAsync(GetCompetitorPriceListInput input)
    {
        var query = await base.CreateFilteredQueryAsync(input);

        if (input.ShopId.HasValue) query = query.Where(x => x.ShopId == input.ShopId.Value);
        if (input.ProductId.HasValue) query = query.Where(x => x.ProductId == input.ProductId.Value);

        return query;
    }
}
