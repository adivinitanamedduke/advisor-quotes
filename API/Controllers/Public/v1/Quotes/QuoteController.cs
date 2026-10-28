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

        /// <summary>Lists the calling adviser's quotes, newest first, optionally filtered by status.</summary>
        [HttpPost("search")]
        [ProducesResponseType(typeof(PagedResult<QuoteSummaryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SearchAsync([FromBody] SearchQuotesRequest request, CancellationToken cancellationToken)
        {
            var page = await quoteService.SearchQuotesAsync(request, AdviserId, cancellationToken);
            return Ok(page.ToPagedResult());
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

        /// <summary>
        /// Moves a quote to a new status and records the change in its history.
        /// Allowed: Draft to SentToClient or Declined; SentToClient to Accepted or Declined.
        /// </summary>
        [HttpPut("{identifier:guid}/status")]
        [ProducesResponseType(typeof(QuoteDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateStatusAsync(
            Guid identifier,
            [FromBody] UpdateQuoteStatusRequest request,
            CancellationToken cancellationToken)
        {
            var quote = await quoteService.UpdateQuoteStatusAsync(identifier, request, AdviserId, cancellationToken);
            return Ok(quote.ToDto());
        }

        // Same ErrorResponse body as every other error, so clients parse one shape
        private NotFoundObjectResult QuoteNotFound(Guid identifier) => NotFound(new ErrorResponse
        {
            StatusCode = StatusCodes.Status404NotFound,
            Message = $"Quote {identifier} was not found"
        });
    }
}
