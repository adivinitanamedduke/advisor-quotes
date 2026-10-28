using Business.Abstractions.Exceptions;
using Contracts.API.Common.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace API.Filters
{
    /// <summary>
    /// Maps ApiException subclasses to their status code and anything else to a logged 500,
    /// always with the ErrorResponse body. Controllers never catch these themselves.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class ApiExceptionHandlerAttribute : ExceptionFilterAttribute
    {
        public override void OnException(ExceptionContext context)
        {
            if (context.Exception is ApiException apiException)
            {
                context.Result = ErrorResult(apiException.StatusCode, apiException.Message, apiException.Errors);
                context.ExceptionHandled = true;
                return;
            }

            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILogger<ApiExceptionHandlerAttribute>>();
            logger.LogError(context.Exception, "Unhandled exception on {Method} {Path}",
                context.HttpContext.Request.Method, context.HttpContext.Request.Path);

            context.Result = ErrorResult(StatusCodes.Status500InternalServerError, "An unexpected error occurred", []);
            context.ExceptionHandled = true;
        }

        public static ObjectResult ErrorResult(int statusCode, string message, IEnumerable<ApiError> errors) =>
            new(new ErrorResponse
            {
                StatusCode = statusCode,
                Message = message,
                Errors = errors.Select(e => new ErrorDetail { Field = e.Field, Message = e.Message }).ToList()
            })
            {
                StatusCode = statusCode
            };
    }
}
