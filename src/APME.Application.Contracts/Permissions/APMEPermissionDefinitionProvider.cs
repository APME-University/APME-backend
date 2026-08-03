using APME.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace APME.Permissions;

public class APMEPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(APMEPermissions.GroupName);

        var costing = myGroup.AddPermission(APMEPermissions.Costing.Default, L("Permission:Costing"));
        costing.AddChild(APMEPermissions.Costing.View, L("Permission:Costing.View"));
        costing.AddChild(APMEPermissions.Costing.ManageCost, L("Permission:Costing.ManageCost"));
        costing.AddChild(APMEPermissions.Costing.ManagePolicy, L("Permission:Costing.ManagePolicy"));

        var advisor = myGroup.AddPermission(APMEPermissions.PricingAdvisor.Default, L("Permission:PricingAdvisor"));
        advisor.AddChild(APMEPermissions.PricingAdvisor.View, L("Permission:PricingAdvisor.View"));
        advisor.AddChild(APMEPermissions.PricingAdvisor.Generate, L("Permission:PricingAdvisor.Generate"));
        advisor.AddChild(APMEPermissions.PricingAdvisor.Approve, L("Permission:PricingAdvisor.Approve"));
        advisor.AddChild(APMEPermissions.PricingAdvisor.ManagePolicy, L("Permission:PricingAdvisor.ManagePolicy"));
        advisor.AddChild(APMEPermissions.PricingAdvisor.ManageCompetitor, L("Permission:PricingAdvisor.ManageCompetitor"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<APMEResource>(name);
    }
}
