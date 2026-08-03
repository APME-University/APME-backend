using System;
using Volo.Abp.Application.Services;

namespace APME.PricingAdvisor;

public interface ICompetitorPriceAppService :
    ICrudAppService<CompetitorPriceDto, Guid, GetCompetitorPriceListInput, CreateUpdateCompetitorPriceDto>
{
}
