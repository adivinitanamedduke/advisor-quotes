using FluentValidation;

namespace Contracts.API.Public.Quotes.Requests.Validators
{
    /// <summary>
    /// Request shape rules only. Product specific limits (age, term, cover per product) are pricing rules
    /// and are enforced by the premium calculator.
    /// </summary>
    public class CreateQuoteRequestValidator : AbstractValidator<CreateQuoteRequest>
    {
        public const int MaxNameLength = 200;
        public const int MaxNotesLength = 2000;

        public CreateQuoteRequestValidator()
        {
            RuleFor(x => x.Client).NotNull().WithMessage("Client is required");
            RuleFor(x => x.Product).NotNull().WithMessage("Product is required");

            When(x => x.Client is not null, () =>
            {
                RuleFor(x => x.Client.FullName)
                    .NotEmpty().WithMessage("Client full name is required")
                    .MaximumLength(MaxNameLength).WithMessage($"Client full name must not exceed {MaxNameLength} characters");

                RuleFor(x => x.Client.DateOfBirth)
                    .NotEqual(default(DateOnly)).WithMessage("Client date of birth is required")
                    .LessThan(_ => DateOnly.FromDateTime(DateTime.UtcNow)).WithMessage("Client date of birth must be in the past");
            });

            When(x => x.Product is not null, () =>
            {
                RuleFor(x => x.Product.Code)
                    .NotEmpty().WithMessage("Product code is required");

                RuleFor(x => x.Product.CoverAmount)
                    .GreaterThan(0).WithMessage("Cover amount must be greater than zero");

                RuleFor(x => x.Product.TermYears)
                    .GreaterThan(0).WithMessage("Term must be at least one year");
            });

            RuleFor(x => x.Notes)
                .MaximumLength(MaxNotesLength).WithMessage($"Notes must not exceed {MaxNotesLength} characters");
        }
    }
}
