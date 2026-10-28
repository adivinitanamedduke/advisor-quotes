namespace Business.Abstractions.Exceptions
{
    /// <summary>
    /// Base for exceptions that map to an HTTP status code. Mapped by API/Filters/ApiExceptionHandlerAttribute.
    /// </summary>
    public abstract class ApiException(int statusCode, string message, IReadOnlyList<ApiError>? errors = null)
        : Exception(message)
    {
        public int StatusCode { get; } = statusCode;
        public IReadOnlyList<ApiError> Errors { get; } = errors ?? [];
    }

    public record ApiError(string? Field, string Message);
}
