using Volo.Abp.Modularity;

namespace APME;

[DependsOn(
    typeof(APMEDomainModule),
    typeof(APMETestBaseModule)
)]
public class APMEDomainTestModule : AbpModule
{

}
