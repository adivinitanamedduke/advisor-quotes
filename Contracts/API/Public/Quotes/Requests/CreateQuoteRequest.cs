namespace Contracts.API.Public.Quotes.Requests
{
    public class CreateQuoteRequest
    {
        public QuoteClientRequest Client { get; set; } = new();
        public QuoteProductRequest Product { get; set; } = new();
        public string? Notes { get; set; }
    }

    public class QuoteClientRequest
    {
        public string FullName { get; set; } = string.Empty;
        public DateOnly DateOfBirth { get; set; }
        public bool IsSmoker { get; set; }
    }

    public class QuoteProductRequest
    {
        public string Code { get; set; } = string.Empty;
        public decimal CoverAmount { get; set; }
        public int TermYears { get; set; }
    }
}
