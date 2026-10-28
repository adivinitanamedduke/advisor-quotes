using API.Controllers.Public;
using API.Controllers.Public.v1.Quotes;
using Business.Abstractions.Domain.Quotes;
using Business.Abstractions.Exceptions;
using Business.Abstractions.Interfaces.Business.Quotes;
using Contracts.API.Common.Responses;
using Contracts.API.Public.Quotes.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Tests.Unit.Business.Services.Quotes;

namespace Tests.Unit.API.Controllers.Quotes
{
    public partial class QuoteControllerTests
    {
        private readonly Mock<IQuoteService> _service = new(MockBehavior.Strict);

        private QuoteController CreateController(string? adviserId = QuoteTestData.AdviserId, string? idempotencyKey = null)
        {
            var httpContext = new DefaultHttpContext();
            if (adviserId is not null)
            {
                httpContext.Request.Headers[BaseController.AdviserIdHeader] = adviserId;
            }

            if (idempotencyKey is not null)
            {
                httpContext.Request.Headers[BaseController.IdempotencyKeyHeader] = idempotencyKey;
            }

            return new QuoteController(_service.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext }
            };
        }

        [Fact]
        public async Task GetAsync_WhenFound_Returns200WithDto()
        {
            // Arrange
            var quote = QuoteTestData.Quote();
            _service
                .Setup(s => s.GetQuoteAsync(quote.Identifier, QuoteTestData.AdviserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(quote);

            // Act
            var result = await CreateController().GetAsync(quote.Identifier, CancellationToken.None);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<QuoteDto>(ok.Value);
            Assert.Equal(quote.Identifier, dto.Identifier);
        }

        [Fact]
        public async Task GetAsync_WhenMissing_Returns404WithErrorBody()
        {
            // Arrange
            _service
                .Setup(s => s.GetQuoteAsync(It.IsAny<Guid>(), QuoteTestData.AdviserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Quote?)null);

            // Act
            var result = await CreateController().GetAsync(Guid.NewGuid(), CancellationToken.None);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var body = Assert.IsType<ErrorResponse>(notFound.Value);
            Assert.Equal(404, body.StatusCode);
        }

        [Fact]
        public async Task GetAsync_WithoutAdviserHeader_ThrowsBadRequest()
        {
            // Act
            var ex = await Assert.ThrowsAsync<BadRequestException>(
                () => CreateController(adviserId: null).GetAsync(Guid.NewGuid(), CancellationToken.None));

            // Assert
            Assert.Equal(400, ex.StatusCode);
            _service.VerifyNoOtherCalls();
        }
    }
}
