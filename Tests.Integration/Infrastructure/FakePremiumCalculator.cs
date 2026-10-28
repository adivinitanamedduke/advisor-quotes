using Pricing;

namespace Tests.Integration.Infrastructure
{
    /// <summary>
    /// Deterministic stand-in for the pricing engine, so these tests cover the quote lifecycle only.
    /// Product code UNKNOWN breaks a pricing rule.
    /// </summary>
    public sealed class FakePremiumCalculator : IPremiumCalculator
    {
        public const string UnknownProduct = "UNKNOWN";

        public PremiumResult Calculate(PremiumRequest request)
        {
            if (request.ProductCode == UnknownProduct)
            {
                throw new PricingRuleException($"Product {UnknownProduct} is not offered");
            }

            var premium = Math.Round(request.CoverAmount / 1000m * 0.85m, 2);
            return new PremiumResult(premium, "ZAR", "test", new Dictionary<string, decimal> { ["base"] = 0.85m });
        }
    }
}
