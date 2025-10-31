using APME.Localization;
using Volo.Abp.AspNetCore.Components;

namespace APME.Blazor.Client;

public abstract class APMEComponentBase : AbpComponentBase
{
    protected APMEComponentBase()
    {
        LocalizationResource = typeof(APMEResource);
    }
}
