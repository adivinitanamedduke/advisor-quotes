namespace Contracts.API.Public.Products.Responses
{
    public class ProductDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal MinCoverAmount { get; set; }
        public decimal MaxCoverAmount { get; set; }
        public int MinTermYears { get; set; }
        public int MaxTermYears { get; set; }
        public int MinEntryAge { get; set; }
        public int MaxEntryAge { get; set; }
    }
}
