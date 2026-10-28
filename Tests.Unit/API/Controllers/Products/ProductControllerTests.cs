using API.Controllers.Public.v1.Products;
using Contracts.API.Public.Products.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Pricing;

namespace Tests.Unit.API.Controllers.Products
{
    public class ProductControllerTests
    {
        private static readonly ProductDefinition TermLife = new()
        {
            Code = "TERM_LIFE",
            Name = "Term Life Cover",
            BaseRatePer1000 = 0.85m,
            MinCoverAmount = 100_000m,
            MaxCoverAmount = 20_000_000m,
            MinTermYears = 5,
            MaxTermYears = 30,
            MinEntryAge = 18,
            MaxEntryAge = 65
        };

        private static ProductController ControllerFor(params ProductDefinition[] products)
        {
            var catalog = new Mock<IProductCatalog>();
            catalog.Setup(c => c.GetAll()).Returns(products);

            // No X-Adviser-Id header: the catalogue is not adviser specific
            return new ProductController(catalog.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };
        }

        [Fact]
        public void GetAll_WithProducts_Returns200WithEveryProductMapped()
        {
            // Arrange
            var controller = ControllerFor(TermLife);

            // Act
            var result = controller.GetAll();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
            var dto = Assert.Single(Assert.IsType<ProductDto[]>(ok.Value));
            Assert.Equal("TERM_LIFE", dto.Code);
            Assert.Equal("Term Life Cover", dto.Name);
            Assert.Equal(100_000m, dto.MinCoverAmount);
            Assert.Equal(20_000_000m, dto.MaxCoverAmount);
            Assert.Equal(5, dto.MinTermYears);
            Assert.Equal(30, dto.MaxTermYears);
            Assert.Equal(18, dto.MinEntryAge);
            Assert.Equal(65, dto.MaxEntryAge);
        }

        [Fact]
        public void GetAll_WithEmptyCatalogue_Returns200WithEmptyArray()
        {
            // Arrange
            var controller = ControllerFor();

            // Act
            var result = controller.GetAll();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Empty(Assert.IsType<ProductDto[]>(ok.Value));
        }
    }
}
