using FluentValidation;

namespace Contracts.API.Public.Quotes.Requests.Validators
{
    /// <summary>
    /// Shape only. Whether the move is allowed from the current status is a domain rule checked in the service.
    /// </summary>
    public class UpdateQuoteStatusRequestValidator : AbstractValidator<UpdateQuoteStatusRequest>
    {
        public const int MaxReasonLength = 500;

        public UpdateQuoteStatusRequestValidator()
        {
            RuleFor(x => x.Status)
                .NotEmpty().WithMessage("Status is required")
                .Must(QuoteStatusNames.IsValid)
                .WithMessage($"Status must be one of: {string.Join(", ", QuoteStatusNames.All)}");

            RuleFor(x => x.Reason)
                .MaximumLength(MaxReasonLength).WithMessage($"Reason must not exceed {MaxReasonLength} characters");
        }
    }
}
