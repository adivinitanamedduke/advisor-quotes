using Data.SQL;
using Data.SQL.EF;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pricing;
using Testcontainers.PostgreSql;

namespace Tests.Integration.Infrastructure
{
    /// <summary>
    /// One API host and one migrated database for the whole collection. Tests call ResetDatabaseAsync first,
    /// so each starts from empty tables.
    /// </summary>
    public sealed class QuotesApiFixture : IAsyncLifetime
    {
        private PostgreSqlContainer? _container;
        private WebApplicationFactory<Program>? _factory;
        private string _connectionString = string.Empty;

        public WebApplicationFactory<Program> Factory =>
            _factory ?? throw new InvalidOperationException(PostgresAvailability.SkipReason ?? "Fixture not initialised");

        public async Task InitializeAsync()
        {
            if (PostgresAvailability.SkipReason is not null)
            {
                return;
            }

            if (PostgresAvailability.UseTestcontainers)
            {
                _container = new PostgreSqlBuilder("postgres:16-alpine").Build();
                await _container.StartAsync();
                _connectionString = _container.GetConnectionString();
            }
            else
            {
                _connectionString = PostgresAvailability.ConnectionString!;
            }

            await using (var db = CreateDbContext())
            {
                await db.Database.MigrateAsync();
            }

            _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("IntegrationTests");
                builder.UseSetting("ConnectionStrings:Quotes", _connectionString);
                builder.UseSetting("Database:MigrateOnStartup", "false");
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IPremiumCalculator>();
                    services.AddSingleton<IPremiumCalculator, FakePremiumCalculator>();
                });
            });
        }

        public QuotesDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<QuotesDbContext>();
            DataSqlServiceCollectionExtensions.ConfigureNpgsql(options, _connectionString);
            return new QuotesDbContext(options.Options);
        }

        public async Task ResetDatabaseAsync()
        {
            await using var db = CreateDbContext();
            await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE quote_status_changes, quotes");
        }

        public async Task DisposeAsync()
        {
            if (_factory is not null)
            {
                await _factory.DisposeAsync();
            }

            if (_container is not null)
            {
                await _container.DisposeAsync();
            }
        }
    }

    [CollectionDefinition(Name)]
    public sealed class PostgresCollection : ICollectionFixture<QuotesApiFixture>
    {
        public const string Name = "Postgres";
    }
}
