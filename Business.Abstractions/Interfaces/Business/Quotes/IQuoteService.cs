using Business.Abstractions.Domain.Quotes;
using Business.Abstractions.Models;
using Contracts.API.Public.Quotes.Requests;

namespace Business.Abstractions.Interfaces.Business.Quotes
{
    public interface IQuoteService
    {
        /// <summary>
        /// Validates, prices and stores a quote. A duplicate submission (same Idempotency-Key, or same
        /// fingerprint inside the configured window) returns the existing quote with IsReplay = true.
        /// </summary>
        Task<CreateQuoteOutcome> CreateQuoteAsync(
            CreateQuoteRequest request,
            string adviserId,
            string? idempotencyKey,
            CancellationToken cancellationToken = default);

        Task<PagedList<Quote>> SearchQuotesAsync(
            SearchQuotesRequest request,
            string adviserId,
            CancellationToken cancellationToken = default);

        /// <summary>Returns null when the quote does not exist for this adviser.</summary>
        Task<Quote?> GetQuoteAsync(Guid identifier, string adviserId, CancellationToken cancellationToken = default);

        /// <summary>Applies a status transition and records it in the history. Throws NotFound, BadRequest or Conflict.</summary>
        Task<Quote> UpdateQuoteStatusAsync(
            Guid identifier,
            UpdateQuoteStatusRequest request,
            string adviserId,
            CancellationToken cancellationToken = default);
    }
}
