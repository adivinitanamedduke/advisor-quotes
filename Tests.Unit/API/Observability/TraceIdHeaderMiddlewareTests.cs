using System.Diagnostics;
using API.Observability;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Tests.Unit.API.Observability
{
    public class TraceIdHeaderMiddlewareTests
    {
        /// <summary>DefaultHttpContext ignores OnStarting, so this feature records the callbacks for the test to fire.</summary>
        private sealed class StartableResponseFeature : HttpResponseFeature
        {
            private readonly List<(Func<object, Task> Callback, object State)> _onStarting = [];

            public override void OnStarting(Func<object, Task> callback, object state) => _onStarting.Add((callback, state));

            public async Task StartAsync()
            {
                foreach (var (callback, state) in _onStarting.AsEnumerable().Reverse())
                {
                    await callback(state);
                }
            }
        }

        private static (DefaultHttpContext Context, StartableResponseFeature Response) NewContext()
        {
            var response = new StartableResponseFeature();
            var context = new DefaultHttpContext();
            context.Features.Set<IHttpResponseFeature>(response);
            context.TraceIdentifier = "fallback-id";
            return (context, response);
        }

        [Fact]
        public async Task InvokeAsync_WithCurrentActivity_SetsHeaderToTraceId()
        {
            // Arrange
            using var activity = new Activity("request").SetIdFormat(ActivityIdFormat.W3C).Start();
            var (context, response) = NewContext();
            var middleware = new TraceIdHeaderMiddleware(_ => Task.CompletedTask);

            // Act
            await middleware.InvokeAsync(context);
            await response.StartAsync();

            // Assert
            Assert.Equal(activity.TraceId.ToHexString(), context.Response.Headers[TraceIdHeaderMiddleware.HeaderName].ToString());
        }

        [Fact]
        public async Task InvokeAsync_WithoutActivity_FallsBackToTraceIdentifier()
        {
            // Arrange
            Activity.Current = null;
            var (context, response) = NewContext();
            var middleware = new TraceIdHeaderMiddleware(_ => Task.CompletedTask);

            // Act
            await middleware.InvokeAsync(context);
            await response.StartAsync();

            // Assert
            Assert.Equal("fallback-id", context.Response.Headers[TraceIdHeaderMiddleware.HeaderName].ToString());
        }

        [Fact]
        public async Task InvokeAsync_WhenHeadersClearedForErrorResponse_StillSetsHeader()
        {
            // Arrange
            using var activity = new Activity("request").SetIdFormat(ActivityIdFormat.W3C).Start();
            var (context, response) = NewContext();
            var middleware = new TraceIdHeaderMiddleware(ctx =>
            {
                // What an error handler does before writing a 500
                ctx.Response.Headers.Clear();
                ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
                return Task.CompletedTask;
            });

            // Act
            await middleware.InvokeAsync(context);
            await response.StartAsync();

            // Assert
            Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
            Assert.Equal(activity.TraceId.ToHexString(), context.Response.Headers[TraceIdHeaderMiddleware.HeaderName].ToString());
        }
    }
}
