namespace API.Observability
{
    public static class ObservabilityServiceCollection
    {
        /// <summary>
        /// Tracing, structured logging and health checks. The observability session fills this in
        /// (OpenTelemetry exporters, Serilog with TraceId, X-Trace-Id header, /health endpoints).
        /// </summary>
        public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder)
        {
            return builder;
        }

        public static WebApplication UseObservability(this WebApplication app)
        {
            return app;
        }
    }
}
