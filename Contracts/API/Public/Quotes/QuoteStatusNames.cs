namespace Contracts.API.Public.Quotes
{
    /// <summary>
    /// Status values as they travel over the API. Kept as strings so the contract stays readable for the UI.
    /// </summary>
    public static class QuoteStatusNames
    {
        public const string Draft = "Draft";
        public const string SentToClient = "SentToClient";
        public const string Accepted = "Accepted";
        public const string Declined = "Declined";

        public static readonly IReadOnlyList<string> All = [Draft, SentToClient, Accepted, Declined];

        public static bool IsValid(string? value) => value is not null && All.Contains(value);
    }
}
