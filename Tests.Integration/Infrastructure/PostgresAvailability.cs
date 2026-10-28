namespace Tests.Integration.Infrastructure
{
    /// <summary>
    /// Decides where the integration tests get PostgreSQL from (docs/ASSUMPTIONS.md A-10):
    /// TEST_POSTGRES_CONNECTION when set, else Testcontainers when a Docker endpoint exists, else skip.
    /// </summary>
    public static class PostgresAvailability
    {
        public const string ConnectionVariable = "TEST_POSTGRES_CONNECTION";

        public static string? ConnectionString =>
            Environment.GetEnvironmentVariable(ConnectionVariable) is { Length: > 0 } value ? value : null;

        public static bool UseTestcontainers => ConnectionString is null && DockerLooksAvailable();

        public static string? SkipReason =>
            ConnectionString is not null || UseTestcontainers
                ? null
                : $"No PostgreSQL for integration tests: set {ConnectionVariable} to a disposable database, or start Docker for Testcontainers";

        // A cheap probe, so a machine without Docker skips instead of waiting on a container start to time out
        private static bool DockerLooksAvailable()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOCKER_HOST")))
            {
                return true;
            }

            return OperatingSystem.IsWindows()
                ? File.Exists(@"\\.\pipe\docker_engine")
                : File.Exists("/var/run/docker.sock");
        }
    }

    /// <summary>A fact that is skipped, with the reason, when no PostgreSQL is available.</summary>
    public sealed class PostgresFactAttribute : FactAttribute
    {
        public PostgresFactAttribute()
        {
            Skip = PostgresAvailability.SkipReason;
        }
    }
}
