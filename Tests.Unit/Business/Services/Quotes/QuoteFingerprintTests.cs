using Business.Services.Quotes;
using Contracts.API.Public.Quotes.Requests;

namespace Tests.Unit.Business.Services.Quotes
{
    public class QuoteFingerprintTests
    {
        private const string Adviser = "adviser-1";

        private static CreateQuoteRequest Request(
            string name = "Thandi Mokoena",
            string code = "TERM_LIFE",
            decimal cover = 1_000_000m,
            string? notes = "Annual review",
            bool smoker = false) => new()
        {
            Client = new QuoteClientRequest { FullName = name, DateOfBirth = new DateOnly(1985, 4, 12), IsSmoker = smoker },
            Product = new QuoteProductRequest { Code = code, CoverAmount = cover, TermYears = 20 },
            Notes = notes
        };

        [Fact]
        public void Compute_ReturnsLowerCaseSha256Hex()
        {
            // Act
            var fingerprint = QuoteFingerprint.Compute(Adviser, Request());

            // Assert
            Assert.Equal(64, fingerprint.Length);
            Assert.Matches("^[0-9a-f]{64}$", fingerprint);
        }

        [Fact]
        public void Compute_WithSameInput_IsStable()
        {
            // Act
            var first = QuoteFingerprint.Compute(Adviser, Request());
            var second = QuoteFingerprint.Compute(Adviser, Request());

            // Assert
            Assert.Equal(first, second);
        }

        [Fact]
        public void Compute_WithCosmeticDifferences_ReturnsSameFingerprint()
        {
            // Arrange
            var original = Request();
            var cosmetic = Request(name: "  thandi   MOKOENA ", code: " term_life", cover: 1_000_000.00m, notes: "  Annual review  ");

            // Act
            var a = QuoteFingerprint.Compute(Adviser, original);
            var b = QuoteFingerprint.Compute(" " + Adviser + " ", cosmetic);

            // Assert
            Assert.Equal(a, b);
        }

        [Fact]
        public void Compute_WithEmptyOrWhitespaceNotes_MatchesNullNotes()
        {
            // Act
            var nullNotes = QuoteFingerprint.Compute(Adviser, Request(notes: null));
            var blankNotes = QuoteFingerprint.Compute(Adviser, Request(notes: "   "));

            // Assert
            Assert.Equal(nullNotes, blankNotes);
        }

        public static TheoryData<CreateQuoteRequest> MaterialChanges => new()
        {
            Request(name: "Thandi Mokoena-Dlamini"),
            Request(code: "FUNERAL"),
            Request(cover: 1_000_001m),
            Request(notes: "Different note"),
            Request(smoker: true)
        };

        [Theory]
        [MemberData(nameof(MaterialChanges))]
        public void Compute_WithMaterialChange_ReturnsDifferentFingerprint(CreateQuoteRequest changed)
        {
            // Act
            var original = QuoteFingerprint.Compute(Adviser, Request());
            var other = QuoteFingerprint.Compute(Adviser, changed);

            // Assert
            Assert.NotEqual(original, other);
        }

        [Fact]
        public void Compute_ForDifferentAdviser_ReturnsDifferentFingerprint()
        {
            // Act
            var a = QuoteFingerprint.Compute("adviser-1", Request());
            var b = QuoteFingerprint.Compute("adviser-2", Request());

            // Assert
            Assert.NotEqual(a, b);
        }
    }
}
