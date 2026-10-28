using API.Filters;
using Business.Abstractions.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Public
{
    /// <summary>
    /// Base for public controllers: versioned route, exception to HTTP mapping, and the caller context.
    /// Authentication is out of scope, so the adviser comes from a header (see docs/ASSUMPTIONS.md A-03).
    /// </summary>
    [ApiController]
    [ApiExceptionHandler]
    [Produces("application/json")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class BaseController : ControllerBase
    {
        public const string AdviserIdHeader = "X-Adviser-Id";
        public const string IdempotencyKeyHeader = "Idempotency-Key";
        public const string IdempotentReplayedHeader = "Idempotent-Replayed";

        private const int MaxHeaderValueLength = 100;

        protected string AdviserId
        {
            get
            {
                var value = Request.Headers[AdviserIdHeader].ToString().Trim();
                if (string.IsNullOrEmpty(value) || value.Length > MaxHeaderValueLength)
                {
                    throw new BadRequestException($"Header {AdviserIdHeader} is required (max {MaxHeaderValueLength} characters)");
                }

                return value;
            }
        }

        protected string? IdempotencyKey
        {
            get
            {
                var value = Request.Headers[IdempotencyKeyHeader].ToString().Trim();
                if (string.IsNullOrEmpty(value))
                {
                    return null;
                }

                if (value.Length > MaxHeaderValueLength)
                {
                    throw new BadRequestException($"Header {IdempotencyKeyHeader} must not exceed {MaxHeaderValueLength} characters");
                }

                return value;
            }
        }
    }
}
