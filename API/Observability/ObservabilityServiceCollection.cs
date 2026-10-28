using Business.Abstractions.Telemetry;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace API.Observability
{
    public static class ObservabilityServiceCollection
    {
        public const string ServiceName = "advisor-quotes-api";
        public const string OtlpEndpointKey = "OTEL_EXPORTER_OTLP_ENDPOINT";

        /// <summary>
        /// Tracing and metrics (OpenTelemetry). Spans and metrics go to the console by default,
        /// or to an OTLP collector such as Jaeger when OTEL_EXPORTER_OTLP_ENDPOINT is set.
        /// </summary>
        public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder)
        {
            var useOtlp = UseOtlpExporter(builder.Configuration);

            builder.Services
                .AddOpenTelemetry()
                .ConfigureResource(resource => resource.AddService(ServiceName))
                .WithTracing(tracing =>
                {
                    tracing
                        .AddSource(QuoteTelemetry.SourceName)
                        .AddAspNetCoreInstrumentation(options => options.RecordException = true)
                        .AddHttpClientInstrumentation()
                        // Npgsql spans cover every EF Core command, with the SQL text as db.statement
                        .AddNpgsql();

                    if (useOtlp)
                    {
                        tracing.AddOtlpExporter();
                    }
                    else
                    {
                        tracing.AddConsoleExporter();
                    }
                })
                .WithMetrics(metrics =>
                {
                    metrics
                        .AddMeter(QuoteTelemetry.SourceName)
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation();

                    if (useOtlp)
                    {
                        metrics.AddOtlpExporter();
                    }
                    else
                    {
                        metrics.AddConsoleExporter();
                    }
                });

            return builder;
        }

        public static WebApplication UseObservability(this WebApplication app)
        {
            return app;
        }

        /// <summary>The OTLP exporter reads the same variable for its endpoint, so its presence is the switch.</summary>
        public static bool UseOtlpExporter(IConfiguration configuration) =>
            !string.IsNullOrWhiteSpace(configuration[OtlpEndpointKey]);
    }
}
