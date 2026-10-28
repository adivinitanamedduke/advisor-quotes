using Business.Abstractions.Domain.Quotes;
using Business.Abstractions.Exceptions;
using Business.Abstractions.Models;
using Contracts.API.Common.Responses;
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
        public async Task SearchAsync_Returns200WithPagedSummaries()
        {
            // Arrange
            var request = new SearchQuotesRequest { Status = "Draft" };
            var page = new PagedList<Quote> { Items = [QuoteTestData.Quote()], TotalCount = 1, PageNumber = 1, PageSize = 20 };
            _service
                .Setup(s => s.SearchQuotesAsync(request, QuoteTestData.AdviserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(page);

            // Act
            var result = await CreateController().SearchAsync(request, CancellationToken.None);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            var body = Assert.IsType<PagedResult<QuoteSummaryDto>>(ok.Value);
            Assert.Single(body.Items);
            Assert.Equal(1, body.TotalCount);
        }

        [Fact]
        public async Task SearchAsync_WhenServiceRejectsRequest_PropagatesBadRequest()
        {
            // Arrange
            _service
                .Setup(s => s.SearchQuotesAsync(It.IsAny<SearchQuotesRequest>(), QuoteTestData.AdviserId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new BadRequestException("Page size must be between 1 and 100"));

            // Act
            var ex = await Assert.ThrowsAsync<BadRequestException>(
                () => CreateController().SearchAsync(new SearchQuotesRequest { PageSize = 0 }, CancellationToken.None));

            // Assert
            Assert.Equal(400, ex.StatusCode);
        }
    }
}
