using Asp.Versioning;
using Business.Abstractions.Interfaces.Business.Quotes;
using Business.Mappers;
using Contracts.API.Common.Responses;
using Contracts.API.Public.Quotes.Requests;
using Contracts.API.Public.Quotes.Responses;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Public.v1.Quotes
{
    [ApiVersion("1.0")]
    public class QuoteController(IQuoteService quoteService) : BaseController
    {
        public const string GetQuoteRouteName = "GetQuote";

        /// <summary>
        /// Prices and stores a new quote in Draft. Send an Idempotency-Key header to make retries safe; a duplicate
        /// submission returns the original quote with 200 and Idempotent-Replayed: true.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(QuoteDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(QuoteDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateAsync([FromBody] CreateQuoteRequest request, CancellationToken cancellationToken)
        {
            var outcome = await quoteService.CreateQuoteAsync(request, AdviserId, IdempotencyKey, cancellationToken);
            var dto = outcome.Quote.ToDto();

            if (outcome.IsReplay)
            {
                Response.Headers[IdempotentReplayedHeader] = "true";
                return Ok(dto);
            }

            // "1" rather than "1.0" so Location reads /api/v1/Quote/{identifier}, like every documented route
            return CreatedAtRoute(GetQuoteRouteName, new { version = "1", identifier = dto.Identifier }, dto);
        }

        /// <summary>Gets one of the calling adviser's quotes, with its status history.</summary>
        [HttpGet("{identifier:guid}", Name = GetQuoteRouteName)]
        [ProducesResponseType(typeof(QuoteDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAsync(Guid identifier, CancellationToken cancellationToken)
        {
            var quote = await quoteService.GetQuoteAsync(identifier, AdviserId, cancellationToken);
            return quote is null ? QuoteNotFound(identifier) : Ok(quote.ToDto());
        }

        // Same ErrorResponse body as every other error, so clients parse one shape
        private NotFoundObjectResult QuoteNotFound(Guid identifier) => NotFound(new ErrorResponse
        {
            StatusCode = StatusCodes.Status404NotFound,
            Message = $"Quote {identifier} was not found"
        });
    }
}
