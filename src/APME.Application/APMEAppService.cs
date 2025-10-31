using System;
using System.Collections.Generic;
using System.Text;
using APME.Localization;
using Volo.Abp.Application.Services;

namespace APME;

/* Inherit your application services from this class.
 */
public abstract class APMEAppService : ApplicationService
{
    protected APMEAppService()
    {
        LocalizationResource = typeof(APMEResource);
    }
}
