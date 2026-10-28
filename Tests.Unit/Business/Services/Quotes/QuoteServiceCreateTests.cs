using Business.Abstractions.Domain.Quotes;
using Business.Abstractions.Exceptions;
using Business.Services.Quotes;
using Contracts.API.Public.Quotes.Requests;
using Moq;
using Pricing;

namespace Tests.Unit.Business.Services.Quotes
{
    public partial class QuoteServiceTests
    {
        private static readonly PremiumResult Premium = new(
            912.34m, "ZAR", "v1", new Dictionary<string, decimal> { ["base"] = 0.85m, ["age"] = 1.07m });

        private void SetupPricing() =>
            _calculator.Setup(c => c.Calculate(It.IsAny<PremiumRequest>())).Returns(Premium);

        // Returns the candidate as stored, filling the database generated reference
        private void SetupInsertReturnsCandidate() =>
            _repository
                .Setup(r => r.AddUnlessDuplicateAsync(It.IsAny<Quote>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Quote candidate, DateTime _, CancellationToken _) =>
                {
                    candidate.Reference = "QT-000042";
                    return candidate;
                });

        private void SetupRepositoryReturns(Quote existing) =>
            _repository
                .Setup(r => r.AddUnlessDuplicateAsync(It.IsAny<Quote>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);

        [Fact]
        public async Task CreateQuoteAsync_WithNewRequest_StoresDraftWithInitialHistory()
        {
            // Arrange
            SetupPricing();
            SetupInsertReturnsCandidate();

            // Act
            var outcome = await CreateService().CreateQuoteAsync(QuoteTestData.CreateRequest(), QuoteTestData.AdviserId, "key-1");

            // Assert
            Assert.False(outcome.IsReplay);
            var quote = outcome.Quote;
            Assert.NotEqual(Guid.Empty, quote.Id);
            Assert.NotEqual(Guid.Empty, quote.Identifier);
            Assert.Equal("QT-000042", quote.Reference);
            Assert.Equal(QuoteTestData.AdviserId, quote.AdviserId);
            Assert.Equal("Thandi Mokoena", quote.ClientFullName);
            Assert.Equal("TERM_LIFE", quote.ProductCode);
            Assert.Equal(912.34m, quote.MonthlyPremium);
            Assert.Equal("ZAR", quote.Currency);
            Assert.Equal("v1", quote.PricingVersion);
            Assert.Equal(1.07m, quote.RatingFactors["age"]);
            Assert.Equal(QuoteStatus.Draft, quote.Status);
            Assert.Equal("key-1", quote.IdempotencyKey);
            Assert.Equal(QuoteFingerprint.Compute(QuoteTestData.AdviserId, QuoteTestData.CreateRequest()), quote.RequestFingerprint);
            Assert.Equal(QuoteTestData.Now, quote.CreatedAt);
            Assert.Equal(QuoteTestData.Now, quote.UpdatedAt);

            var history = Assert.Single(quote.StatusHistory);
            Assert.Null(history.FromStatus);
            Assert.Equal(QuoteStatus.Draft, history.ToStatus);
            Assert.Equal(QuoteTestData.AdviserId, history.ChangedBy);
            Assert.Equal(quote.Id, history.QuoteId);
        }

        [Fact]
        public async Task CreateQuoteAsync_PassesNormalisedRequestToPricing()
        {
            // Arrange
            PremiumRequest? priced = null;
            _calculator.Setup(c => c.Calculate(It.IsAny<PremiumRequest>()))
                .Callback<PremiumRequest>(r => priced = r)
                .Returns(Premium);
            SetupInsertReturnsCandidate();

            // Act
            await CreateService().CreateQuoteAsync(QuoteTestData.CreateRequest(), QuoteTestData.AdviserId, null);

            // Assert
            Assert.NotNull(priced);
            Assert.Equal("TERM_LIFE", priced.ProductCode);
            Assert.Equal(1_000_000m, priced.CoverAmount);
            Assert.Equal(20, priced.TermYears);
            Assert.Equal(new DateOnly(1985, 4, 12), priced.DateOfBirth);
            Assert.Equal(DateOnly.FromDateTime(QuoteTestData.Now), priced.QuoteDate);
        }

        [Fact]
        public async Task CreateQuoteAsync_PassesWindowStartFromOptions()
        {
            // Arrange
            SetupPricing();
            SetupInsertReturnsCandidate();

            // Act
            await CreateService().CreateQuoteAsync(QuoteTestData.CreateRequest(), QuoteTestData.AdviserId, null);

            // Assert
            _repository.Verify(r => r.AddUnlessDuplicateAsync(
                It.IsAny<Quote>(), QuoteTestData.Now.AddMinutes(-WindowMinutes), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("   ")]
        public async Task CreateQuoteAsync_WithBlankNotes_StoresNull(string? notes)
        {
            // Arrange
            SetupPricing();
            SetupInsertReturnsCandidate();

            // Act
            var outcome = await CreateService().CreateQuoteAsync(QuoteTestData.CreateRequest(notes), QuoteTestData.AdviserId, null);

            // Assert
            Assert.Null(outcome.Quote.Notes);
        }

        [Fact]
        public async Task CreateQuoteAsync_WhenDuplicateFingerprintFound_ReturnsReplay()
        {
            // Arrange
            SetupPricing();
            var fingerprint = QuoteFingerprint.Compute(QuoteTestData.AdviserId, QuoteTestData.CreateRequest());
            var existing = QuoteTestData.Quote(fingerprint: fingerprint);
            SetupRepositoryReturns(existing);

            // Act
            var outcome = await CreateService().CreateQuoteAsync(QuoteTestData.CreateRequest(), QuoteTestData.AdviserId, null);

            // Assert
            Assert.True(outcome.IsReplay);
            Assert.Same(existing, outcome.Quote);
        }

        [Fact]
        public async Task CreateQuoteAsync_WhenSameKeyAndSameBody_ReturnsReplay()
        {
            // Arrange
            SetupPricing();
            var fingerprint = QuoteFingerprint.Compute(QuoteTestData.AdviserId, QuoteTestData.CreateRequest());
            var existing = QuoteTestData.Quote(fingerprint: fingerprint, idempotencyKey: "key-1");
            SetupRepositoryReturns(existing);

            // Act
            var outcome = await CreateService().CreateQuoteAsync(QuoteTestData.CreateRequest(), QuoteTestData.AdviserId, "key-1");

            // Assert
            Assert.True(outcome.IsReplay);
            Assert.Equal(existing.Identifier, outcome.Quote.Identifier);
        }

        [Fact]
        public async Task CreateQuoteAsync_WhenSameKeyWithDifferentBody_ThrowsConflict()
        {
            // Arrange
            SetupPricing();
            var existing = QuoteTestData.Quote(fingerprint: new string('a', 64), idempotencyKey: "key-1");
            SetupRepositoryReturns(existing);

            // Act
            var ex = await Assert.ThrowsAsync<ConflictException>(
                () => CreateService().CreateQuoteAsync(QuoteTestData.CreateRequest(), QuoteTestData.AdviserId, "key-1"));

            // Assert
            Assert.Equal(409, ex.StatusCode);
        }

        [Fact]
        public async Task CreateQuoteAsync_WhenPricingRuleBreached_ThrowsBadRequestBeforeRepositoryCall()
        {
            // Arrange
            _calculator.Setup(c => c.Calculate(It.IsAny<PremiumRequest>()))
                .Throws(new PricingRuleException("Client age 70 is outside the entry ages for TERM_LIFE (18 to 65)"));

            // Act
            var ex = await Assert.ThrowsAsync<BadRequestException>(
                () => CreateService().CreateQuoteAsync(QuoteTestData.CreateRequest(), QuoteTestData.AdviserId, null));

            // Assert
            Assert.Contains("entry ages", ex.Message);
            Assert.Equal("Product", Assert.Single(ex.Errors).Field);
            _repository.Verify(
                r => r.AddUnlessDuplicateAsync(It.IsAny<Quote>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        public static TheoryData<CreateQuoteRequest?> InvalidCreateRequests => new()
        {
            null,
            new CreateQuoteRequest(),
            new CreateQuoteRequest
            {
                Client = QuoteTestData.CreateRequest().Client,
                Product = new QuoteProductRequest { Code = "TERM_LIFE", CoverAmount = 0, TermYears = 20 }
            }
        };

        [Theory]
        [MemberData(nameof(InvalidCreateRequests))]
        public async Task CreateQuoteAsync_WithInvalidRequest_ThrowsBeforePricingOrRepositoryCall(CreateQuoteRequest? request)
        {
            // Act
            await Assert.ThrowsAsync<BadRequestException>(
                () => CreateService().CreateQuoteAsync(request!, QuoteTestData.AdviserId, null));

            // Assert
            _calculator.Verify(c => c.Calculate(It.IsAny<PremiumRequest>()), Times.Never);
            _repository.Verify(
                r => r.AddUnlessDuplicateAsync(It.IsAny<Quote>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task CreateQuoteAsync_WithoutAdviser_ThrowsBeforeRepositoryCall()
        {
            // Act
            await Assert.ThrowsAsync<BadRequestException>(
                () => CreateService().CreateQuoteAsync(QuoteTestData.CreateRequest(), " ", null));

            // Assert
            _repository.Verify(
                r => r.AddUnlessDuplicateAsync(It.IsAny<Quote>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
