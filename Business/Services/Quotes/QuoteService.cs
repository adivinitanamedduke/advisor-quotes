using Business.Abstractions.Domain.Quotes;
using Business.Abstractions.Exceptions;
using Business.Abstractions.Interfaces.Business.Quotes;
using Business.Abstractions.Interfaces.Repository.Quotes;
using Business.Abstractions.Models;
using Contracts.API.Public.Quotes.Requests;
using Microsoft.Extensions.Logging;

namespace Business.Services.Quotes
{
    public class QuoteService(
        IQuoteRepository quoteRepository,
        ILogger<QuoteService> logger) : IQuoteService
    {
        public Task<CreateQuoteOutcome> CreateQuoteAsync(
            CreateQuoteRequest request,
            string adviserId,
            string? idempotencyKey,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<PagedList<Quote>> SearchQuotesAsync(
            SearchQuotesRequest request,
            string adviserId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public async Task<Quote?> GetQuoteAsync(Guid identifier, string adviserId, CancellationToken cancellationToken = default)
        {
            EnsureAdviser(adviserId);

            var quote = await quoteRepository.GetByIdentifierAsync(identifier, adviserId, cancellationToken);
            if (quote is null)
            {
                logger.LogInformation("Quote {QuoteIdentifier} not found for adviser {AdviserId}", identifier, adviserId);
            }

            return quote;
        }

        public Task<Quote> UpdateQuoteStatusAsync(
            Guid identifier,
            UpdateQuoteStatusRequest request,
            string adviserId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        // The controller already rejects a missing X-Adviser-Id; this guards callers that are not HTTP
        private static void EnsureAdviser(string adviserId)
        {
            if (string.IsNullOrWhiteSpace(adviserId))
            {
                throw new BadRequestException("Adviser id is required", [new ApiError("AdviserId", "Adviser id is required")]);
            }
        }
    }
}
