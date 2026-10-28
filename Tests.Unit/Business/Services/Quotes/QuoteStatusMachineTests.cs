using Business.Abstractions.Domain.Quotes;
using Business.Abstractions.Exceptions;
using Business.Services.Quotes;

namespace Tests.Unit.Business.Services.Quotes
{
    public class QuoteStatusMachineTests
    {
        public static TheoryData<QuoteStatus, QuoteStatus> AllowedMoves => new()
        {
            { QuoteStatus.Draft, QuoteStatus.SentToClient },
            { QuoteStatus.Draft, QuoteStatus.Declined },
            { QuoteStatus.SentToClient, QuoteStatus.Accepted },
            { QuoteStatus.SentToClient, QuoteStatus.Declined }
        };

        public static TheoryData<QuoteStatus, QuoteStatus> RejectedMoves => new()
        {
            { QuoteStatus.Draft, QuoteStatus.Draft },
            { QuoteStatus.Draft, QuoteStatus.Accepted },
            { QuoteStatus.SentToClient, QuoteStatus.SentToClient },
            { QuoteStatus.SentToClient, QuoteStatus.Draft },
            { QuoteStatus.Accepted, QuoteStatus.Accepted },
            { QuoteStatus.Accepted, QuoteStatus.Draft },
            { QuoteStatus.Accepted, QuoteStatus.SentToClient },
            { QuoteStatus.Accepted, QuoteStatus.Declined },
            { QuoteStatus.Declined, QuoteStatus.Declined },
            { QuoteStatus.Declined, QuoteStatus.Draft },
            { QuoteStatus.Declined, QuoteStatus.SentToClient },
            { QuoteStatus.Declined, QuoteStatus.Accepted }
        };

        [Theory]
        [MemberData(nameof(AllowedMoves))]
        public void EnsureCanMove_WithAllowedMove_DoesNotThrow(QuoteStatus from, QuoteStatus to)
        {
            // Act
            var ex = Record.Exception(() => QuoteStatusMachine.EnsureCanMove(from, to));

            // Assert
            Assert.Null(ex);
            Assert.True(QuoteStatusMachine.CanMove(from, to));
        }

        [Theory]
        [MemberData(nameof(RejectedMoves))]
        public void EnsureCanMove_WithRejectedMove_ThrowsBadRequest(QuoteStatus from, QuoteStatus to)
        {
            // Act
            var ex = Assert.Throws<BadRequestException>(() => QuoteStatusMachine.EnsureCanMove(from, to));

            // Assert
            Assert.Equal(400, ex.StatusCode);
            Assert.Contains($"from {from} to {to}", ex.Message);
            Assert.False(QuoteStatusMachine.CanMove(from, to));
        }

        [Theory]
        [InlineData(QuoteStatus.Accepted)]
        [InlineData(QuoteStatus.Declined)]
        public void AllowedTargets_ForFinalStatus_IsEmpty(QuoteStatus status)
        {
            // Act
            var targets = QuoteStatusMachine.AllowedTargets(status);

            // Assert
            Assert.Empty(targets);
        }

        [Fact]
        public void EnsureCanMove_FromFinalStatus_SaysItIsFinal()
        {
            // Act
            var ex = Assert.Throws<BadRequestException>(
                () => QuoteStatusMachine.EnsureCanMove(QuoteStatus.Accepted, QuoteStatus.Declined));

            // Assert
            Assert.Contains("Accepted is a final status", ex.Message);
        }
    }
}
