using System.Diagnostics;

namespace API.Observability
{
    /// <summary>
    /// Returns the request's trace id as X-Trace-Id on every response. The header is added when the response
    /// starts, so it survives error handlers that clear headers and is present on 4xx and 5xx bodies too.
    /// </summary>
    public sealed class TraceIdHeaderMiddleware(RequestDelegate next)
    {
        public const string HeaderName = "X-Trace-Id";

        public Task InvokeAsync(HttpContext context)
        {
            // Captured while the request activity is current; falls back to the ASP.NET request id without tracing
            var traceId = Activity.Current?.TraceId.ToHexString() ?? context.TraceIdentifier;

            context.Response.OnStarting(() =>
            {
                context.Response.Headers[HeaderName] = traceId;
                return Task.CompletedTask;
            });

            return next(context);
        }
    }
}
