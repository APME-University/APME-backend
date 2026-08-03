using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace APME.PricingAdvisor;

public interface IPricingDashboardAppService : IApplicationService
{
    Task<AttentionListDto> GetAttentionAsync(Guid shopId);

    /// <summary>Run attention detection for the shop now (on-demand refresh), then return the list.</summary>
    Task<AttentionListDto> RefreshAttentionAsync(Guid shopId);
}
