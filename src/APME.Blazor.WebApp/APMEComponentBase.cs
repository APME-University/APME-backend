using APME.Localization;
using Volo.Abp.AspNetCore.Components;

namespace APME.Blazor.WebApp;

public abstract class APMEComponentBase : AbpComponentBase
{
    protected APMEComponentBase()
    {
        LocalizationResource = typeof(APMEResource);
    }
}
