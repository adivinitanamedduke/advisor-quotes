using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace Pricing
{
    /// <summary>
    /// monthlyPremium = round2(cover / 1000 × baseRatePer1000 × ageFactor × smokerFactor × termFactor),
    /// rounded half away from zero. Formula, bands and a worked example: docs/DECISIONS.md D-10.
    /// </summary>
    public class PremiumCalculator(IProductCatalog catalog, IOptions<PricingOptions> options) : IPremiumCalculator
    {
        public static class FactorNames
        {
            public const string CoverUnits = "coverUnits";
            public const string BaseRatePer1000 = "baseRatePer1000";
            public const string AgeAtQuote = "ageAtQuote";
            public const string AgeFactor = "ageFactor";
            public const string SmokerFactor = "smokerFactor";
            public const string TermFactor = "termFactor";
        }

        // Lower bound of each age band (inclusive) and its factor, youngest first.
        // Illustrative only; a real table would come from an actuary (docs/ASSUMPTIONS.md A-11)
        private static readonly (int FromAge, decimal Factor)[] AgeBands =
        [
            (0, 0.80m),
            (30, 1.00m),
            (40, 1.50m),
            (50, 2.50m),
            (60, 4.00m)
        ];

        // Each year above or below the reference term adds or removes 1% (A-12)
        public const int ReferenceTermYears = 10;
        public const decimal TermFactorPerYear = 0.01m;

        private readonly PricingOptions _options = options.Value;

        public PremiumResult Calculate(PremiumRequest request)
        {
            using var activity = PricingTelemetry.ActivitySource.StartActivity(PricingTelemetry.CalculateSpan);
            activity?.SetTag(PricingTelemetry.ProductCodeTag, request.ProductCode);
            activity?.SetTag(PricingTelemetry.PricingVersionTag, _options.Version);

            try
            {
                var product = catalog.Find(request.ProductCode)
                    ?? throw new PricingRuleException($"Unknown product '{request.ProductCode}'");

                var age = AgeAt(request.DateOfBirth, request.QuoteDate);
                EnsureWithinLimits(product, request, age);

                var coverUnits = request.CoverAmount / 1000m;
                var ageFactor = AgeFactor(age);
                var smokerFactor = request.IsSmoker ? _options.SmokerLoading : 1m;
                var termFactor = TermFactor(request.TermYears);

                var monthlyPremium = Math.Round(
                    coverUnits * product.BaseRatePer1000 * ageFactor * smokerFactor * termFactor,
                    2,
                    MidpointRounding.AwayFromZero);

                activity?.SetTag(PricingTelemetry.PremiumTag, monthlyPremium);

                var factors = new Dictionary<string, decimal>
                {
                    [FactorNames.CoverUnits] = coverUnits,
                    [FactorNames.BaseRatePer1000] = product.BaseRatePer1000,
                    [FactorNames.AgeAtQuote] = age,
                    [FactorNames.AgeFactor] = ageFactor,
                    [FactorNames.SmokerFactor] = smokerFactor,
                    [FactorNames.TermFactor] = termFactor
                };

                return new PremiumResult(monthlyPremium, _options.Currency, _options.Version, factors);
            }
            catch (PricingRuleException ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
        }

        /// <summary>Age in completed years on the quote date.</summary>
        public static int AgeAt(DateOnly dateOfBirth, DateOnly quoteDate)
        {
            var age = quoteDate.Year - dateOfBirth.Year;
            if (dateOfBirth > quoteDate.AddYears(-age))
            {
                age--;
            }

            return age;
        }

        public static decimal AgeFactor(int age) => AgeBands.Last(band => age >= band.FromAge).Factor;

        public static decimal TermFactor(int termYears) => 1m + (termYears - ReferenceTermYears) * TermFactorPerYear;

        private static void EnsureWithinLimits(ProductDefinition product, PremiumRequest request, int age)
        {
            // Checked before AgeFactor, so a negative age (birth after the quote date) never reaches the band lookup
            if (age < product.MinEntryAge || age > product.MaxEntryAge)
            {
                throw new PricingRuleException(
                    $"Entry age {age} is outside {product.Code} limits ({product.MinEntryAge} to {product.MaxEntryAge})");
            }

            if (request.TermYears < product.MinTermYears || request.TermYears > product.MaxTermYears)
            {
                throw new PricingRuleException(
                    $"Term of {request.TermYears} years is outside {product.Code} limits ({product.MinTermYears} to {product.MaxTermYears})");
            }

            if (request.CoverAmount < product.MinCoverAmount || request.CoverAmount > product.MaxCoverAmount)
            {
                throw new PricingRuleException(
                    $"Cover amount {request.CoverAmount} is outside {product.Code} limits ({product.MinCoverAmount} to {product.MaxCoverAmount})");
            }
        }
    }
}
