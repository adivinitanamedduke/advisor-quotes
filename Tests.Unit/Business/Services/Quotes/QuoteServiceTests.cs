using Business.Abstractions.Config;
using Business.Abstractions.Domain.Quotes;
using Business.Abstractions.Exceptions;
using Business.Abstractions.Interfaces.Repository.Quotes;
using Business.Services.Quotes;
using Contracts.API.Public.Quotes.Requests.Validators;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Pricing;

namespace Tests.Unit.Business.Services.Quotes
{
    /// <summary>Shared setup and the get quote tests; each other flow has its own partial file.</summary>
    public partial class QuoteServiceTests
    {
        private const int WindowMinutes = 10;

        private readonly Mock<IQuoteRepository> _repository = new(MockBehavior.Strict);
        private readonly Mock<IPremiumCalculator> _calculator = new(MockBehavior.Strict);

        private QuoteService CreateService() => new(
            _repository.Object,
            _calculator.Object,
            new CreateQuoteRequestValidator(),
            new SearchQuotesRequestValidator(),
            Options.Create(new IdempotencyOptions { WindowMinutes = WindowMinutes }),
            new FixedTimeProvider(QuoteTestData.Now),
            NullLogger<QuoteService>.Instance);

        private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
        {
            public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
        }

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
                .ReturnsAsync((Quote?)null);

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
