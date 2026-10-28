using Business.Abstractions.Exceptions;
using Business.Abstractions.Interfaces.Repository.Quotes;
using Business.Services.Quotes;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Tests.Unit.Business.Services.Quotes
{
    public partial class QuoteServiceTests
    {
        private readonly Mock<IQuoteRepository> _repository = new(MockBehavior.Strict);

        private QuoteService CreateService() => new(_repository.Object, NullLogger<QuoteService>.Instance);

        [Fact]
        public async Task GetQuoteAsync_WhenQuoteExists_ReturnsIt()
        {
            // Arrange
            var quote = QuoteTestData.Quote();
            _repository
                .Setup(r => r.GetByIdentifierAsync(quote.Identifier, QuoteTestData.AdviserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(quote);

            // Act
            var result = await CreateService().GetQuoteAsync(quote.Identifier, QuoteTestData.AdviserId);

            // Assert
            Assert.Same(quote, result);
        }

        [Fact]
        public async Task GetQuoteAsync_WhenQuoteMissing_ReturnsNull()
        {
            // Arrange
            _repository
                .Setup(r => r.GetByIdentifierAsync(It.IsAny<Guid>(), QuoteTestData.AdviserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((global::Business.Abstractions.Domain.Quotes.Quote?)null);

            // Act
            var result = await CreateService().GetQuoteAsync(Guid.NewGuid(), QuoteTestData.AdviserId);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetQuoteAsync_WithoutAdviser_ThrowsBeforeRepositoryCall(string adviserId)
        {
            // Act
            await Assert.ThrowsAsync<BadRequestException>(() => CreateService().GetQuoteAsync(Guid.NewGuid(), adviserId));

            // Assert
            _repository.Verify(
                r => r.GetByIdentifierAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
