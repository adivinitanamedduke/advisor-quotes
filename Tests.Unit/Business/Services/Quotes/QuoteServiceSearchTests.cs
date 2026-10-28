using Business.Abstractions.Domain.Quotes;
using Business.Abstractions.Exceptions;
using Business.Abstractions.Models;
using Contracts.API.Public.Quotes.Requests;
using Moq;

namespace Tests.Unit.Business.Services.Quotes
{
    public partial class QuoteServiceTests
    {
        private void SetupSearchReturns(PagedList<Quote> page) =>
            _repository
                .Setup(r => r.SearchAsync(
                    It.IsAny<string>(), It.IsAny<QuoteStatus?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(page);

        [Fact]
        public async Task SearchQuotesAsync_WithStatusFilter_PassesParsedStatusAndPaging()
        {
            // Arrange
            var page = new PagedList<Quote> { Items = [QuoteTestData.Quote(QuoteStatus.SentToClient)], TotalCount = 1, PageNumber = 2, PageSize = 5 };
            SetupSearchReturns(page);
            var request = new SearchQuotesRequest { Status = "SentToClient", PageNumber = 2, PageSize = 5 };

            // Act
            var result = await CreateService().SearchQuotesAsync(request, QuoteTestData.AdviserId);

            // Assert
            Assert.Same(page, result);
            _repository.Verify(r => r.SearchAsync(
                QuoteTestData.AdviserId, QuoteStatus.SentToClient, 2, 5, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SearchQuotesAsync_WithoutStatus_PassesNullFilter()
        {
            // Arrange
            SetupSearchReturns(new PagedList<Quote> { PageNumber = 1, PageSize = 20 });

            // Act
            await CreateService().SearchQuotesAsync(new SearchQuotesRequest(), QuoteTestData.AdviserId);

            // Assert
            _repository.Verify(r => r.SearchAsync(
                QuoteTestData.AdviserId, null, 1, 20, It.IsAny<CancellationToken>()), Times.Once);
        }

        public static TheoryData<SearchQuotesRequest?> InvalidSearchRequests => new()
        {
            null,
            new SearchQuotesRequest { Status = "Pending" },
            new SearchQuotesRequest { PageNumber = 0 },
            new SearchQuotesRequest { PageSize = 101 }
        };

        [Theory]
        [MemberData(nameof(InvalidSearchRequests))]
        public async Task SearchQuotesAsync_WithInvalidRequest_ThrowsBeforeRepositoryCall(SearchQuotesRequest? request)
        {
            // Act
            await Assert.ThrowsAsync<BadRequestException>(
                () => CreateService().SearchQuotesAsync(request!, QuoteTestData.AdviserId));

            // Assert
            _repository.Verify(r => r.SearchAsync(
                It.IsAny<string>(), It.IsAny<QuoteStatus?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task SearchQuotesAsync_WithoutAdviser_ThrowsBeforeRepositoryCall()
        {
            // Act
            await Assert.ThrowsAsync<BadRequestException>(
                () => CreateService().SearchQuotesAsync(new SearchQuotesRequest(), ""));

            // Assert
            _repository.Verify(r => r.SearchAsync(
                It.IsAny<string>(), It.IsAny<QuoteStatus?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
