using System.Diagnostics.Metrics;
using Business.Abstractions.Domain.Quotes;
using Business.Abstractions.Exceptions;
using Business.Abstractions.Telemetry;
using Business.Services.Quotes;
using Contracts.API.Public.Quotes.Requests;
using Moq;

namespace Tests.Unit.Business.Services.Quotes
{
    // Same class as the other service tests, so xUnit never runs these in parallel with code that also records
    public partial class QuoteServiceTests
    {
        private sealed record Measurement(string Instrument, long Value, IReadOnlyDictionary<string, object?> Tags);

        private static (MeterListener Listener, List<Measurement> Measurements) ListenToQuoteCounters()
        {
            var measurements = new List<Measurement>();
            var listener = new MeterListener
            {
                InstrumentPublished = (instrument, l) =>
                {
                    if (instrument.Meter.Name == QuoteTelemetry.SourceName)
                    {
                        l.EnableMeasurementEvents(instrument);
                    }
                }
            };
            listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
            {
                lock (measurements)
                {
                    measurements.Add(new Measurement(
                        instrument.Name, value, tags.ToArray().ToDictionary(t => t.Key, t => t.Value)));
                }
            });
            listener.Start();
            return (listener, measurements);
        }

        [Fact]
        public async Task CreateQuoteAsync_WhenCreated_IncrementsCreatedCounter()
        {
            // Arrange
            SetupPricing();
            SetupInsertReturnsCandidate();
            var (listener, measurements) = ListenToQuoteCounters();

            // Act
            using (listener)
            {
                await CreateService().CreateQuoteAsync(QuoteTestData.CreateRequest(), QuoteTestData.AdviserId, null);
            }

            // Assert
            var created = Assert.Single(measurements, m => m.Instrument == QuoteMetrics.QuotesCreated);
            Assert.Equal(1, created.Value);
            Assert.Equal("TERM_LIFE", created.Tags[QuoteTelemetry.Tags.ProductCode]);
            Assert.DoesNotContain(measurements, m => m.Instrument == QuoteMetrics.QuotesReplayed);
        }

        [Fact]
        public async Task CreateQuoteAsync_WhenReplayed_IncrementsReplayedCounter()
        {
            // Arrange
            SetupPricing();
            var fingerprint = QuoteFingerprint.Compute(QuoteTestData.AdviserId, QuoteTestData.CreateRequest());
            SetupRepositoryReturns(QuoteTestData.Quote(fingerprint: fingerprint));
            var (listener, measurements) = ListenToQuoteCounters();

            // Act
            using (listener)
            {
                await CreateService().CreateQuoteAsync(QuoteTestData.CreateRequest(), QuoteTestData.AdviserId, null);
            }

            // Assert
            var replayed = Assert.Single(measurements, m => m.Instrument == QuoteMetrics.QuotesReplayed);
            Assert.Equal("TERM_LIFE", replayed.Tags[QuoteTelemetry.Tags.ProductCode]);
            Assert.DoesNotContain(measurements, m => m.Instrument == QuoteMetrics.QuotesCreated);
        }

        [Fact]
        public async Task CreateQuoteAsync_WhenKeyReusedWithDifferentBody_IncrementsConflictCounter()
        {
            // Arrange
            SetupPricing();
            SetupRepositoryReturns(QuoteTestData.Quote(fingerprint: new string('a', 64), idempotencyKey: "key-1"));
            var (listener, measurements) = ListenToQuoteCounters();

            // Act
            using (listener)
            {
                await Assert.ThrowsAsync<ConflictException>(
                    () => CreateService().CreateQuoteAsync(QuoteTestData.CreateRequest(), QuoteTestData.AdviserId, "key-1"));
            }

            // Assert
            var conflict = Assert.Single(measurements, m => m.Instrument == QuoteMetrics.QuoteConflicts);
            Assert.Equal("TERM_LIFE", conflict.Tags[QuoteTelemetry.Tags.ProductCode]);
        }

        [Fact]
        public async Task UpdateQuoteStatusAsync_WhenSaved_IncrementsStatusChangeCounterWithTargetStatus()
        {
            // Arrange
            SetupGetForUpdate(QuoteTestData.Quote());
            _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            var (listener, measurements) = ListenToQuoteCounters();

            // Act
            using (listener)
            {
                await CreateService().UpdateQuoteStatusAsync(
                    Guid.NewGuid(), new UpdateQuoteStatusRequest { Status = "SentToClient" }, QuoteTestData.AdviserId);
            }

            // Assert
            var change = Assert.Single(measurements, m => m.Instrument == QuoteMetrics.QuoteStatusChanges);
            Assert.Equal("TERM_LIFE", change.Tags[QuoteTelemetry.Tags.ProductCode]);
            Assert.Equal("SentToClient", change.Tags[QuoteTelemetry.Tags.ToStatus]);
        }

        [Fact]
        public async Task UpdateQuoteStatusAsync_WhenTransitionRejected_DoesNotCount()
        {
            // Arrange
            SetupGetForUpdate(QuoteTestData.Quote(QuoteStatus.Accepted));
            var (listener, measurements) = ListenToQuoteCounters();

            // Act
            using (listener)
            {
                await Assert.ThrowsAsync<BadRequestException>(() => CreateService().UpdateQuoteStatusAsync(
                    Guid.NewGuid(), new UpdateQuoteStatusRequest { Status = "Declined" }, QuoteTestData.AdviserId));
            }

            // Assert
            Assert.DoesNotContain(measurements, m => m.Instrument == QuoteMetrics.QuoteStatusChanges);
        }
    }
}
