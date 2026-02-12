using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalFinanceOfflineTracker.Domain.Models;

namespace PersonalFinanceOfflineTracker.Infrastructure.Persistence.Configurations;

internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasMaxLength(36).IsRequired();
        builder.Property(x => x.UserId).HasMaxLength(36).IsRequired();
        builder.Property(x => x.CategoryId).HasMaxLength(36).IsRequired(false);
        builder.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(50).IsRequired();
        builder.Property(x => x.TransactionDate).IsRequired();

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired();
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.Source).HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.UserId, x.TransactionDate });
        builder.HasIndex(x => new { x.UserId, x.UpdatedAt });
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
