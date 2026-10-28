using Microsoft.EntityFrameworkCore;

namespace Data.SQL
{
    public static class DataSqlServiceCollectionExtensions
    {
        /// <summary>One place for provider options, shared by DI and the design time factory.</summary>
        public static void ConfigureNpgsql(DbContextOptionsBuilder options, string connectionString)
        {
            options
                .UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsHistoryTable("__ef_migrations_history"))
                .UseSnakeCaseNamingConvention();
        }
    }
}
