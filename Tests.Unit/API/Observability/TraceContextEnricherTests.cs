using System.Diagnostics;
using API.Observability;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;

namespace Tests.Unit.API.Observability
{
    public class TraceContextEnricherTests
    {
        private sealed class PropertyFactory : ILogEventPropertyFactory
        {
            public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false) =>
                new(name, new ScalarValue(value));
        }

        private static LogEvent NewLogEvent() =>
            new(DateTimeOffset.UtcNow, LogEventLevel.Information, null, new MessageTemplateParser().Parse("hello"), []);

        [Fact]
        public void Enrich_WithCurrentActivity_AddsTraceIdAndSpanId()
        {
            // Arrange
            using var activity = new Activity("test").SetIdFormat(ActivityIdFormat.W3C).Start();
            var logEvent = NewLogEvent();

            // Act
            new TraceContextEnricher().Enrich(logEvent, new PropertyFactory());

            // Assert
            Assert.Equal(activity.TraceId.ToHexString(),
                ((ScalarValue)logEvent.Properties[TraceContextEnricher.TraceIdProperty]).Value);
            Assert.Equal(activity.SpanId.ToHexString(),
                ((ScalarValue)logEvent.Properties[TraceContextEnricher.SpanIdProperty]).Value);
        }

        [Fact]
        public void Enrich_WithoutActivity_AddsNothing()
        {
            // Arrange
            Activity.Current = null;
            var logEvent = NewLogEvent();

            // Act
            new TraceContextEnricher().Enrich(logEvent, new PropertyFactory());

            // Assert
            Assert.Empty(logEvent.Properties);
        }
    }
}
