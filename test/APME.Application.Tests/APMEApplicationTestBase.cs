using Volo.Abp.Modularity;

namespace APME;

public abstract class APMEApplicationTestBase<TStartupModule> : APMETestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
