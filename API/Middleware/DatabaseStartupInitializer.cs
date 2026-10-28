using Data.SQL.EF;
using Microsoft.EntityFrameworkCore;

namespace API.Middleware
{
    public static class DatabaseStartupInitializer
    {
        public const string MigrateOnStartupKey = "Database:MigrateOnStartup";

        /// <summary>
        /// Applies pending EF migrations before the app listens when Database:MigrateOnStartup is true
        /// (on for local runs). Other environments run `dotnet ef database update` or a migration bundle instead.
        /// </summary>
        public static async Task MigrateIfEnabledAsync(this WebApplication app)
        {
            if (!app.Configuration.GetValue<bool>(MigrateOnStartupKey))
            {
                return;
            }

            await using var scope = app.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<QuotesDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<QuotesDbContext>>();

            var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
            logger.LogInformation("Applying {Count} pending migration(s)", pending.Count);
            await db.Database.MigrateAsync();
        }
    }
}
