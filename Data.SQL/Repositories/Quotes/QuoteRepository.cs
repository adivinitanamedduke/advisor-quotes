using Business.Abstractions.Domain.Quotes;
using Business.Abstractions.Interfaces.Repository.Quotes;
using Business.Abstractions.Models;
using Data.SQL.EF;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Data.SQL.Repositories.Quotes
{
    public class QuoteRepository(QuotesDbContext db) : IQuoteRepository
    {
        // Unique (adviser_id, idempotency_key) index from the InitialQuotes migration
        private const string IdempotencyKeyIndex = "ix_quotes_adviser_id_idempotency_key";

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

        public async Task<Quote> AddUnlessDuplicateAsync(
            Quote candidate,
            DateTime fingerprintWindowStart,
            CancellationToken cancellationToken = default)
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            // Raw SQL because LINQ cannot take an advisory lock. Two identical submissions serialise here, so the
            // second one sees the first one's row; the lock is released at commit or rollback.
            await db.Database.ExecuteSqlAsync(
                $"SELECT pg_advisory_xact_lock(hashtext({candidate.RequestFingerprint}))",
                cancellationToken);

            var existing = await FindDuplicateAsync(candidate, fingerprintWindowStart, cancellationToken);
            if (existing is not null)
            {
                await transaction.CommitAsync(cancellationToken);
                return existing;
            }

            db.Quotes.Add(candidate);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return candidate;
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException
                   {
                       SqlState: PostgresErrorCodes.UniqueViolation,
                       ConstraintName: IdempotencyKeyIndex
                   })
            {
                // Same Idempotency-Key with a different body slipped past the fingerprint lock; the unique index
                // settled it, so return the quote that won
                await transaction.RollbackAsync(cancellationToken);
                db.ChangeTracker.Clear();

                return await FindByIdempotencyKeyAsync(candidate.AdviserId, candidate.IdempotencyKey, cancellationToken)
                    ?? throw new InvalidOperationException("Unique violation on create but no existing quote was found", ex);
            }
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        private async Task<Quote?> FindDuplicateAsync(Quote candidate, DateTime fingerprintWindowStart, CancellationToken cancellationToken)
        {
            var byKey = await FindByIdempotencyKeyAsync(candidate.AdviserId, candidate.IdempotencyKey, cancellationToken);
            if (byKey is not null)
            {
                return byKey;
            }

            return await db.Quotes
                .AsNoTracking()
                .Include(q => q.StatusHistory)
                .Where(q => q.AdviserId == candidate.AdviserId
                            && q.RequestFingerprint == candidate.RequestFingerprint
                            && q.CreatedAt >= fingerprintWindowStart)
                .OrderByDescending(q => q.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private async Task<Quote?> FindByIdempotencyKeyAsync(string adviserId, string? idempotencyKey, CancellationToken cancellationToken)
        {
            if (idempotencyKey is null)
            {
                return null;
            }

            return await db.Quotes
                .AsNoTracking()
                .Include(q => q.StatusHistory)
                .FirstOrDefaultAsync(q => q.AdviserId == adviserId && q.IdempotencyKey == idempotencyKey, cancellationToken);
        }
    }
}
