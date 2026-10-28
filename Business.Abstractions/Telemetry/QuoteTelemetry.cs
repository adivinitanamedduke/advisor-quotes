using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Business.Abstractions.Telemetry
{
    /// <summary>
    /// Shared tracing and metrics sources. Business code starts spans here; API/Observability wires the exporters.
    /// </summary>
    public static class QuoteTelemetry
    {
        public const string SourceName = "AdvisorQuotes";

        public static readonly ActivitySource ActivitySource = new(SourceName);
        public static readonly Meter Meter = new(SourceName);

        public static class Spans
        {
            public const string QuoteCreate = "quote.create";
            public const string DuplicateCheck = "quote.duplicate_check";
            public const string PricingCalculate = "pricing.calculate";
            public const string QuoteStatusChange = "quote.status_change";
        }

        public static class Tags
        {
            public const string QuoteIdentifier = "quote.identifier";
            public const string QuoteReference = "quote.reference";
            public const string AdviserId = "adviser.id";
            public const string ProductCode = "product.code";
            public const string Premium = "quote.premium";
            public const string FromStatus = "quote.status.from";
            public const string ToStatus = "quote.status.to";
            public const string IdempotencyOutcome = "idempotency.outcome";
        }
    }
}
