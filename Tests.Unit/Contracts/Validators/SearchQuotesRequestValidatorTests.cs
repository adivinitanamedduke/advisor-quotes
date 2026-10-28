using Contracts.API.Public.Quotes.Requests;
using Contracts.API.Public.Quotes.Requests.Validators;

namespace Tests.Unit.Contracts.Validators
{
    public class SearchQuotesRequestValidatorTests
    {
        private readonly SearchQuotesRequestValidator _validator = new();

        [Theory]
        [InlineData(null)]
        [InlineData("Draft")]
        [InlineData("Declined")]
        public void Validate_WithNoOrKnownStatus_IsValid(string? status)
        {
            // Act
            var result = _validator.Validate(new SearchQuotesRequest { Status = status });

            // Assert
            Assert.True(result.IsValid);
        }

        [Theory]
        [InlineData("")]
        [InlineData("draft")]
        [InlineData("Pending")]
        public void Validate_WithUnknownStatus_ReturnsError(string status)
        {
            // Act
            var result = _validator.Validate(new SearchQuotesRequest { Status = status });

            // Assert
            Assert.Contains(result.Errors, e => e.PropertyName == "Status");
        }

        [Theory]
        [InlineData(0, 20, "PageNumber")]
        [InlineData(1, 0, "PageSize")]
        [InlineData(1, SearchQuotesRequestValidator.MaxPageSize + 1, "PageSize")]
        public void Validate_WithPagingOutOfRange_ReturnsError(int pageNumber, int pageSize, string property)
        {
            // Act
            var result = _validator.Validate(new SearchQuotesRequest { PageNumber = pageNumber, PageSize = pageSize });

            // Assert
            Assert.Contains(result.Errors, e => e.PropertyName == property);
        }

        [Fact]
        public void Validate_WithMaxPageSize_IsValid()
        {
            // Act
            var result = _validator.Validate(new SearchQuotesRequest { PageSize = SearchQuotesRequestValidator.MaxPageSize });

            // Assert
            Assert.True(result.IsValid);
        }
    }
}
