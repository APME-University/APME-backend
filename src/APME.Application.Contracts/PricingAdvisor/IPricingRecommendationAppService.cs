using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace APME.PricingAdvisor;

public interface IPricingRecommendationAppService : IApplicationService
{
    Task<RecommendationDto> GenerateAsync(GenerateRecommendationInput input);
    Task<SimulationResultDto> SimulateAsync(SimulatePriceInput input);
    Task<RecommendationDto> GetAsync(Guid id);
    Task<PagedResultDto<RecommendationDto>> GetListAsync(GetRecommendationListInput input);
    Task<RecommendationDto> ApproveAsync(Guid id, ApproveInput input);
    Task<RecommendationDto> RejectAsync(Guid id, ApproveInput input);
    Task<ProductPricingAnalysisDto> GetAnalysisAsync(Guid productId, Guid shopId);
    Task<ProductDemandHistoryDto> GetDemandHistoryAsync(Guid productId, Guid shopId);
}
