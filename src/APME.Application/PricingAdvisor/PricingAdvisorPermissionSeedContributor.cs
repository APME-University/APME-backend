using System.Threading.Tasks;
using APME.Permissions;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.PermissionManagement;

namespace APME.PricingAdvisor;

/// <summary>Grants the Pricing Advisor permissions to the admin role on seed (idempotent).</summary>
public class PricingAdvisorPermissionSeedContributor : IDataSeedContributor, ITransientDependency
{
    private const string AdminRole = "admin";
    private readonly IPermissionManager _permissionManager;

    public PricingAdvisorPermissionSeedContributor(IPermissionManager permissionManager)
    {
        _permissionManager = permissionManager;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        await _permissionManager.SetForRoleAsync(AdminRole, APMEPermissions.PricingAdvisor.View, true);
        await _permissionManager.SetForRoleAsync(AdminRole, APMEPermissions.PricingAdvisor.Generate, true);
        await _permissionManager.SetForRoleAsync(AdminRole, APMEPermissions.PricingAdvisor.Approve, true);
        await _permissionManager.SetForRoleAsync(AdminRole, APMEPermissions.PricingAdvisor.ManagePolicy, true);
        await _permissionManager.SetForRoleAsync(AdminRole, APMEPermissions.PricingAdvisor.ManageCompetitor, true);
    }
}
