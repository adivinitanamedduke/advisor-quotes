using System.Text.Json;
using Data.SQL.EF;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace API.Observability
{
    /// <summary>
    /// /health/live: the process is up. /health/ready: PostgreSQL is reachable through the DbContext.
    /// Both sit outside the versioned API, so they need no X-Adviser-Id.
    /// </summary>
    public static class HealthEndpoints
    {
        public const string LivePath = "/health/live";
        public const string ReadyPath = "/health/ready";

        private const string LiveTag = "live";
        private const string ReadyTag = "ready";

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public static IServiceCollection AddHealthEndpoints(this IServiceCollection services)
        {
            services
                .AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy(), tags: [LiveTag])
                .AddDbContextCheck<QuotesDbContext>("postgres", tags: [ReadyTag]);

            return services;
        }

        public static WebApplication MapHealthEndpoints(this WebApplication app)
        {
            app.MapHealthChecks(LivePath, OptionsFor(LiveTag));
            app.MapHealthChecks(ReadyPath, OptionsFor(ReadyTag));
            return app;
        }

        private static HealthCheckOptions OptionsFor(string tag) => new()
        {
            Predicate = check => check.Tags.Contains(tag),
            ResponseWriter = WriteJsonAsync
        };

        /// <summary>Status and per-check timings only; exception details stay in the logs.</summary>
        public static Task WriteJsonAsync(HttpContext context, HealthReport report)
        {
            var body = new
            {
                status = report.Status.ToString(),
                totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 1),
                checks = report.Entries.Select(entry => new
                {
                    name = entry.Key,
                    status = entry.Value.Status.ToString(),
                    durationMs = Math.Round(entry.Value.Duration.TotalMilliseconds, 1)
                })
            };

            context.Response.ContentType = "application/json";
            return context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
        }
    }
}
