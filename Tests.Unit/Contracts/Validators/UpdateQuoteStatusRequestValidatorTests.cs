using Contracts.API.Public.Quotes.Requests;
using Contracts.API.Public.Quotes.Requests.Validators;

namespace Tests.Unit.Contracts.Validators
{
    public class UpdateQuoteStatusRequestValidatorTests
    {
        private readonly UpdateQuoteStatusRequestValidator _validator = new();

        [Theory]
        [InlineData("Draft")]
        [InlineData("SentToClient")]
        [InlineData("Accepted")]
        [InlineData("Declined")]
        public void Validate_WithKnownStatus_IsValid(string status)
        {
            // Act
            var result = _validator.Validate(new UpdateQuoteStatusRequest { Status = status });

            // Assert
            Assert.True(result.IsValid);
        }

        [Theory]
        [InlineData("")]
        [InlineData("accepted")]
        [InlineData("Cancelled")]
        public void Validate_WithUnknownStatus_ReturnsError(string status)
        {
            // Act
            var result = _validator.Validate(new UpdateQuoteStatusRequest { Status = status });

            // Assert
            Assert.Contains(result.Errors, e => e.PropertyName == "Status");
        }
    }
}
