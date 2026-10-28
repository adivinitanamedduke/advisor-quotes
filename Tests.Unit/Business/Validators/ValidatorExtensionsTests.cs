using Business.Abstractions.Exceptions;
using Business.Validators;
using Contracts.API.Public.Quotes.Requests;
using Contracts.API.Public.Quotes.Requests.Validators;

namespace Tests.Unit.Business.Validators
{
    public class ValidatorExtensionsTests
    {
        private readonly SearchQuotesRequestValidator _validator = new();

        [Fact]
        public async Task ValidateOrThrowAsync_WithNullRequest_ThrowsBadRequest()
        {
            // Act
            var ex = await Assert.ThrowsAsync<BadRequestException>(() => _validator.ValidateOrThrowAsync(null));

            // Assert
            Assert.Equal(400, ex.StatusCode);
            Assert.Equal("Request body is required", ex.Message);
        }

        [Fact]
        public async Task ValidateOrThrowAsync_WithSeveralFailures_JoinsEveryMessage()
        {
            // Arrange
            var request = new SearchQuotesRequest { PageNumber = 0, PageSize = 0 };

            // Act
            var ex = await Assert.ThrowsAsync<BadRequestException>(() => _validator.ValidateOrThrowAsync(request));

            // Assert
            Assert.Equal(2, ex.Errors.Count);
            Assert.Equal(string.Join("; ", ex.Errors.Select(e => e.Message)), ex.Message);
        }

        [Fact]
        public async Task ValidateOrThrowAsync_WithValidRequest_DoesNotThrow()
        {
            // Act
            var ex = await Record.ExceptionAsync(() => _validator.ValidateOrThrowAsync(new SearchQuotesRequest()));

            // Assert
            Assert.Null(ex);
        }
    }
}
