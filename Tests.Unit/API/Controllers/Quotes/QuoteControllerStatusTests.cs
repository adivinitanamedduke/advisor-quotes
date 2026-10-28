using Business.Abstractions.Domain.Quotes;
using Business.Abstractions.Exceptions;
using Contracts.API.Public.Quotes.Requests;
using Contracts.API.Public.Quotes.Responses;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Tests.Unit.Business.Services.Quotes;

namespace Tests.Unit.API.Controllers.Quotes
{
    public partial class QuoteControllerTests
    {
        [Fact]
        public async Task UpdateStatusAsync_WhenMoved_Returns200WithDto()
        {
            // Arrange
            var quote = QuoteTestData.Quote(QuoteStatus.SentToClient);
            var request = new UpdateQuoteStatusRequest { Status = "SentToClient" };
            _service
                .Setup(s => s.UpdateQuoteStatusAsync(quote.Identifier, request, QuoteTestData.AdviserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(quote);

            // Act
            var result = await CreateController().UpdateStatusAsync(quote.Identifier, request, CancellationToken.None);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("SentToClient", Assert.IsType<QuoteDto>(ok.Value).Status);
        }

        public static TheoryData<Exception, int> StatusFailures => new()
        {
            { new BadRequestException("bad move"), 400 },
            { new NotFoundException("missing"), 404 },
            { new ConflictException("clash"), 409 }
        };

        [Theory]
        [MemberData(nameof(StatusFailures))]
        public async Task UpdateStatusAsync_WhenServiceThrows_PropagatesForTheFilter(Exception failure, int expectedStatus)
        {
            // Arrange
            _service
                .Setup(s => s.UpdateQuoteStatusAsync(
                    It.IsAny<Guid>(), It.IsAny<UpdateQuoteStatusRequest>(), QuoteTestData.AdviserId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(failure);

            // Act
            var ex = await Assert.ThrowsAsync(failure.GetType(), () => CreateController().UpdateStatusAsync(
                Guid.NewGuid(), new UpdateQuoteStatusRequest { Status = "Declined" }, CancellationToken.None));

            // Assert
            Assert.Equal(expectedStatus, ((ApiException)ex).StatusCode);
        }

        [Fact]
        public async Task UpdateStatusAsync_WithoutAdviserHeader_ThrowsBadRequest()
        {
            // Act
            await Assert.ThrowsAsync<BadRequestException>(() => CreateController(adviserId: null).UpdateStatusAsync(
                Guid.NewGuid(), new UpdateQuoteStatusRequest { Status = "Declined" }, CancellationToken.None));

            // Assert
            _service.VerifyNoOtherCalls();
        }
    }
}
