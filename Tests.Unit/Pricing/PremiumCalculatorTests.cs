using System.Diagnostics;
using Pricing;
using static Pricing.PremiumCalculator;

namespace Tests.Unit.Pricing
{
    public class PremiumCalculatorTests
    {
        private readonly PremiumCalculator _calculator = PricingTestData.Calculator();

        [Fact]
        public void Calculate_BaseCase_MatchesWorkedExample()
        {
            // Arrange: 1 000 000 cover, age 35, non-smoker, 20 years
            // 1 000 units × 0.85 × 1.00 (age 30-39) × 1 (non-smoker) × 1.10 (20 years) = 935.00
            var request = PricingTestData.Request();

            // Act
            var result = _calculator.Calculate(request);

            // Assert
            Assert.Equal(935.00m, result.MonthlyPremium);
            Assert.Equal("ZAR", result.Currency);
            Assert.Equal("v1", result.PricingVersion);
        }

        [Fact]
        public void Calculate_BaseCase_ReturnsEveryFactorUsed()
        {
            // Act
            var result = _calculator.Calculate(PricingTestData.Request());

            // Assert
            Assert.Equal(1000m, result.Factors[FactorNames.CoverUnits]);
            Assert.Equal(0.85m, result.Factors[FactorNames.BaseRatePer1000]);
            Assert.Equal(35m, result.Factors[FactorNames.AgeAtQuote]);
            Assert.Equal(1.00m, result.Factors[FactorNames.AgeFactor]);
            Assert.Equal(1m, result.Factors[FactorNames.SmokerFactor]);
            Assert.Equal(1.10m, result.Factors[FactorNames.TermFactor]);
            Assert.Equal(6, result.Factors.Count);
        }

        [Fact]
        public void Calculate_Smoker_AppliesSmokerLoading()
        {
            // Arrange: 935.00 × 1.5
            var request = PricingTestData.Request(isSmoker: true);

            // Act
            var result = _calculator.Calculate(request);

            // Assert
            Assert.Equal(1402.50m, result.MonthlyPremium);
            Assert.Equal(1.5m, result.Factors[FactorNames.SmokerFactor]);
        }

        [Theory]
        [InlineData(18, 0.80)]
        [InlineData(29, 0.80)]
        [InlineData(30, 1.00)]
        [InlineData(39, 1.00)]
        [InlineData(40, 1.50)]
        [InlineData(49, 1.50)]
        [InlineData(50, 2.50)]
        [InlineData(59, 2.50)]
        [InlineData(60, 4.00)]
        [InlineData(65, 4.00)]
        public void Calculate_AtAgeBandEdge_UsesThatBandsFactor(int age, double expectedFactor)
        {
            // Arrange
            var request = PricingTestData.Request(age: age, termYears: ReferenceTermYears);

            // Act
            var result = _calculator.Calculate(request);

            // Assert: term factor is 1 at the reference term, so premium = 1 000 × 0.85 × ageFactor
            Assert.Equal((decimal)expectedFactor, result.Factors[FactorNames.AgeFactor]);
            Assert.Equal(Math.Round(850m * (decimal)expectedFactor, 2), result.MonthlyPremium);
        }

        [Fact]
        public void Calculate_DayBeforeBirthday_UsesCompletedYears()
        {
            // Arrange: turns 30 the day after the quote date, so still 29
            var dateOfBirth = PricingTestData.BornAged(30).AddDays(1);
            var request = PricingTestData.Request() with { DateOfBirth = dateOfBirth };

            // Act
            var result = _calculator.Calculate(request);

            // Assert
            Assert.Equal(29m, result.Factors[FactorNames.AgeAtQuote]);
            Assert.Equal(0.80m, result.Factors[FactorNames.AgeFactor]);
        }

        [Theory]
        [InlineData(5, 0.95)]
        [InlineData(10, 1.00)]
        [InlineData(20, 1.10)]
        [InlineData(30, 1.20)]
        public void Calculate_Term_AppliesTermFactor(int termYears, double expectedFactor)
        {
            // Arrange
            var request = PricingTestData.Request(termYears: termYears);

            // Act
            var result = _calculator.Calculate(request);

            // Assert
            Assert.Equal((decimal)expectedFactor, result.Factors[FactorNames.TermFactor]);
            Assert.Equal(Math.Round(850m * (decimal)expectedFactor, 2), result.MonthlyPremium);
        }

        [Fact]
        public void Calculate_AtMidpoint_RoundsAwayFromZero()
        {
            // Arrange: 100 units × 0.00125 = 0.125 exactly; banker's rounding would give 0.12
            var product = PricingTestData.TermLife();
            product.BaseRatePer1000 = 0.00125m;
            var calculator = PricingTestData.Calculator(product);
            var request = PricingTestData.Request(coverAmount: 100_000m, termYears: ReferenceTermYears);

            // Act
            var result = calculator.Calculate(request);

            // Assert
            Assert.Equal(0.13m, result.MonthlyPremium);
        }

        [Theory]
        [InlineData(100_000, 5, 18)]
        [InlineData(20_000_000, 30, 65)]
        public void Calculate_AtInclusiveLimits_Prices(double cover, int termYears, int age)
        {
            // Arrange
            var request = PricingTestData.Request(coverAmount: (decimal)cover, termYears: termYears, age: age);

            // Act
            var result = _calculator.Calculate(request);

            // Assert
            Assert.True(result.MonthlyPremium > 0);
        }

        [Fact]
        public void Calculate_UnknownProduct_ThrowsPricingRuleException()
        {
            // Arrange
            var request = PricingTestData.Request(productCode: "WHOLE_LIFE");

            // Act
            var exception = Assert.Throws<PricingRuleException>(() => _calculator.Calculate(request));

            // Assert
            Assert.Contains("WHOLE_LIFE", exception.Message);
        }

        public static TheoryData<string, PremiumRequest> RuleBreaches => new()
        {
            { "age below minimum", PricingTestData.Request(age: 17) },
            { "age above maximum", PricingTestData.Request(age: 66) },
            { "born after quote date", PricingTestData.Request() with { DateOfBirth = PricingTestData.QuoteDate.AddDays(1) } },
            { "term below minimum", PricingTestData.Request(termYears: 4) },
            { "term above maximum", PricingTestData.Request(termYears: 31) },
            { "cover below minimum", PricingTestData.Request(coverAmount: 99_999.99m) },
            { "cover above maximum", PricingTestData.Request(coverAmount: 20_000_000.01m) }
        };

        [Theory]
        [MemberData(nameof(RuleBreaches))]
        public void Calculate_OutsideProductLimits_ThrowsPricingRuleException(string scenario, PremiumRequest request)
        {
            // Arrange: scenario only labels the case in test output
            _ = scenario;

            // Act
            var act = () => _calculator.Calculate(request);

            // Assert
            Assert.Throws<PricingRuleException>(act);
        }

        [Fact]
        public void Calculate_StartsPricingSpanWithProductTag()
        {
            // Arrange
            var spans = new List<Activity>();
            using var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == "AdvisorQuotes",
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = activity =>
                {
                    lock (spans)
                    {
                        spans.Add(activity);
                    }
                }
            };
            ActivitySource.AddActivityListener(listener);

            // Act
            _calculator.Calculate(PricingTestData.Request(productCode: "term_life"));

            // Assert
            lock (spans)
            {
                Assert.Contains(spans, s =>
                    s.OperationName == "pricing.calculate"
                    && (string?)s.GetTagItem("product.code") == "term_life"
                    && s.GetTagItem("quote.premium") is decimal);
            }
        }
    }
}
