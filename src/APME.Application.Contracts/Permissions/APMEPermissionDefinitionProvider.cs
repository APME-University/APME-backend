using APME.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace APME.Permissions;

public class APMEPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(APMEPermissions.GroupName);
        //Define your own permissions here. Example:
        //myGroup.AddPermission(APMEPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<APMEResource>(name);
    }
}
