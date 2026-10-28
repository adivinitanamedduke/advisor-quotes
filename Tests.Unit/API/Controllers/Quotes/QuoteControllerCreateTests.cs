using API.Controllers.Public;
using API.Controllers.Public.v1.Quotes;
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
        public async Task CreateAsync_WhenCreated_Returns201AtGetRoute()
        {
            // Arrange
            var quote = QuoteTestData.Quote();
            _service
                .Setup(s => s.CreateQuoteAsync(It.IsAny<CreateQuoteRequest>(), QuoteTestData.AdviserId, "key-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CreateQuoteOutcome(quote, IsReplay: false));
            var controller = CreateController(idempotencyKey: "key-1");

            // Act
            var result = await controller.CreateAsync(QuoteTestData.CreateRequest(), CancellationToken.None);

            // Assert
            var created = Assert.IsType<CreatedAtRouteResult>(result);
            Assert.Equal(201, created.StatusCode);
            Assert.Equal(QuoteController.GetQuoteRouteName, created.RouteName);
            Assert.Equal(quote.Identifier, created.RouteValues!["identifier"]);
            Assert.Equal(quote.Identifier, Assert.IsType<QuoteDto>(created.Value).Identifier);
            Assert.False(controller.Response.Headers.ContainsKey(BaseController.IdempotentReplayedHeader));
        }

        [Fact]
        public async Task CreateAsync_WhenReplay_Returns200WithReplayHeader()
        {
            // Arrange
            var quote = QuoteTestData.Quote();
            _service
                .Setup(s => s.CreateQuoteAsync(It.IsAny<CreateQuoteRequest>(), QuoteTestData.AdviserId, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CreateQuoteOutcome(quote, IsReplay: true));
            var controller = CreateController();

            // Act
            var result = await controller.CreateAsync(QuoteTestData.CreateRequest(), CancellationToken.None);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(quote.Identifier, Assert.IsType<QuoteDto>(ok.Value).Identifier);
            Assert.Equal("true", controller.Response.Headers[BaseController.IdempotentReplayedHeader].ToString());
        }

        [Fact]
        public async Task CreateAsync_WhenServiceThrowsConflict_PropagatesForTheFilter()
        {
            // Arrange
            _service
                .Setup(s => s.CreateQuoteAsync(It.IsAny<CreateQuoteRequest>(), QuoteTestData.AdviserId, "key-1", It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ConflictException("reused"));

            // Act
            var ex = await Assert.ThrowsAsync<ConflictException>(
                () => CreateController(idempotencyKey: "key-1").CreateAsync(QuoteTestData.CreateRequest(), CancellationToken.None));

            // Assert
            Assert.Equal(409, ex.StatusCode);
        }

        [Fact]
        public async Task CreateAsync_WithoutAdviserHeader_ThrowsBadRequest()
        {
            // Act
            var ex = await Assert.ThrowsAsync<BadRequestException>(
                () => CreateController(adviserId: null).CreateAsync(QuoteTestData.CreateRequest(), CancellationToken.None));

            // Assert
            Assert.Equal(400, ex.StatusCode);
            _service.VerifyNoOtherCalls();
        }
    }
}
