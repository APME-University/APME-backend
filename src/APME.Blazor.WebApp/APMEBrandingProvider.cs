using Microsoft.Extensions.Localization;
using APME.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace APME.Blazor.WebApp;

[Dependency(ReplaceServices = true)]
public class APMEBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<APMEResource> _localizer;

    public APMEBrandingProvider(IStringLocalizer<APMEResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
