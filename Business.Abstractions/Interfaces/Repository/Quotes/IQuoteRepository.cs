using Business.Abstractions.Domain.Quotes;
using Business.Abstractions.Models;

namespace Business.Abstractions.Interfaces.Repository.Quotes
{
    public interface IQuoteRepository
    {
        /// <summary>Read only, includes status history. Adviser scoped.</summary>
        Task<Quote?> GetByIdentifierAsync(Guid identifier, string adviserId, CancellationToken cancellationToken = default);

        /// <summary>Tracked, includes status history, for a status update followed by SaveChangesAsync.</summary>
        Task<Quote?> GetForUpdateAsync(Guid identifier, string adviserId, CancellationToken cancellationToken = default);

        Task<PagedList<Quote>> SearchAsync(
            string adviserId,
            QuoteStatus? status,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// In one transaction: serialise on the candidate fingerprint, look for an existing quote with the same
        /// (adviser, idempotency key) or the same (adviser, fingerprint) created after fingerprintWindowStart,
        /// and insert the candidate plus its initial status history row only when none exists.
        /// Returns the existing quote when one is found, otherwise the inserted candidate.
        /// </summary>
        Task<Quote> AddUnlessDuplicateAsync(
            Quote candidate,
            DateTime fingerprintWindowStart,
            CancellationToken cancellationToken = default);

        /// <summary>Saves tracked changes. A concurrency clash surfaces as ConflictException.</summary>
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
