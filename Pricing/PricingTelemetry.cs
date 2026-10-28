using System.Diagnostics;

namespace Pricing
{
    /// <summary>
    /// Pricing cannot reference Business.Abstractions, so it keeps its own ActivitySource under the shared
    /// "AdvisorQuotes" name. The observability wiring listens to that name, so these spans export with the rest.
    /// </summary>
    internal static class PricingTelemetry
    {
        public const string SourceName = "AdvisorQuotes";
        public const string CalculateSpan = "pricing.calculate";

        public const string ProductCodeTag = "product.code";
        public const string PricingVersionTag = "pricing.version";
        public const string PremiumTag = "quote.premium";

        public static readonly ActivitySource ActivitySource = new(SourceName);
    }
}
