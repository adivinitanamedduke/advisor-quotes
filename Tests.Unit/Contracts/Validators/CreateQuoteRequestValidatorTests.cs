using Contracts.API.Public.Quotes.Requests;
using Contracts.API.Public.Quotes.Requests.Validators;

namespace Tests.Unit.Contracts.Validators
{
    public class CreateQuoteRequestValidatorTests
    {
        private readonly CreateQuoteRequestValidator _validator = new();

        private static CreateQuoteRequest ValidRequest() => new()
        {
            Client = new QuoteClientRequest { FullName = "Thandi Mokoena", DateOfBirth = new DateOnly(1990, 4, 12), IsSmoker = false },
            Product = new QuoteProductRequest { Code = "TERM_LIFE", CoverAmount = 1_000_000m, TermYears = 20 },
            Notes = "Prefers email"
        };

        [Fact]
        public void Validate_WithValidRequest_IsValid()
        {
            // Act
            var result = _validator.Validate(ValidRequest());

            // Assert
            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_WithEmptyClientName_ReturnsError()
        {
            // Arrange
            var request = ValidRequest();
            request.Client.FullName = "";

            // Act
            var result = _validator.Validate(request);

            // Assert
            Assert.Contains(result.Errors, e => e.PropertyName == "Client.FullName");
        }

        [Fact]
        public void Validate_WithFutureDateOfBirth_ReturnsError()
        {
            // Arrange
            var request = ValidRequest();
            request.Client.DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

            // Act
            var result = _validator.Validate(request);

            // Assert
            Assert.Contains(result.Errors, e => e.PropertyName == "Client.DateOfBirth");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Validate_WithNonPositiveCover_ReturnsError(decimal coverAmount)
        {
            // Arrange
            var request = ValidRequest();
            request.Product.CoverAmount = coverAmount;

            // Act
            var result = _validator.Validate(request);

            // Assert
            Assert.Contains(result.Errors, e => e.PropertyName == "Product.CoverAmount");
        }

        [Fact]
        public void Validate_WithNotesTooLong_ReturnsError()
        {
            // Arrange
            var request = ValidRequest();
            request.Notes = new string('x', CreateQuoteRequestValidator.MaxNotesLength + 1);

            // Act
            var result = _validator.Validate(request);

            // Assert
            Assert.Contains(result.Errors, e => e.PropertyName == "Notes");
        }
    }
}
