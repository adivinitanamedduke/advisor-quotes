using System.Net;
using API.Middleware.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Tests.Integration.API
{
    /// <summary>The product list needs no database, so these run without PostgreSQL.</summary>
    public class CorsTests
    {
        private const string UiOrigin = "http://localhost:5173";

        private static WebApplicationFactory<Program> CreateFactory(string? configuredOrigin = null) =>
            new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseSetting("Database:MigrateOnStartup", "false");
                if (configuredOrigin is not null)
                {
                    builder.UseSetting($"{CorsServiceCollection.UiOriginsKey}:0", configuredOrigin);
                }
            });

        [Fact]
        public async Task Preflight_FromUiOrigin_AllowsAdviserAndIdempotencyHeaders()
        {
            // Arrange
            await using var factory = CreateFactory();
            using var client = factory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/Quote");
            request.Headers.Add("Origin", UiOrigin);
            request.Headers.Add("Access-Control-Request-Method", "POST");
            request.Headers.Add("Access-Control-Request-Headers", "content-type,x-adviser-id,idempotency-key");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(UiOrigin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
            var allowed = string.Join(",", response.Headers.GetValues("Access-Control-Allow-Headers")).ToLowerInvariant();
            Assert.Contains("x-adviser-id", allowed);
            Assert.Contains("idempotency-key", allowed);
        }

        [Fact]
        public async Task Get_FromUiOrigin_ExposesReplayTraceAndLocationHeaders()
        {
            // Arrange
            await using var factory = CreateFactory();
            using var client = factory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/Product");
            request.Headers.Add("Origin", UiOrigin);

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var exposed = string.Join(",", response.Headers.GetValues("Access-Control-Expose-Headers"));
            Assert.Contains("Idempotent-Replayed", exposed);
            Assert.Contains("X-Trace-Id", exposed);
            Assert.Contains("Location", exposed);
        }

        [Fact]
        public async Task Get_FromUnknownOrigin_HasNoAllowOriginHeader()
        {
            // Arrange
            await using var factory = CreateFactory();
            using var client = factory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/Product");
            request.Headers.Add("Origin", "http://evil.example");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
        }

        [Fact]
        public async Task Get_FromConfiguredOrigin_IsAllowed()
        {
            // Arrange
            const string deployedUi = "https://quotes.example.test";
            await using var factory = CreateFactory(deployedUi);
            using var client = factory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/Product");
            request.Headers.Add("Origin", deployedUi);

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(deployedUi, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        }
    }
}
