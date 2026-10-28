namespace Business.Abstractions.Domain.Quotes
{
    /// <summary>
    /// Result of a create call. IsReplay is true when a duplicate submission returned an existing quote.
    /// </summary>
    public record CreateQuoteOutcome(Quote Quote, bool IsReplay);
}
