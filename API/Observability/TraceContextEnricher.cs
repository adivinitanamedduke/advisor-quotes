using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace API.Observability
{
    /// <summary>
    /// Stamps every log event with the current TraceId and SpanId, so a trace id from a response header
    /// or a span finds its log lines (and the other way round).
    /// </summary>
    public sealed class TraceContextEnricher : ILogEventEnricher
    {
        public const string TraceIdProperty = "TraceId";
        public const string SpanIdProperty = "SpanId";

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var activity = Activity.Current;
            if (activity is null)
            {
                return;
            }

            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(TraceIdProperty, activity.TraceId.ToHexString()));
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(SpanIdProperty, activity.SpanId.ToHexString()));
        }
    }
}
