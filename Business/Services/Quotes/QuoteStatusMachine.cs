using Business.Abstractions.Domain.Quotes;
using Business.Abstractions.Exceptions;

namespace Business.Services.Quotes
{
    /// <summary>
    /// Allowed quote status moves (docs/ASSUMPTIONS.md A-05). Accepted and Declined are final,
    /// and moving to the status a quote already has is rejected.
    /// </summary>
    public static class QuoteStatusMachine
    {
        private static readonly IReadOnlyDictionary<QuoteStatus, QuoteStatus[]> AllowedMoves =
            new Dictionary<QuoteStatus, QuoteStatus[]>
            {
                [QuoteStatus.Draft] = [QuoteStatus.SentToClient, QuoteStatus.Declined],
                [QuoteStatus.SentToClient] = [QuoteStatus.Accepted, QuoteStatus.Declined],
                [QuoteStatus.Accepted] = [],
                [QuoteStatus.Declined] = []
            };

        public static IReadOnlyList<QuoteStatus> AllowedTargets(QuoteStatus from) =>
            AllowedMoves.TryGetValue(from, out var targets) ? targets : [];

        public static bool CanMove(QuoteStatus from, QuoteStatus to) => AllowedTargets(from).Contains(to);

        /// <summary>Throws BadRequestException (400) when the move is not allowed.</summary>
        public static void EnsureCanMove(QuoteStatus from, QuoteStatus to)
        {
            if (CanMove(from, to))
            {
                return;
            }

            var allowed = AllowedTargets(from);
            var detail = allowed.Count == 0
                ? $"{from} is a final status"
                : $"allowed from {from}: {string.Join(", ", allowed)}";

            throw new BadRequestException(
                $"Cannot change status from {from} to {to}; {detail}",
                [new ApiError("Status", $"Transition {from} to {to} is not allowed")]);
        }
    }
}
