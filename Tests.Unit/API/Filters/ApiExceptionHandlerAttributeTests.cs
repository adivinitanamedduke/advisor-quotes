using API.Filters;
using Business.Abstractions.Exceptions;
using Contracts.API.Common.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Tests.Unit.API.Filters
{
    public class ApiExceptionHandlerAttributeTests
    {
        private static ExceptionContext ContextFor(Exception exception)
        {
            var services = new ServiceCollection()
                .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
                .BuildServiceProvider();
            var httpContext = new DefaultHttpContext { RequestServices = services };
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            return new ExceptionContext(actionContext, []) { Exception = exception };
        }

        public static TheoryData<ApiException, int> ApiExceptions => new()
        {
            { new BadRequestException("boom"), 400 },
            { new NotFoundException("boom"), 404 },
            { new ConflictException("boom"), 409 }
        };

        [Theory]
        [MemberData(nameof(ApiExceptions))]
        public void OnException_WithApiException_MapsToItsStatusCode(ApiException exception, int expectedStatus)
        {
            // Arrange
            var context = ContextFor(exception);

            // Act
            new ApiExceptionHandlerAttribute().OnException(context);

            // Assert
            var result = Assert.IsType<ObjectResult>(context.Result);
            Assert.Equal(expectedStatus, result.StatusCode);
            var body = Assert.IsType<ErrorResponse>(result.Value);
            Assert.Equal(expectedStatus, body.StatusCode);
            Assert.Equal("boom", body.Message);
            Assert.True(context.ExceptionHandled);
        }

        [Fact]
        public void OnException_WithUnexpectedException_Returns500WithoutLeakingDetail()
        {
            // Arrange
            var context = ContextFor(new InvalidOperationException("secret detail"));

            // Act
            new ApiExceptionHandlerAttribute().OnException(context);

            // Assert
            var result = Assert.IsType<ObjectResult>(context.Result);
            Assert.Equal(500, result.StatusCode);
            var body = Assert.IsType<ErrorResponse>(result.Value);
            Assert.DoesNotContain("secret", body.Message);
        }
    }
}
