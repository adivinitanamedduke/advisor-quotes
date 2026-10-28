using Business.Abstractions.Domain.Quotes;
using Contracts.API.Public.Quotes.Requests;

namespace Tests.Unit.Business.Services.Quotes
{
    /// <summary>Shared builders for quote unit tests.</summary>
    public static class QuoteTestData
    {
        public const string AdviserId = "adviser-1";

        public static readonly DateTime Now = new(2026, 10, 28, 9, 30, 0, DateTimeKind.Utc);

        public static CreateQuoteRequest CreateRequest(string? notes = "First quote") => new()
        {
            Client = new QuoteClientRequest
            {
                FullName = "  Thandi Mokoena ",
                DateOfBirth = new DateOnly(1985, 4, 12),
                IsSmoker = false
            },
            Product = new QuoteProductRequest { Code = "term_life", CoverAmount = 1_000_000m, TermYears = 20 },
            Notes = notes
        };

        public static Quote Quote(QuoteStatus status = QuoteStatus.Draft, string fingerprint = "fp", string? idempotencyKey = null)
        {
            var id = Guid.NewGuid();
            return new Quote
            {
                Id = id,
                Identifier = Guid.NewGuid(),
                Reference = "QT-000001",
                AdviserId = AdviserId,
                ClientFullName = "Thandi Mokoena",
                ClientDateOfBirth = new DateOnly(1985, 4, 12),
                ClientIsSmoker = false,
                ProductCode = "TERM_LIFE",
                CoverAmount = 1_000_000m,
                TermYears = 20,
                MonthlyPremium = 850m,
                Currency = "ZAR",
                PricingVersion = "v1",
                RatingFactors = new Dictionary<string, decimal> { ["base"] = 0.85m },
                Status = status,
                Notes = "First quote",
                IdempotencyKey = idempotencyKey,
                RequestFingerprint = fingerprint,
                CreatedAt = Now.AddMinutes(-5),
                UpdatedAt = Now.AddMinutes(-5),
                StatusHistory =
                [
                    new QuoteStatusChange
                    {
                        Id = Guid.NewGuid(),
                        QuoteId = id,
                        FromStatus = null,
                        ToStatus = QuoteStatus.Draft,
                        ChangedBy = AdviserId,
                        ChangedAt = Now.AddMinutes(-5)
                    }
                ]
            };
        }
    }
}
