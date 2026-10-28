namespace Pricing
{
    /// <summary>
    /// Bound from the "Pricing" configuration section, so rates can change per environment without a release.
    /// </summary>
    public class PricingOptions
    {
        public const string SectionName = "Pricing";

        public string Currency { get; set; } = "ZAR";
        public string Version { get; set; } = "v1";
        public decimal SmokerLoading { get; set; } = 1.5m;
        public List<ProductDefinition> Products { get; set; } = [];
    }

    public class ProductDefinition
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        // Monthly rate per 1 000 of cover before factors
        public decimal BaseRatePer1000 { get; set; }

        public decimal MinCoverAmount { get; set; }
        public decimal MaxCoverAmount { get; set; }
        public int MinTermYears { get; set; }
        public int MaxTermYears { get; set; }
        public int MinEntryAge { get; set; }
        public int MaxEntryAge { get; set; }
    }
}
