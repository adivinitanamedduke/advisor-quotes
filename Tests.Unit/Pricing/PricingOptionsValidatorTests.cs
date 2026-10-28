using API.Middleware.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pricing;

namespace Tests.Unit.Pricing
{
    public class PricingOptionsValidatorTests
    {
        private readonly PricingOptionsValidator _validator = new();

        private static PricingOptions ValidOptions() => PricingTestData.Options().Value;

        private static string ShippedAppSettingsPath()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AdvisorQuotes.sln")))
            {
                directory = directory.Parent;
            }

            Assert.NotNull(directory);
            return Path.Combine(directory.FullName, "API", "appsettings.json");
        }

        private static IOptions<PricingOptions> ResolveThroughModule(IConfiguration configuration) =>
            new ServiceCollection()
                .AddPricingModule(configuration)
                .BuildServiceProvider()
                .GetRequiredService<IOptions<PricingOptions>>();

        [Fact]
        public void Validate_ShippedAppSettings_Succeeds()
        {
            // Arrange
            var configuration = new ConfigurationBuilder().AddJsonFile(ShippedAppSettingsPath()).Build();

            // Act
            var options = ResolveThroughModule(configuration).Value;

            // Assert
            Assert.Equal(["TERM_LIFE", "FUNERAL", "DISABILITY"], options.Products.Select(p => p.Code));
        }

        [Fact]
        public void Validate_ValidOptions_Succeeds()
        {
            // Act
            var result = _validator.Validate(null, ValidOptions());

            // Assert
            Assert.True(result.Succeeded);
        }

        public static TheoryData<string, Action<PricingOptions>, string> Breaches => new()
        {
            { "no products", o => o.Products.Clear(), "at least one product" },
            { "smoker loading zero", o => o.SmokerLoading = 0m, "SmokerLoading" },
            { "smoker loading negative", o => o.SmokerLoading = -1m, "SmokerLoading" },
            { "blank code", o => o.Products[0].Code = " ", "Code is required" },
            { "blank name", o => o.Products[0].Name = "", "Name is required" },
            { "base rate zero", o => o.Products[0].BaseRatePer1000 = 0m, "BaseRatePer1000" },
            { "base rate negative", o => o.Products[0].BaseRatePer1000 = -0.5m, "BaseRatePer1000" },
            { "cover min above max", o => o.Products[0].MinCoverAmount = o.Products[0].MaxCoverAmount + 1, "MinCoverAmount" },
            { "term min above max", o => o.Products[0].MinTermYears = o.Products[0].MaxTermYears + 1, "MinTermYears" },
            { "entry age min above max", o => o.Products[0].MinEntryAge = o.Products[0].MaxEntryAge + 1, "MinEntryAge" },
            {
                "duplicate code in another case",
                o =>
                {
                    var copy = PricingTestData.TermLife();
                    copy.Code = "term_life";
                    o.Products.Add(copy);
                },
                "duplicate code"
            }
        };

        [Theory]
        [MemberData(nameof(Breaches))]
        public void Validate_InvalidOptions_FailsWithReason(string scenario, Action<PricingOptions> breach, string expectedReason)
        {
            // Arrange: scenario only labels the case in test output
            _ = scenario;
            var options = ValidOptions();
            breach(options);

            // Act
            var result = _validator.Validate(null, options);

            // Assert
            Assert.True(result.Failed);
            Assert.Contains(expectedReason, result.FailureMessage);
        }

        [Fact]
        public void AddPricingModule_WithInvalidConfiguration_ThrowsOnResolve()
        {
            // Arrange: one product with a blank name and a zero base rate
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Pricing:SmokerLoading"] = "1.5",
                    ["Pricing:Products:0:Code"] = "TERM_LIFE",
                    ["Pricing:Products:0:BaseRatePer1000"] = "0"
                })
                .Build();

            // Act
            var exception = Assert.Throws<OptionsValidationException>(() => ResolveThroughModule(configuration).Value);

            // Assert
            Assert.Contains(exception.Failures, f => f.Contains("Name is required"));
            Assert.Contains(exception.Failures, f => f.Contains("BaseRatePer1000"));
        }
    }
}
