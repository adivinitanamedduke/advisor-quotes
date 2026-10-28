namespace Contracts.API.Common.Responses
{
    /// <summary>
    /// Body of every 4xx/5xx response: { statusCode, message, errors[] }.
    /// </summary>
    public class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public IReadOnlyList<ErrorDetail> Errors { get; set; } = [];
    }

    public class ErrorDetail
    {
        public string? Field { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
