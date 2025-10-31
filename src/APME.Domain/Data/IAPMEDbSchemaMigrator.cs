using System.Threading.Tasks;

namespace APME.Data;

public interface IAPMEDbSchemaMigrator
{
    Task MigrateAsync();
}
