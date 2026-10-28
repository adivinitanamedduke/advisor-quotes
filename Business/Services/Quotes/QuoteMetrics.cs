using System.Diagnostics.Metrics;
using Business.Abstractions.Telemetry;

namespace Business.Services.Quotes
{
    /// <summary>
    /// Quote lifecycle counters on QuoteTelemetry.Meter. Tags carry product and status only, never client data.
    /// </summary>
    public static class QuoteMetrics
    {
        public const string QuotesCreated = "quotes_created_total";
        public const string QuotesReplayed = "quotes_replayed_total";
        public const string QuoteConflicts = "quotes_conflicts_total";
        public const string QuoteStatusChanges = "quote_status_changes_total";

        private static readonly Counter<long> Created = QuoteTelemetry.Meter.CreateCounter<long>(
            QuotesCreated, description: "Quotes created");

        private static readonly Counter<long> Replayed = QuoteTelemetry.Meter.CreateCounter<long>(
            QuotesReplayed, description: "Duplicate submissions answered with an existing quote");

        private static readonly Counter<long> Conflicts = QuoteTelemetry.Meter.CreateCounter<long>(
            QuoteConflicts, description: "Idempotency keys reused with a different body");

        private static readonly Counter<long> StatusChanges = QuoteTelemetry.Meter.CreateCounter<long>(
            QuoteStatusChanges, description: "Quote status changes saved");

        public static void RecordCreated(string productCode) => Created.Add(1, ProductTag(productCode));

        public static void RecordReplayed(string productCode) => Replayed.Add(1, ProductTag(productCode));

        public static void RecordConflict(string productCode) => Conflicts.Add(1, ProductTag(productCode));

        public static void RecordStatusChange(string productCode, string toStatus) =>
            StatusChanges.Add(1, ProductTag(productCode), new KeyValuePair<string, object?>(QuoteTelemetry.Tags.ToStatus, toStatus));

        private static KeyValuePair<string, object?> ProductTag(string productCode) =>
            new(QuoteTelemetry.Tags.ProductCode, productCode);
    }
}
