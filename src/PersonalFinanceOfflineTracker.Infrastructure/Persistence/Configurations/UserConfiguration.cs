using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalFinanceOfflineTracker.Domain.Models;

namespace PersonalFinanceOfflineTracker.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasMaxLength(36).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(256).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired();
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.Source).HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.HasIndex(x => x.Email).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
