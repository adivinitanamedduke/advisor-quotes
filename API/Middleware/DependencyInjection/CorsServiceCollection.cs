namespace API.Middleware.DependencyInjection
{
    public static class CorsServiceCollection
    {
        public const string UiPolicy = "Ui";
        public const string UiOriginsKey = "Cors:UiOrigins";

        private static readonly string[] DefaultUiOrigins = ["http://localhost:5173"];

        // The UI sends the adviser and idempotency headers and reads the replay, trace and location headers back
        private static readonly string[] AllowedHeaders = ["Content-Type", "Accept", "X-Adviser-Id", "Idempotency-Key"];
        private static readonly string[] ExposedHeaders = ["Idempotent-Replayed", "X-Trace-Id", "Location"];
        private static readonly string[] AllowedMethods = ["GET", "POST", "PUT"];

        /// <summary>
        /// Lets the browser UI call the API from its own origin. Origins come from Cors:UiOrigins
        /// (array, or Cors__UiOrigins__0 in the environment), defaulting to the Vite dev server.
        /// </summary>
        public static IServiceCollection AddUiCors(this IServiceCollection services, IConfiguration configuration)
        {
            var origins = configuration.GetSection(UiOriginsKey).Get<string[]>() is { Length: > 0 } configured
                ? configured
                : DefaultUiOrigins;

            services.AddCors(options => options.AddPolicy(UiPolicy, policy => policy
                .WithOrigins(origins)
                .WithHeaders(AllowedHeaders)
                .WithMethods(AllowedMethods)
                .WithExposedHeaders(ExposedHeaders)));

            return services;
        }
    }
}
