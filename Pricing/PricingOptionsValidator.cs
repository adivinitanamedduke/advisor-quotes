using Microsoft.Extensions.Options;

namespace Pricing
{
    /// <summary>
    /// Fails startup on a pricing configuration that could never price correctly,
    /// instead of surfacing it as a 500 on the first quote.
    /// </summary>
    public class PricingOptionsValidator : IValidateOptions<PricingOptions>
    {
        public ValidateOptionsResult Validate(string? name, PricingOptions options)
        {
            var failures = new List<string>();

            if (options.SmokerLoading <= 0)
            {
                failures.Add($"{PricingOptions.SectionName}:SmokerLoading must be greater than 0");
            }

            if (options.Products.Count == 0)
            {
                failures.Add($"{PricingOptions.SectionName}:Products must contain at least one product");
            }

            for (var i = 0; i < options.Products.Count; i++)
            {
                failures.AddRange(ValidateProduct(options.Products[i], $"{PricingOptions.SectionName}:Products:{i}"));
            }

            var duplicateCodes = options.Products
                .Where(p => !string.IsNullOrWhiteSpace(p.Code))
                .GroupBy(p => p.Code.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (var code in duplicateCodes)
            {
                failures.Add($"{PricingOptions.SectionName}:Products has duplicate code '{code}' (codes are case-insensitive)");
            }

            return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
        }

        private static IEnumerable<string> ValidateProduct(ProductDefinition product, string path)
        {
            if (string.IsNullOrWhiteSpace(product.Code))
            {
                yield return $"{path}:Code is required";
            }

            if (string.IsNullOrWhiteSpace(product.Name))
            {
                yield return $"{path}:Name is required";
            }

            if (product.BaseRatePer1000 <= 0)
            {
                yield return $"{path}:BaseRatePer1000 must be greater than 0";
            }

            if (product.MinCoverAmount > product.MaxCoverAmount)
            {
                yield return $"{path}:MinCoverAmount must not exceed MaxCoverAmount";
            }

            if (product.MinTermYears > product.MaxTermYears)
            {
                yield return $"{path}:MinTermYears must not exceed MaxTermYears";
            }

            if (product.MinEntryAge > product.MaxEntryAge)
            {
                yield return $"{path}:MinEntryAge must not exceed MaxEntryAge";
            }
        }
    }
}
