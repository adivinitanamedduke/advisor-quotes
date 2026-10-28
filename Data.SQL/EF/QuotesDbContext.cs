using Business.Abstractions.Domain.Quotes;
using Microsoft.EntityFrameworkCore;

namespace Data.SQL.EF
{
    public class QuotesDbContext(DbContextOptions<QuotesDbContext> options) : DbContext(options)
    {
        public const string ReferenceSequence = "quote_reference_seq";

        public DbSet<Quote> Quotes { get; set; } = null!;
        public DbSet<QuoteStatusChange> QuoteStatusChanges { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasSequence<long>(ReferenceSequence).StartsAt(1);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(QuotesDbContext).Assembly);
        }
    }
}
