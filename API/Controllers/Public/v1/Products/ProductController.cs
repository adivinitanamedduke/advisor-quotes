using Asp.Versioning;
using Contracts.API.Public.Products.Responses;
using Microsoft.AspNetCore.Mvc;
using Pricing;

namespace API.Controllers.Public.v1.Products
{
    /// <summary>
    /// The product catalogue, so a UI can offer products and show their limits before quoting.
    /// Not adviser specific, so it does not read X-Adviser-Id. Base rates stay internal.
    /// </summary>
    [ApiVersion("1.0")]
    public class ProductController(IProductCatalog catalog) : BaseController
    {
        [HttpGet]
        [ProducesResponseType<ProductDto[]>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<ProductDto[]> GetAll() => Ok(catalog.GetAll().Select(ToDto).ToArray());

        private static ProductDto ToDto(ProductDefinition product) => new()
        {
            Code = product.Code,
            Name = product.Name,
            MinCoverAmount = product.MinCoverAmount,
            MaxCoverAmount = product.MaxCoverAmount,
            MinTermYears = product.MinTermYears,
            MaxTermYears = product.MaxTermYears,
            MinEntryAge = product.MinEntryAge,
            MaxEntryAge = product.MaxEntryAge
        };
    }
}
