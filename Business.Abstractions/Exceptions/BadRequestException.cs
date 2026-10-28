namespace Business.Abstractions.Exceptions
{
    /// <summary>400: request shape is invalid or a business rule is breached.</summary>
    public class BadRequestException(string message, IReadOnlyList<ApiError>? errors = null)
        : ApiException(400, message, errors);
}
