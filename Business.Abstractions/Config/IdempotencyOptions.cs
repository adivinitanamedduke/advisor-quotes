namespace Business.Abstractions.Config
{
    /// <summary>
    /// Bound from the "Idempotency" configuration section.
    /// </summary>
    public class IdempotencyOptions
    {
        public const string SectionName = "Idempotency";

        /// <summary>How long an identical submission without an Idempotency-Key is treated as a duplicate.</summary>
        public int WindowMinutes { get; set; } = 10;
    }
}
