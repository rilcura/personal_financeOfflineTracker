using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalFinanceOfflineTracker.Domain.Models;

namespace PersonalFinanceOfflineTracker.Infrastructure.Persistence.Configurations;

internal sealed class IngestedMessageConfiguration : IEntityTypeConfiguration<IngestedMessage>
{
    public void Configure(EntityTypeBuilder<IngestedMessage> builder)
    {
        builder.ToTable("IngestedMessages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasMaxLength(36).IsRequired();
        builder.Property(x => x.Provider).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.TelegramUpdateId).IsRequired();
        builder.Property(x => x.TelegramMessageId).IsRequired(false);
        builder.Property(x => x.RawText).HasMaxLength(4000).IsRequired(false);
        builder.Property(x => x.NormalizedCommand).HasMaxLength(2000).IsRequired(false);
        builder.Property(x => x.ParseStatus).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.ErrorCode).HasMaxLength(100).IsRequired(false);
        builder.Property(x => x.UserId).HasMaxLength(36).IsRequired(false);
        builder.Property(x => x.CreatedTransactionId).HasMaxLength(36).IsRequired(false);
        builder.Property(x => x.ReceivedAt).IsRequired();
        builder.Property(x => x.ProcessedAt).IsRequired(false);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired();
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.Source).HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Transaction>()
            .WithMany()
            .HasForeignKey(x => x.CreatedTransactionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.Provider, x.TelegramUpdateId }).IsUnique();
        builder.HasIndex(x => x.ReceivedAt);
        builder.HasIndex(x => x.ParseStatus);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
