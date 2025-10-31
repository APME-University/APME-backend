using Volo.Abp.Modularity;

namespace APME;

/* Inherit from this class for your domain layer tests. */
public abstract class APMEDomainTestBase<TStartupModule> : APMETestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
