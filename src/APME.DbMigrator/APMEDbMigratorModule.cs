using APME.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace APME.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(APMEEntityFrameworkCoreModule),
    typeof(APMEApplicationContractsModule)
    )]
public class APMEDbMigratorModule : AbpModule
{
}
