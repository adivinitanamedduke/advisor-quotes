namespace Contracts.API.Public.Quotes.Responses
{
    /// <summary>List row: enough to scan and pick a quote, without history or rating factors.</summary>
    public class QuoteSummaryDto
    {
        public Guid Identifier { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string ClientFullName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public decimal CoverAmount { get; set; }
        public decimal MonthlyPremium { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
