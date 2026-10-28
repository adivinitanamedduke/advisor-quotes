namespace Pricing
{
    /// <summary>
    /// A request the pricing engine cannot price (unknown product, age or term out of range).
    /// The quote service translates it to a 400.
    /// </summary>
    public class PricingRuleException(string message) : Exception(message);
}
