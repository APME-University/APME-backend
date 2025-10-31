using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace APME.Data;

/* This is used if database provider does't define
 * IAPMEDbSchemaMigrator implementation.
 */
public class NullAPMEDbSchemaMigrator : IAPMEDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
