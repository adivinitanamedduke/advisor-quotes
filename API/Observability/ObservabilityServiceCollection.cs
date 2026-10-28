using Business.Abstractions.Telemetry;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace API.Observability
{
    public static class ObservabilityServiceCollection
    {
        public const string ServiceName = "advisor-quotes-api";
        public const string OtlpEndpointKey = "OTEL_EXPORTER_OTLP_ENDPOINT";
        public const string LogFormatKey = "Observability:LogFormat";

        private const string TextLogTemplate =
            "[{Timestamp:HH:mm:ss} {Level:u3}] {TraceId} {SourceContext}: {Message:lj}{NewLine}{Exception}";

        /// <summary>
        /// Tracing and metrics (OpenTelemetry) plus structured logs (Serilog) that share one TraceId.
        /// Spans and metrics go to the console by default, or to an OTLP collector such as Jaeger
        /// when OTEL_EXPORTER_OTLP_ENDPOINT is set.
        /// </summary>
        public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder)
        {
            builder.AddTracingAndMetrics();
            builder.AddStructuredLogging();
            return builder;
        }

        public static WebApplication UseObservability(this WebApplication app)
        {
            app.UseMiddleware<TraceIdHeaderMiddleware>();

            // One line per request: method, path, status, elapsed, plus TraceId from the enricher
            app.UseSerilogRequestLogging(options => options.GetLevel = RequestLogLevel);

            return app;
        }

        /// <summary>The OTLP exporter reads the same variable for its endpoint, so its presence is the switch.</summary>
        public static bool UseOtlpExporter(IConfiguration configuration) =>
            !string.IsNullOrWhiteSpace(configuration[OtlpEndpointKey]);

        /// <summary>Compact JSON unless Observability:LogFormat is "text" (easier to read while developing).</summary>
        public static bool UseTextLogs(IConfiguration configuration) =>
            string.Equals(configuration[LogFormatKey], "text", StringComparison.OrdinalIgnoreCase);

        /// <summary>Errors and 5xx at Error; health probes at Debug so they do not drown real traffic.</summary>
        public static LogEventLevel RequestLogLevel(HttpContext httpContext, double elapsedMs, Exception? exception)
        {
            if (exception is not null || httpContext.Response.StatusCode >= StatusCodes.Status500InternalServerError)
            {
                return LogEventLevel.Error;
            }

            return httpContext.Request.Path.StartsWithSegments("/health")
                ? LogEventLevel.Debug
                : LogEventLevel.Information;
        }

        private static void AddTracingAndMetrics(this WebApplicationBuilder builder)
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
        }

        private static void AddStructuredLogging(this WebApplicationBuilder builder)
        {
            var useText = UseTextLogs(builder.Configuration);

            // Levels and overrides come from the "Serilog" section; the sink is chosen here
            builder.Services.AddSerilog((services, logger) =>
            {
                logger
                    .ReadFrom.Configuration(builder.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .Enrich.With<TraceContextEnricher>();

                if (useText)
                {
                    logger.WriteTo.Console(outputTemplate: TextLogTemplate);
                }
                else
                {
                    logger.WriteTo.Console(new CompactJsonFormatter());
                }
            });
        }
    }
}
