using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Contracts.API.Public.Quotes.Requests;

namespace Business.Services.Quotes
{
    /// <summary>
    /// SHA-256 (lower-case hex) of a canonical JSON form of a create request, used to spot an identical
    /// submission from the same adviser inside the duplicate window when no Idempotency-Key is sent.
    /// Cosmetic differences (name case and spacing, product code case, notes padding, 1000 vs 1000.00)
    /// produce the same fingerprint.
    /// </summary>
    public static partial class QuoteFingerprint
    {
        public static string Compute(string adviserId, CreateQuoteRequest request)
        {
            // Property order is fixed by this anonymous type, so the JSON is stable across runs
            var canonical = new
            {
                adviser = adviserId.Trim(),
                client = new
                {
                    name = NormaliseName(request.Client.FullName),
                    dateOfBirth = request.Client.DateOfBirth.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    smoker = request.Client.IsSmoker
                },
                product = new
                {
                    code = request.Product.Code.Trim().ToUpperInvariant(),
                    cover = request.Product.CoverAmount.ToString("0.############", CultureInfo.InvariantCulture),
                    term = request.Product.TermYears
                },
                notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
            };

            var json = JsonSerializer.Serialize(canonical);
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
            return Convert.ToHexStringLower(hash);
        }

        private static string NormaliseName(string name) =>
            Whitespace().Replace(name.Trim(), " ").ToUpperInvariant();

        [GeneratedRegex(@"\s+")]
        private static partial Regex Whitespace();
    }
}
