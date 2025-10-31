using APME.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace APME.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class APMEController : AbpControllerBase
{
    protected APMEController()
    {
        LocalizationResource = typeof(APMEResource);
    }
}
