using Volo.Abp.Modularity;

namespace APME;

[DependsOn(
    typeof(APMEApplicationModule),
    typeof(APMEDomainTestModule)
)]
public class APMEApplicationTestModule : AbpModule
{

}
