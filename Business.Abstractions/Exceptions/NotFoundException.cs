namespace Business.Abstractions.Exceptions
{
    /// <summary>404: unknown identifier, or one owned by another adviser (never 403).</summary>
    public class NotFoundException(string message) : ApiException(404, message);
}
