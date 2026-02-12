using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalFinanceOfflineTracker.Domain.Models;

namespace PersonalFinanceOfflineTracker.Infrastructure.Persistence.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasMaxLength(36).IsRequired();
        builder.Property(x => x.UserId).HasMaxLength(36).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(30).IsRequired();
        builder.Property(x => x.NormalizedName).HasMaxLength(30).IsRequired();

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired();
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.Source).HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.UserId, x.NormalizedName })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = 0");

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
