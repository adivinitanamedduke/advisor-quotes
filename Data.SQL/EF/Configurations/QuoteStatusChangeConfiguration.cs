using Business.Abstractions.Domain.Quotes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.SQL.EF.Configurations
{
    public class QuoteStatusChangeConfiguration : IEntityTypeConfiguration<QuoteStatusChange>
    {
        public void Configure(EntityTypeBuilder<QuoteStatusChange> builder)
        {
            builder.ToTable("quote_status_changes");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.FromStatus).HasConversion<string>().HasMaxLength(20);
            builder.Property(e => e.ToStatus).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(e => e.Reason).HasMaxLength(500);
            builder.Property(e => e.ChangedBy).HasMaxLength(100).IsRequired();
            builder.Property(e => e.ChangedAt).IsRequired();

            builder.HasIndex(e => new { e.QuoteId, e.ChangedAt });
        }
    }
}
