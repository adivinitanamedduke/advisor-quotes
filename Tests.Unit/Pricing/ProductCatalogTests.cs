using Pricing;

namespace Tests.Unit.Pricing
{
    public class ProductCatalogTests
    {
        private readonly ProductCatalog _catalog = new(PricingTestData.Options());

        [Theory]
        [InlineData("TERM_LIFE")]
        [InlineData("term_life")]
        [InlineData("Term_Life")]
        [InlineData("  term_life ")]
        public void Find_WithCodeInAnyCase_ReturnsProduct(string code)
        {
            // Act
            var product = _catalog.Find(code);

            // Assert
            Assert.NotNull(product);
            Assert.Equal("TERM_LIFE", product.Code);
        }

        [Theory]
        [InlineData("WHOLE_LIFE")]
        [InlineData("")]
        [InlineData("   ")]
        public void Find_WithUnknownOrBlankCode_ReturnsNull(string code)
        {
            // Act
            var product = _catalog.Find(code);

            // Assert
            Assert.Null(product);
        }

        [Fact]
        public void GetAll_ReturnsConfiguredProductsInOrder()
        {
            // Arrange
            var funeral = PricingTestData.TermLife();
            funeral.Code = "FUNERAL";
            var catalog = new ProductCatalog(PricingTestData.Options(PricingTestData.TermLife(), funeral));

            // Act
            var products = catalog.GetAll();

            // Assert
            Assert.Equal(["TERM_LIFE", "FUNERAL"], products.Select(p => p.Code));
        }
    }
}
