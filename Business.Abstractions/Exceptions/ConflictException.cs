namespace Business.Abstractions.Exceptions
{
    /// <summary>409: idempotency key reused with a different body, or a concurrent update clash.</summary>
    public class ConflictException(string message) : ApiException(409, message);
}
