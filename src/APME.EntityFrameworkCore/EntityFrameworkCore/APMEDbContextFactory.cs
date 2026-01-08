using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace APME.EntityFrameworkCore;

/* This class is needed for EF Core console commands
 * (like Add-Migration and Update-Database commands) */
public class APMEDbContextFactory : IDesignTimeDbContextFactory<APMEDbContext>
{
    public APMEDbContext CreateDbContext(string[] args)
    {
        // https://www.npgsql.org/efcore/release-notes/6.0.html#opting-out-of-the-new-timestamp-mapping-logic
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        APMEEfCoreEntityExtensionMappings.Configure();

        var configuration = BuildConfiguration();
        var connectionString = configuration.GetConnectionString("Default");

        // Configure NpgsqlDataSource with pgvector support
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.UseVector();
        var dataSource = dataSourceBuilder.Build();

        var builder = new DbContextOptionsBuilder<APMEDbContext>()
            .UseNpgsql(dataSource, npgsqlOptions =>
            {
                npgsqlOptions.UseVector();
            });

        return new APMEDbContext(builder.Options);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../APME.DbMigrator/"))
            .AddJsonFile("appsettings.json", optional: false);

        return builder.Build();
    }
}
