namespace Contracts.API.Public.Quotes.Responses
{
    public class QuoteDto
    {
        public Guid Identifier { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string AdviserId { get; set; } = string.Empty;
        public QuoteClientDto Client { get; set; } = new();
        public QuoteProductDto Product { get; set; } = new();
        public decimal MonthlyPremium { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string PricingVersion { get; set; } = string.Empty;
        public IReadOnlyDictionary<string, decimal> RatingFactors { get; set; } = new Dictionary<string, decimal>();
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public IReadOnlyList<QuoteStatusChangeDto> StatusHistory { get; set; } = [];
    }

    public class QuoteClientDto
    {
        public string FullName { get; set; } = string.Empty;
        public DateOnly DateOfBirth { get; set; }
        public bool IsSmoker { get; set; }
    }

    public class QuoteProductDto
    {
        public string Code { get; set; } = string.Empty;
        public decimal CoverAmount { get; set; }
        public int TermYears { get; set; }
    }
}
