namespace Contracts.API.Public.Quotes.Responses
{
    public class QuoteStatusChangeDto
    {
        public string? FromStatus { get; set; }
        public string ToStatus { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public string ChangedBy { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
    }
}
