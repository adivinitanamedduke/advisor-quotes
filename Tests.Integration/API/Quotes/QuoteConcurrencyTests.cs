using System.Net;
using System.Net.Http.Json;
using Business.Abstractions.Domain.Quotes;
using Business.Abstractions.Exceptions;
using Business.Abstractions.Interfaces.Repository.Quotes;
using Business.Abstractions.Models;
using Contracts.API.Common.Responses;
using Contracts.API.Public.Quotes.Requests;
using Contracts.API.Public.Quotes.Responses;
using Data.SQL.Repositories.Quotes;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tests.Integration.Infrastructure;

namespace Tests.Integration.API.Quotes
{
    /// <summary>Optimistic concurrency on status changes: a stale xmin surfaces as ConflictException / 409.</summary>
    [Collection(PostgresCollection.Name)]
    public class QuoteConcurrencyTests(QuotesApiFixture fixture) : IAsyncLifetime
    {
        private const string BaseUrl = "/api/v1/Quote";
        private const string AdviserId = "adviser-concurrency";

        public Task InitializeAsync() =>
            PostgresAvailability.SkipReason is null ? fixture.ResetDatabaseAsync() : Task.CompletedTask;

        public Task DisposeAsync() => Task.CompletedTask;

        private static async Task<QuoteDto> CreateQuoteAsync(HttpClient client)
        {
            var response = await client.PostAsJsonAsync(BaseUrl, new CreateQuoteRequest
            {
                Client = new QuoteClientRequest { FullName = "Sipho Ndlovu", DateOfBirth = new DateOnly(1990, 1, 15) },
                Product = new QuoteProductRequest { Code = "FUNERAL", CoverAmount = 50_000m, TermYears = 10 }
            });
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<QuoteDto>())!;
        }

        private static HttpClient WithAdviser(HttpClient client)
        {
            client.DefaultRequestHeaders.Add("X-Adviser-Id", AdviserId);
            return client;
        }

        private static void MoveTo(Quote quote, QuoteStatus target)
        {
            quote.StatusHistory.Add(new QuoteStatusChange
            {
                QuoteId = quote.Id,
                FromStatus = quote.Status,
                ToStatus = target,
                ChangedBy = AdviserId,
                ChangedAt = DateTime.UtcNow
            });
            quote.Status = target;
            quote.UpdatedAt = DateTime.UtcNow;
        }

        [PostgresFact]
        public async Task SaveChangesAsync_WhenAnotherContextSavedFirst_ThrowsConflict()
        {
            // Arrange
            var created = await CreateQuoteAsync(WithAdviser(fixture.Factory.CreateClient()));
            await using var firstDb = fixture.CreateDbContext();
            await using var secondDb = fixture.CreateDbContext();
            var first = new QuoteRepository(firstDb);
            var second = new QuoteRepository(secondDb);

            var firstQuote = (await first.GetForUpdateAsync(created.Identifier, AdviserId))!;
            var secondQuote = (await second.GetForUpdateAsync(created.Identifier, AdviserId))!;
            MoveTo(firstQuote, QuoteStatus.SentToClient);
            MoveTo(secondQuote, QuoteStatus.Declined);

            // Act
            await first.SaveChangesAsync();
            var ex = await Assert.ThrowsAsync<ConflictException>(() => second.SaveChangesAsync());

            // Assert
            Assert.Equal(409, ex.StatusCode);
            await using var verifyDb = fixture.CreateDbContext();
            var stored = await verifyDb.Quotes.Include(q => q.StatusHistory).SingleAsync(q => q.Identifier == created.Identifier);
            Assert.Equal(QuoteStatus.SentToClient, stored.Status);
            Assert.Equal(2, stored.StatusHistory.Count);
        }

        [PostgresFact]
        public async Task UpdateStatus_WhenQuoteChangedAfterItWasLoaded_Returns409()
        {
            // Arrange: the repository updates the row from another connection right after loading it,
            // so the request's save always carries a stale xmin
            await using var interfering = fixture.Factory.WithWebHostBuilder(builder =>
                builder.ConfigureTestServices(services =>
                    services.AddScoped<IQuoteRepository>(provider => new ChangedAfterLoadRepository(
                        ActivatorUtilities.CreateInstance<QuoteRepository>(provider), fixture))));
            var client = WithAdviser(interfering.CreateClient());
            var created = await CreateQuoteAsync(client);

            // Act
            var response = await client.PutAsJsonAsync(
                $"{BaseUrl}/{created.Identifier}/status", new UpdateQuoteStatusRequest { Status = "SentToClient" });

            // Assert
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            Assert.Equal(409, error!.StatusCode);
            await using var verifyDb = fixture.CreateDbContext();
            var stored = await verifyDb.Quotes.Include(q => q.StatusHistory).SingleAsync(q => q.Identifier == created.Identifier);
            Assert.Equal(QuoteStatus.Draft, stored.Status);
            Assert.Single(stored.StatusHistory);
        }

        private sealed class ChangedAfterLoadRepository(IQuoteRepository inner, QuotesApiFixture fixture) : IQuoteRepository
        {
            public Task<Quote?> GetByIdentifierAsync(Guid identifier, string adviserId, CancellationToken cancellationToken = default) =>
                inner.GetByIdentifierAsync(identifier, adviserId, cancellationToken);

            public async Task<Quote?> GetForUpdateAsync(Guid identifier, string adviserId, CancellationToken cancellationToken = default)
            {
                var quote = await inner.GetForUpdateAsync(identifier, adviserId, cancellationToken);

                await using var other = fixture.CreateDbContext();
                await other.Quotes
                    .Where(q => q.Identifier == identifier)
                    .ExecuteUpdateAsync(set => set.SetProperty(q => q.UpdatedAt, DateTime.UtcNow), cancellationToken);

                return quote;
            }

            public Task<PagedList<Quote>> SearchAsync(
                string adviserId, QuoteStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken = default) =>
                inner.SearchAsync(adviserId, status, pageNumber, pageSize, cancellationToken);

            public Task<Quote> AddUnlessDuplicateAsync(
                Quote candidate, DateTime fingerprintWindowStart, CancellationToken cancellationToken = default) =>
                inner.AddUnlessDuplicateAsync(candidate, fingerprintWindowStart, cancellationToken);

            public Task SaveChangesAsync(CancellationToken cancellationToken = default) => inner.SaveChangesAsync(cancellationToken);
        }
    }
}
