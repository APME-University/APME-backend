using System.Threading.Tasks;
using APME.Dashboard;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace APME.Controllers;

[ApiController]
[Route("api/app/dashboard")]
public class DashboardController : AbpControllerBase
{
    private readonly IDashboardAppService _dashboardAppService;

    public DashboardController(IDashboardAppService dashboardAppService)
    {
        _dashboardAppService = dashboardAppService;
    }

    [HttpGet]
    [Route("tenant-statistics")]
    public async Task<DashboardStatisticsDto> GetTenantStatisticsAsync()
    {
        return await _dashboardAppService.GetTenantDashboardStatisticsAsync();
    }

    [HttpGet]
    [Route("host-statistics")]
    public async Task<HostDashboardStatisticsDto> GetHostStatisticsAsync()
    {
        return await _dashboardAppService.GetHostDashboardStatisticsAsync();
    }
}
