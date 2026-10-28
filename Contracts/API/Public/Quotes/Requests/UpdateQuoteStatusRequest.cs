namespace Contracts.API.Public.Quotes.Requests
{
    public class UpdateQuoteStatusRequest
    {
        /// <summary>Target status, one of QuoteStatusNames.</summary>
        public string Status { get; set; } = string.Empty;

        public string? Reason { get; set; }
    }
}
