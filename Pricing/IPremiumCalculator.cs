namespace Pricing
{
    /// <summary>
    /// Calculates a premium. Knows nothing about quotes, so it can move behind its own service later.
    /// Throws PricingRuleException when the request breaks a product rule.
    /// </summary>
    public interface IPremiumCalculator
    {
        PremiumResult Calculate(PremiumRequest request);
    }

    public record PremiumRequest(
        string ProductCode,
        decimal CoverAmount,
        int TermYears,
        DateOnly DateOfBirth,
        bool IsSmoker,
        DateOnly QuoteDate);

    /// <param name="Factors">Every factor used, keyed by name, so the result can be stored and explained.</param>
    public record PremiumResult(
        decimal MonthlyPremium,
        string Currency,
        string PricingVersion,
        IReadOnlyDictionary<string, decimal> Factors);
}
