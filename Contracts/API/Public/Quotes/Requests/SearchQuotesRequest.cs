namespace Contracts.API.Public.Quotes.Requests
{
    public class SearchQuotesRequest
    {
        /// <summary>Optional filter, one of QuoteStatusNames.</summary>
        public string? Status { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
