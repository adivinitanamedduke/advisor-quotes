using Business.Abstractions.Domain.Quotes;
using Business.Abstractions.Models;
using Business.Mappers;
using Contracts.API.Public.Quotes.Responses;
using Tests.Unit.Business.Services.Quotes;

namespace Tests.Unit.Business.Mappers
{
    public class QuoteMapperTests
    {
        [Fact]
        public void ToDto_MapsEveryPublicField()
        {
            // Arrange
            var quote = QuoteTestData.Quote(QuoteStatus.SentToClient);

            // Act
            var dto = quote.ToDto();

            // Assert
            Assert.Equal(quote.Identifier, dto.Identifier);
            Assert.Equal(quote.Reference, dto.Reference);
            Assert.Equal(quote.AdviserId, dto.AdviserId);
            Assert.Equal(quote.ClientFullName, dto.Client.FullName);
            Assert.Equal(quote.ClientDateOfBirth, dto.Client.DateOfBirth);
            Assert.Equal(quote.ProductCode, dto.Product.Code);
            Assert.Equal(quote.CoverAmount, dto.Product.CoverAmount);
            Assert.Equal(quote.TermYears, dto.Product.TermYears);
            Assert.Equal(quote.MonthlyPremium, dto.MonthlyPremium);
            Assert.Equal("ZAR", dto.Currency);
            Assert.Equal("v1", dto.PricingVersion);
            Assert.Equal(0.85m, dto.RatingFactors["base"]);
            Assert.Equal("SentToClient", dto.Status);
            Assert.Equal(quote.Notes, dto.Notes);
        }

        [Fact]
        public void ToDto_DoesNotExposeInternalFields()
        {
            // Act
            var properties = typeof(QuoteDto).GetProperties().Select(p => p.Name).ToList();

            // Assert
            Assert.DoesNotContain("Id", properties);
            Assert.DoesNotContain("IdempotencyKey", properties);
            Assert.DoesNotContain("RequestFingerprint", properties);
        }

        [Fact]
        public void ToDto_OrdersHistoryByChangedAt()
        {
            // Arrange
            var quote = QuoteTestData.Quote(QuoteStatus.SentToClient);
            quote.StatusHistory.Insert(0, new QuoteStatusChange
            {
                FromStatus = QuoteStatus.Draft,
                ToStatus = QuoteStatus.SentToClient,
                Reason = "Emailed",
                ChangedBy = "adviser-1",
                ChangedAt = QuoteTestData.Now
            });

            // Act
            var history = quote.ToDto().StatusHistory;

            // Assert
            Assert.Equal(2, history.Count);
            Assert.Null(history[0].FromStatus);
            Assert.Equal("Draft", history[0].ToStatus);
            Assert.Equal("Draft", history[1].FromStatus);
            Assert.Equal("SentToClient", history[1].ToStatus);
            Assert.Equal("Emailed", history[1].Reason);
        }

        [Fact]
        public void ToPagedResult_MapsItemsAndPaging()
        {
            // Arrange
            var page = new PagedList<Quote>
            {
                Items = [QuoteTestData.Quote(), QuoteTestData.Quote(QuoteStatus.Declined)],
                TotalCount = 7,
                PageNumber = 2,
                PageSize = 2
            };

            // Act
            var result = page.ToPagedResult();

            // Assert
            Assert.Equal(2, result.Items.Count);
            Assert.Equal("Declined", result.Items[1].Status);
            Assert.Equal(7, result.TotalCount);
            Assert.Equal(2, result.PageNumber);
            Assert.Equal(2, result.PageSize);
        }

        [Theory]
        [InlineData("Draft", QuoteStatus.Draft)]
        [InlineData("SentToClient", QuoteStatus.SentToClient)]
        [InlineData("Accepted", QuoteStatus.Accepted)]
        [InlineData("Declined", QuoteStatus.Declined)]
        public void ToQuoteStatus_RoundTripsStatusNames(string name, QuoteStatus expected)
        {
            // Act
            var status = QuoteMapper.ToQuoteStatus(name);

            // Assert
            Assert.Equal(expected, status);
            Assert.Equal(name, QuoteMapper.ToStatusName(status));
        }
    }
}
