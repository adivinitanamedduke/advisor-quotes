using Microsoft.Extensions.Options;
using Pricing;

namespace Tests.Unit.Pricing
{
    /// <summary>Mirrors the TERM_LIFE product in API/appsettings.json so worked examples match the docs.</summary>
    internal static class PricingTestData
    {
        public static readonly DateOnly QuoteDate = new(2026, 6, 15);

        public static ProductDefinition TermLife() => new()
        {
            Code = "TERM_LIFE",
            Name = "Term Life Cover",
            BaseRatePer1000 = 0.85m,
            MinCoverAmount = 100_000m,
            MaxCoverAmount = 20_000_000m,
            MinTermYears = 5,
            MaxTermYears = 30,
            MinEntryAge = 18,
            MaxEntryAge = 65
        };

        public static IOptions<PricingOptions> Options(params ProductDefinition[] products) =>
            Microsoft.Extensions.Options.Options.Create(new PricingOptions
            {
                Currency = "ZAR",
                Version = "v1",
                SmokerLoading = 1.5m,
                Products = products.Length == 0 ? [TermLife()] : [.. products]
            });

        public static PremiumCalculator Calculator(params ProductDefinition[] products)
        {
            var options = Options(products);
            return new PremiumCalculator(new ProductCatalog(options), options);
        }

        /// <summary>Date of birth that makes the client exactly <paramref name="age"/> on <see cref="QuoteDate"/>.</summary>
        public static DateOnly BornAged(int age) => QuoteDate.AddYears(-age);

        public static PremiumRequest Request(
            string productCode = "TERM_LIFE",
            decimal coverAmount = 1_000_000m,
            int termYears = 20,
            int age = 35,
            bool isSmoker = false) =>
            new(productCode, coverAmount, termYears, BornAged(age), isSmoker, QuoteDate);
    }
}
