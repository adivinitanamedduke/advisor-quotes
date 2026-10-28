using System.Diagnostics;
using Business.Abstractions.Config;
using Business.Abstractions.Domain.Quotes;
using Business.Abstractions.Exceptions;
using Business.Abstractions.Interfaces.Business.Quotes;
using Business.Abstractions.Interfaces.Repository.Quotes;
using Business.Abstractions.Models;
using Business.Abstractions.Telemetry;
using Business.Mappers;
using Business.Validators;
using Contracts.API.Public.Quotes.Requests;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pricing;

namespace Business.Services.Quotes
{
    public class QuoteService(
        IQuoteRepository quoteRepository,
        IPremiumCalculator premiumCalculator,
        IValidator<CreateQuoteRequest> createValidator,
        IValidator<SearchQuotesRequest> searchValidator,
        IOptions<IdempotencyOptions> idempotencyOptions,
        TimeProvider timeProvider,
        ILogger<QuoteService> logger) : IQuoteService
    {
        public const string OutcomeCreated = "created";
        public const string OutcomeReplayed = "replayed";
        public const string OutcomeConflict = "conflict";

        public async Task<CreateQuoteOutcome> CreateQuoteAsync(
            CreateQuoteRequest request,
            string adviserId,
            string? idempotencyKey,
            CancellationToken cancellationToken = default)
        {
            await createValidator.ValidateOrThrowAsync(request, cancellationToken);
            EnsureAdviser(adviserId);

            var productCode = request.Product.Code.Trim().ToUpperInvariant();

            using var activity = QuoteTelemetry.ActivitySource.StartActivity(QuoteTelemetry.Spans.QuoteCreate);
            activity?.SetTag(QuoteTelemetry.Tags.AdviserId, adviserId);
            activity?.SetTag(QuoteTelemetry.Tags.ProductCode, productCode);

            var now = timeProvider.GetUtcNow().UtcDateTime;
            var premium = Price(request, productCode, DateOnly.FromDateTime(now));
            var fingerprint = QuoteFingerprint.Compute(adviserId, request);
            var candidate = BuildCandidate(request, adviserId, idempotencyKey, productCode, premium, fingerprint, now);

            Quote stored;
            using (QuoteTelemetry.ActivitySource.StartActivity(QuoteTelemetry.Spans.DuplicateCheck))
            {
                var windowStart = now.AddMinutes(-idempotencyOptions.Value.WindowMinutes);
                stored = await quoteRepository.AddUnlessDuplicateAsync(candidate, windowStart, cancellationToken);
            }

            activity?.SetTag(QuoteTelemetry.Tags.QuoteIdentifier, stored.Identifier);
            activity?.SetTag(QuoteTelemetry.Tags.QuoteReference, stored.Reference);
            activity?.SetTag(QuoteTelemetry.Tags.Premium, stored.MonthlyPremium);

            if (stored.Identifier == candidate.Identifier)
            {
                activity?.SetTag(QuoteTelemetry.Tags.IdempotencyOutcome, OutcomeCreated);
                logger.LogInformation(
                    "Created quote {QuoteIdentifier} ({QuoteReference}) for adviser {AdviserId}, product {ProductCode}, premium {MonthlyPremium} {Currency}",
                    stored.Identifier, stored.Reference, adviserId, productCode, stored.MonthlyPremium, stored.Currency);
                return new CreateQuoteOutcome(stored, IsReplay: false);
            }

            // Same key, different body: the client reused a key for a new request
            if (idempotencyKey is not null
                && stored.IdempotencyKey == idempotencyKey
                && stored.RequestFingerprint.Trim() != fingerprint)
            {
                activity?.SetTag(QuoteTelemetry.Tags.IdempotencyOutcome, OutcomeConflict);
                activity?.SetStatus(ActivityStatusCode.Error, OutcomeConflict);
                logger.LogWarning(
                    "Idempotency key reused with a different body by adviser {AdviserId}; existing quote {QuoteIdentifier}",
                    adviserId, stored.Identifier);
                throw new ConflictException(
                    "This Idempotency-Key was already used for a different quote request; use a new key for a new quote");
            }

            activity?.SetTag(QuoteTelemetry.Tags.IdempotencyOutcome, OutcomeReplayed);
            logger.LogInformation(
                "Duplicate submission by adviser {AdviserId} replayed existing quote {QuoteIdentifier} ({QuoteReference})",
                adviserId, stored.Identifier, stored.Reference);
            return new CreateQuoteOutcome(stored, IsReplay: true);
        }

        public async Task<PagedList<Quote>> SearchQuotesAsync(
            SearchQuotesRequest request,
            string adviserId,
            CancellationToken cancellationToken = default)
        {
            await searchValidator.ValidateOrThrowAsync(request, cancellationToken);
            EnsureAdviser(adviserId);

            QuoteStatus? status = request.Status is null ? null : QuoteMapper.ToQuoteStatus(request.Status);

            var page = await quoteRepository.SearchAsync(adviserId, status, request.PageNumber, request.PageSize, cancellationToken);

            logger.LogInformation(
                "Search by adviser {AdviserId} for status {Status} returned {Count} of {TotalCount} quotes",
                adviserId, request.Status ?? "any", page.Items.Count, page.TotalCount);
            return page;
        }

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

        private PremiumResult Price(CreateQuoteRequest request, string productCode, DateOnly quoteDate)
        {
            // The calculator starts its own pricing span, so no span here
            try
            {
                return premiumCalculator.Calculate(new PremiumRequest(
                    productCode,
                    request.Product.CoverAmount,
                    request.Product.TermYears,
                    request.Client.DateOfBirth,
                    request.Client.IsSmoker,
                    quoteDate));
            }
            catch (PricingRuleException ex)
            {
                logger.LogInformation("Pricing rejected product {ProductCode}: {PricingRule}", productCode, ex.Message);
                throw new BadRequestException(ex.Message, [new ApiError("Product", ex.Message)]);
            }
        }

        private static Quote BuildCandidate(
            CreateQuoteRequest request,
            string adviserId,
            string? idempotencyKey,
            string productCode,
            PremiumResult premium,
            string fingerprint,
            DateTime now)
        {
            var id = Guid.CreateVersion7();
            return new Quote
            {
                Id = id,
                Identifier = Guid.NewGuid(),
                AdviserId = adviserId,
                ClientFullName = request.Client.FullName.Trim(),
                ClientDateOfBirth = request.Client.DateOfBirth,
                ClientIsSmoker = request.Client.IsSmoker,
                ProductCode = productCode,
                CoverAmount = request.Product.CoverAmount,
                TermYears = request.Product.TermYears,
                MonthlyPremium = premium.MonthlyPremium,
                Currency = premium.Currency,
                PricingVersion = premium.PricingVersion,
                RatingFactors = new Dictionary<string, decimal>(premium.Factors),
                Status = QuoteStatus.Draft,
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                IdempotencyKey = idempotencyKey,
                RequestFingerprint = fingerprint,
                CreatedAt = now,
                UpdatedAt = now,
                StatusHistory =
                [
                    new QuoteStatusChange
                    {
                        Id = Guid.CreateVersion7(),
                        QuoteId = id,
                        FromStatus = null,
                        ToStatus = QuoteStatus.Draft,
                        ChangedBy = adviserId,
                        ChangedAt = now
                    }
                ]
            };
        }

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
