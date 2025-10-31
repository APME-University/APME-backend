using Volo.Abp.Settings;

namespace APME.Settings;

public class APMESettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(APMESettings.MySetting1));
    }
}
