using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Data.SQL.EF
{
    /// <summary>
    /// Used only by `dotnet ef` at design time. Reads ConnectionStrings__Quotes, else a local default.
    /// Creating a migration does not connect to the database.
    /// </summary>
    public class QuotesDbContextFactory : IDesignTimeDbContextFactory<QuotesDbContext>
    {
        public QuotesDbContext CreateDbContext(string[] args)
        {
            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Quotes")
                ?? "Host=localhost;Port=5432;Database=advisor_quotes;Username=postgres;Password=postgres";

            var options = new DbContextOptionsBuilder<QuotesDbContext>();
            DataSqlServiceCollectionExtensions.ConfigureNpgsql(options, connectionString);
            return new QuotesDbContext(options.Options);
        }
    }
}
