using System.Net;
using System.Text.Json;
using API.Observability;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;

namespace Tests.Integration.API.Observability
{
    /// <summary>Skips unless TEST_POSTGRES_CONNECTION points at a reachable PostgreSQL.</summary>
    public sealed class RequiresPostgresFactAttribute : FactAttribute
    {
        public const string ConnectionVariable = "TEST_POSTGRES_CONNECTION";

        public RequiresPostgresFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ConnectionVariable)))
            {
                Skip = $"Set {ConnectionVariable} to run tests that need PostgreSQL";
            }
        }
    }

    public class HealthEndpointsTests
    {
        private static WebApplicationFactory<Program> CreateFactory(string? connectionString = null) =>
            new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                // No migrations: the health checks must not depend on schema, and live must not need a database
                builder.UseSetting("Database:MigrateOnStartup", "false");
                if (connectionString is not null)
                {
                    builder.UseSetting("ConnectionStrings:Quotes", connectionString);
                }
            });

        [Fact]
        public async Task Live_WithoutAdviserHeader_Returns200WithTraceIdHeader()
        {
            // Arrange
            await using var factory = CreateFactory();
            using var client = factory.CreateClient();

            // Act
            var response = await client.GetAsync(HealthEndpoints.LivePath);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
            var traceId = Assert.Single(response.Headers.GetValues(TraceIdHeaderMiddleware.HeaderName));
            Assert.Matches("^[0-9a-f]{32}$", traceId);
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("Healthy", body.RootElement.GetProperty("status").GetString());
        }

        [Fact]
        public async Task UnknownRoute_Returns404WithTraceIdHeader()
        {
            // Arrange
            await using var factory = CreateFactory();
            using var client = factory.CreateClient();

            // Act
            var response = await client.GetAsync("/does-not-exist");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.True(response.Headers.Contains(TraceIdHeaderMiddleware.HeaderName));
        }

        [RequiresPostgresFact]
        public async Task Ready_WithReachableDatabase_Returns200()
        {
            // Arrange: the server's own "postgres" database, so this does not depend on the test database existing yet
            var connectionString = new NpgsqlConnectionStringBuilder(
                Environment.GetEnvironmentVariable(RequiresPostgresFactAttribute.ConnectionVariable)) { Database = "postgres" };
            await using var factory = CreateFactory(connectionString.ConnectionString);
            using var client = factory.CreateClient();

            // Act
            var response = await client.GetAsync(HealthEndpoints.ReadyPath);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("Healthy", body.RootElement.GetProperty("status").GetString());
            Assert.Equal("postgres", body.RootElement.GetProperty("checks")[0].GetProperty("name").GetString());
        }

        [Fact]
        public async Task Ready_WithUnreachableDatabase_Returns503()
        {
            // Arrange: nothing listens on port 1
            await using var factory = CreateFactory("Host=127.0.0.1;Port=1;Database=none;Username=none;Password=none;Timeout=2");
            using var client = factory.CreateClient();

            // Act
            var response = await client.GetAsync(HealthEndpoints.ReadyPath);

            // Assert
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.True(response.Headers.Contains(TraceIdHeaderMiddleware.HeaderName));
        }
    }
}
