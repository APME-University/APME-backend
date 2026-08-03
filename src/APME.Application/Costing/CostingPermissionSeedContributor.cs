using System.Threading.Tasks;
using APME.Permissions;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.PermissionManagement;

namespace APME.Costing;

/// <summary>
/// Grants the Costing permissions to the admin role on seed (host + each tenant), so the module is
/// usable out of the box. Idempotent — runs via APME.DbMigrator.
/// </summary>
public class CostingPermissionSeedContributor : IDataSeedContributor, ITransientDependency
{
    private const string AdminRole = "admin";
    private readonly IPermissionManager _permissionManager;

    public CostingPermissionSeedContributor(IPermissionManager permissionManager)
    {
        _permissionManager = permissionManager;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        await _permissionManager.SetForRoleAsync(AdminRole, APMEPermissions.Costing.View, true);
        await _permissionManager.SetForRoleAsync(AdminRole, APMEPermissions.Costing.ManageCost, true);
        await _permissionManager.SetForRoleAsync(AdminRole, APMEPermissions.Costing.ManagePolicy, true);
    }
}
