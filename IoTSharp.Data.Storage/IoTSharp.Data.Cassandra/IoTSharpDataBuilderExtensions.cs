using IoTSharp.Data;
using IoTSharp.Data.Cassandra;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class IoTSharpDataBuilderExtensions
    {
        public static void ConfigureCassandra(this IServiceCollection services, string connectionString, int poolSize, IHealthChecksBuilder checksBuilder, HealthChecksUIBuilder healthChecksUI)
        {
            services.AddEntityFrameworkCassandra();
            services.AddSingleton<IDataBaseModelBuilderOptions>(c => new CassandraModelBuilderOptions());
            services.AddDbContextPool<ApplicationDbContext>(builder =>
            {
                builder.UseCassandra(connectionString, "", s => s.MigrationsAssembly("IoTSharp.Data.Cassandra").UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
                builder.UseInternalServiceProvider(services.BuildServiceProvider());
            }
     , poolSize);
            checksBuilder.AddCassandra(connectionString, name: "IoTSharp.Data.Cassandra");
            //   healthChecksUI.AddSqliteStorage("Data Source=health_checks.db");
        }
    }
}