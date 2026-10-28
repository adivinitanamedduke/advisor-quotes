using Data.SQL;
using Data.SQL.EF;

namespace API.Middleware.DependencyInjection
{
    public static class DataLayerServiceCollection
    {
        public const string ConnectionStringName = "Quotes";

        public static IServiceCollection AddDataLayer(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException(
                    $"Connection string '{ConnectionStringName}' is missing. Set ConnectionStrings__{ConnectionStringName}.");

            services.AddDbContext<QuotesDbContext>(options =>
                DataSqlServiceCollectionExtensions.ConfigureNpgsql(options, connectionString));

            return services;
        }
    }
}
