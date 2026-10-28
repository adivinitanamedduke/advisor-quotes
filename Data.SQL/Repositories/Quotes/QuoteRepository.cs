using Business.Abstractions.Domain.Quotes;
using Business.Abstractions.Interfaces.Repository.Quotes;
using Business.Abstractions.Models;
using Data.SQL.EF;
using Microsoft.EntityFrameworkCore;

namespace Data.SQL.Repositories.Quotes
{
    public class QuoteRepository(QuotesDbContext db) : IQuoteRepository
    {
        public Task<Quote?> GetByIdentifierAsync(Guid identifier, string adviserId, CancellationToken cancellationToken = default) =>
            db.Quotes
                .AsNoTracking()
                .Include(q => q.StatusHistory)
                .FirstOrDefaultAsync(q => q.Identifier == identifier && q.AdviserId == adviserId, cancellationToken);

        public Task<Quote?> GetForUpdateAsync(Guid identifier, string adviserId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<PagedList<Quote>> SearchAsync(
            string adviserId,
            QuoteStatus? status,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Quote> AddUnlessDuplicateAsync(
            Quote candidate,
            DateTime fingerprintWindowStart,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
