using API.Observability;
using Business.Abstractions.Telemetry;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;

namespace Tests.Unit.API.Observability
{
    public class ObservabilityServiceCollectionTests
    {
        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData("http://localhost:4317", true)]
        public void UseOtlpExporter_WithEndpointSetting_ReturnsWhetherSet(string? endpoint, bool expected)
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { [ObservabilityServiceCollection.OtlpEndpointKey] = endpoint })
                .Build();

            // Act
            var result = ObservabilityServiceCollection.UseOtlpExporter(configuration);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void AddObservability_WhenBuilt_ListensToQuoteActivitySource()
        {
            // Arrange
            var builder = WebApplication.CreateBuilder();

            // Act
            builder.AddObservability();
            using var app = builder.Build();
            var tracerProvider = app.Services.GetService<TracerProvider>();
            using var activity = QuoteTelemetry.ActivitySource.StartActivity(QuoteTelemetry.Spans.QuoteCreate);

            // Assert
            Assert.NotNull(tracerProvider);
            Assert.NotNull(activity);
        }
    }
}
