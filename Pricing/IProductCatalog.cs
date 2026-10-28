namespace Pricing
{
    public interface IProductCatalog
    {
        IReadOnlyList<ProductDefinition> GetAll();

        /// <summary>Case insensitive. Null when the code is unknown.</summary>
        ProductDefinition? Find(string productCode);
    }
}
