using Business.Abstractions.Domain.Quotes;
using Business.Abstractions.Models;
using Contracts.API.Common.Responses;
using Contracts.API.Public.Quotes.Responses;

namespace Business.Mappers
{
    /// <summary>
    /// Domain to response DTO mapping. Id, IdempotencyKey and RequestFingerprint are never mapped out.
    /// </summary>
    public static class QuoteMapper
    {
        public static QuoteDto ToDto(this Quote quote) => new()
        {
            Identifier = quote.Identifier,
            Reference = quote.Reference,
            AdviserId = quote.AdviserId,
            Client = new QuoteClientDto
            {
                FullName = quote.ClientFullName,
                DateOfBirth = quote.ClientDateOfBirth,
                IsSmoker = quote.ClientIsSmoker
            },
            Product = new QuoteProductDto
            {
                Code = quote.ProductCode,
                CoverAmount = quote.CoverAmount,
                TermYears = quote.TermYears
            },
            MonthlyPremium = quote.MonthlyPremium,
            Currency = quote.Currency,
            PricingVersion = quote.PricingVersion,
            RatingFactors = new Dictionary<string, decimal>(quote.RatingFactors),
            Status = ToStatusName(quote.Status),
            Notes = quote.Notes,
            CreatedAt = quote.CreatedAt,
            UpdatedAt = quote.UpdatedAt,
            StatusHistory = quote.StatusHistory
                .OrderBy(change => change.ChangedAt)
                .Select(ToDto)
                .ToList()
        };

        public static QuoteStatusChangeDto ToDto(this QuoteStatusChange change) => new()
        {
            FromStatus = change.FromStatus is { } from ? ToStatusName(from) : null,
            ToStatus = ToStatusName(change.ToStatus),
            Reason = change.Reason,
            ChangedBy = change.ChangedBy,
            ChangedAt = change.ChangedAt
        };

        public static QuoteSummaryDto ToSummaryDto(this Quote quote) => new()
        {
            Identifier = quote.Identifier,
            Reference = quote.Reference,
            ClientFullName = quote.ClientFullName,
            ProductCode = quote.ProductCode,
            CoverAmount = quote.CoverAmount,
            MonthlyPremium = quote.MonthlyPremium,
            Currency = quote.Currency,
            Status = ToStatusName(quote.Status),
            CreatedAt = quote.CreatedAt,
            UpdatedAt = quote.UpdatedAt
        };

        public static PagedResult<QuoteSummaryDto> ToPagedResult(this PagedList<Quote> page) => new()
        {
            Items = page.Items.Select(ToSummaryDto).ToList(),
            TotalCount = page.TotalCount,
            PageNumber = page.PageNumber,
            PageSize = page.PageSize
        };

        // QuoteStatusNames values match the enum member names, so the mapping is the name itself
        public static string ToStatusName(QuoteStatus status) => status.ToString();

        /// <summary>Parses a status that has already passed validation against QuoteStatusNames.</summary>
        public static QuoteStatus ToQuoteStatus(string statusName) => Enum.Parse<QuoteStatus>(statusName, ignoreCase: false);
    }
}
