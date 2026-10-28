using Asp.Versioning;
using Business.Abstractions.Interfaces.Business.Quotes;
using Business.Mappers;
using Contracts.API.Common.Responses;
using Contracts.API.Public.Quotes.Responses;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers.Public.v1.Quotes
{
    [ApiVersion("1.0")]
    public class QuoteController(IQuoteService quoteService) : BaseController
    {
        public const string GetQuoteRouteName = "GetQuote";

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
