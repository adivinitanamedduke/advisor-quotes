using Business.Abstractions.Domain.Quotes;
using Business.Abstractions.Exceptions;
using Contracts.API.Public.Quotes.Requests;
using Moq;

namespace Tests.Unit.Business.Services.Quotes
{
    public partial class QuoteServiceTests
    {
        private void SetupGetForUpdate(Quote? quote) =>
            _repository
                .Setup(r => r.GetForUpdateAsync(It.IsAny<Guid>(), QuoteTestData.AdviserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(quote);

        private void VerifyNoStatusRepositoryCalls()
        {
            _repository.Verify(
                r => r.GetForUpdateAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateQuoteStatusAsync_WithAllowedMove_UpdatesStatusAndAddsHistory()
        {
            // Arrange
            var quote = QuoteTestData.Quote();
            SetupGetForUpdate(quote);
            _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            var request = new UpdateQuoteStatusRequest { Status = "SentToClient", Reason = "  Emailed to client " };

            // Act
            var result = await CreateService().UpdateQuoteStatusAsync(quote.Identifier, request, QuoteTestData.AdviserId);

            // Assert
            Assert.Equal(QuoteStatus.SentToClient, result.Status);
            Assert.Equal(QuoteTestData.Now, result.UpdatedAt);
            Assert.Equal(2, result.StatusHistory.Count);
            var change = result.StatusHistory[^1];
            Assert.Equal(QuoteStatus.Draft, change.FromStatus);
            Assert.Equal(QuoteStatus.SentToClient, change.ToStatus);
            Assert.Equal("Emailed to client", change.Reason);
            Assert.Equal(QuoteTestData.AdviserId, change.ChangedBy);
            Assert.Equal(QuoteTestData.Now, change.ChangedAt);
            Assert.Equal(quote.Id, change.QuoteId);
            _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateQuoteStatusAsync_WhenQuoteMissing_ThrowsNotFound()
        {
            // Arrange
            SetupGetForUpdate(null);

            // Act
            var ex = await Assert.ThrowsAsync<NotFoundException>(() => CreateService().UpdateQuoteStatusAsync(
                Guid.NewGuid(), new UpdateQuoteStatusRequest { Status = "SentToClient" }, QuoteTestData.AdviserId));

            // Assert
            Assert.Equal(404, ex.StatusCode);
            _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory]
        [InlineData(QuoteStatus.Draft, "Draft")]
        [InlineData(QuoteStatus.Draft, "Accepted")]
        [InlineData(QuoteStatus.Accepted, "Declined")]
        [InlineData(QuoteStatus.Declined, "SentToClient")]
        public async Task UpdateQuoteStatusAsync_WithDisallowedMove_ThrowsBadRequestWithoutSaving(QuoteStatus current, string target)
        {
            // Arrange
            var quote = QuoteTestData.Quote(current);
            SetupGetForUpdate(quote);

            // Act
            await Assert.ThrowsAsync<BadRequestException>(() => CreateService().UpdateQuoteStatusAsync(
                quote.Identifier, new UpdateQuoteStatusRequest { Status = target }, QuoteTestData.AdviserId));

            // Assert
            Assert.Equal(current, quote.Status);
            Assert.Single(quote.StatusHistory);
            _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateQuoteStatusAsync_WhenSaveClashes_PropagatesConflict()
        {
            // Arrange
            SetupGetForUpdate(QuoteTestData.Quote());
            _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ConflictException("changed"));

            // Act
            var ex = await Assert.ThrowsAsync<ConflictException>(() => CreateService().UpdateQuoteStatusAsync(
                Guid.NewGuid(), new UpdateQuoteStatusRequest { Status = "Declined" }, QuoteTestData.AdviserId));

            // Assert
            Assert.Equal(409, ex.StatusCode);
        }

        public static TheoryData<UpdateQuoteStatusRequest?> InvalidStatusRequests => new()
        {
            null,
            new UpdateQuoteStatusRequest { Status = "" },
            new UpdateQuoteStatusRequest { Status = "Cancelled" },
            new UpdateQuoteStatusRequest { Status = "Declined", Reason = new string('x', 501) }
        };

        [Theory]
        [MemberData(nameof(InvalidStatusRequests))]
        public async Task UpdateQuoteStatusAsync_WithInvalidRequest_ThrowsBeforeRepositoryCall(UpdateQuoteStatusRequest? request)
        {
            // Act
            await Assert.ThrowsAsync<BadRequestException>(() => CreateService().UpdateQuoteStatusAsync(
                Guid.NewGuid(), request!, QuoteTestData.AdviserId));

            // Assert
            VerifyNoStatusRepositoryCalls();
        }

        [Fact]
        public async Task UpdateQuoteStatusAsync_WithoutAdviser_ThrowsBeforeRepositoryCall()
        {
            // Act
            await Assert.ThrowsAsync<BadRequestException>(() => CreateService().UpdateQuoteStatusAsync(
                Guid.NewGuid(), new UpdateQuoteStatusRequest { Status = "Declined" }, " "));

            // Assert
            VerifyNoStatusRepositoryCalls();
        }
    }
}
