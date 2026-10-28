using Microsoft.Extensions.Options;

namespace Pricing
{
    /// <summary>
    /// Products from the "Pricing" configuration section, read once at startup.
    /// </summary>
    public class ProductCatalog : IProductCatalog
    {
        private readonly IReadOnlyList<ProductDefinition> _products;
        private readonly Dictionary<string, ProductDefinition> _byCode;

        public ProductCatalog(IOptions<PricingOptions> options)
        {
            _products = options.Value.Products.AsReadOnly();
            _byCode = options.Value.Products.ToDictionary(p => p.Code, StringComparer.OrdinalIgnoreCase);
        }

        public IReadOnlyList<ProductDefinition> GetAll() => _products;

        public ProductDefinition? Find(string productCode)
        {
            if (string.IsNullOrWhiteSpace(productCode))
            {
                return null;
            }

            return _byCode.GetValueOrDefault(productCode.Trim());
        }
    }
}
