using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace APME.Dashboard;

public interface IDashboardAppService : IApplicationService
{
    /// <summary>
    /// Gets dashboard statistics for the current tenant
    /// </summary>
    /// <returns>Dashboard statistics for tenant dashboard</returns>
    Task<DashboardStatisticsDto> GetTenantDashboardStatisticsAsync();

    /// <summary>
    /// Gets dashboard statistics for the host
    /// </summary>
    /// <returns>Dashboard statistics for host dashboard</returns>
    Task<HostDashboardStatisticsDto> GetHostDashboardStatisticsAsync();
}
