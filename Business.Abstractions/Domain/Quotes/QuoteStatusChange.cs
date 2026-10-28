namespace Business.Abstractions.Domain.Quotes
{
    /// <summary>
    /// One row of a quote status history. Written in the same transaction as the status change.
    /// </summary>
    public class QuoteStatusChange
    {
        public Guid Id { get; set; }
        public Guid QuoteId { get; set; }

        // Null for the initial row written on create
        public QuoteStatus? FromStatus { get; set; }
        public QuoteStatus ToStatus { get; set; }

        public string? Reason { get; set; }
        public string ChangedBy { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
    }
}
