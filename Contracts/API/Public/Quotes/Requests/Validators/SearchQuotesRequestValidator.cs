using FluentValidation;

namespace Contracts.API.Public.Quotes.Requests.Validators
{
    public class SearchQuotesRequestValidator : AbstractValidator<SearchQuotesRequest>
    {
        public const int MaxPageSize = 100;

        public SearchQuotesRequestValidator()
        {
            RuleFor(x => x.Status)
                .Must(QuoteStatusNames.IsValid)
                .When(x => x.Status is not null)
                .WithMessage($"Status must be one of: {string.Join(", ", QuoteStatusNames.All)}");

            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1).WithMessage("Page number must be 1 or more");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, MaxPageSize).WithMessage($"Page size must be between 1 and {MaxPageSize}");
        }
    }
}
