namespace Business.Abstractions.Domain.Quotes
{
    /// <summary>
    /// Lifecycle of a quote. Allowed moves live in the quote status machine (Business), not here.
    /// </summary>
    public enum QuoteStatus
    {
        Draft,
        SentToClient,
        Accepted,
        Declined
    }
}
