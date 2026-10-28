using API.Observability;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Serilog.Events;

namespace Tests.Unit.API.Observability
{
    public class RequestLogLevelTests
    {
        private static HttpContext ContextFor(string path, int statusCode)
        {
            var context = new DefaultHttpContext();
            context.Request.Path = path;
            context.Response.StatusCode = statusCode;
            return context;
        }

        [Theory]
        [InlineData("/api/v1/quotes", 201, LogEventLevel.Information)]
        [InlineData("/api/v1/quotes", 404, LogEventLevel.Information)]
        [InlineData("/api/v1/quotes", 500, LogEventLevel.Error)]
        [InlineData("/health/live", 200, LogEventLevel.Debug)]
        [InlineData("/health/ready", 503, LogEventLevel.Error)]
        public void RequestLogLevel_ForPathAndStatus_ReturnsExpectedLevel(string path, int statusCode, LogEventLevel expected)
        {
            // Arrange
            var context = ContextFor(path, statusCode);

            // Act
            var level = ObservabilityServiceCollection.RequestLogLevel(context, 1.0, null);

            // Assert
            Assert.Equal(expected, level);
        }

        [Fact]
        public void RequestLogLevel_WithException_ReturnsError()
        {
            // Arrange
            var context = ContextFor("/api/v1/quotes", 200);

            // Act
            var level = ObservabilityServiceCollection.RequestLogLevel(context, 1.0, new InvalidOperationException());

            // Assert
            Assert.Equal(LogEventLevel.Error, level);
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData("json", false)]
        [InlineData("text", true)]
        [InlineData("TEXT", true)]
        public void UseTextLogs_WithLogFormat_ReturnsWhetherText(string? format, bool expected)
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { [ObservabilityServiceCollection.LogFormatKey] = format })
                .Build();

            // Act
            var result = ObservabilityServiceCollection.UseTextLogs(configuration);

            // Assert
            Assert.Equal(expected, result);
        }
    }
}
