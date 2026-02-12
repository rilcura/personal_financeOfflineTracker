using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalFinanceOfflineTracker.Domain.Models;

namespace PersonalFinanceOfflineTracker.Infrastructure.Persistence.Configurations;

internal sealed class ExternalIdentityConfiguration : IEntityTypeConfiguration<ExternalIdentity>
{
    public void Configure(EntityTypeBuilder<ExternalIdentity> builder)
    {
        builder.ToTable("ExternalIdentities");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasMaxLength(36).IsRequired();
        builder.Property(x => x.UserId).HasMaxLength(36).IsRequired();
        builder.Property(x => x.Provider).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.ProviderUserId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ProviderChatId).HasMaxLength(100).IsRequired(false);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.Property(x => x.IsDeleted).IsRequired();
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.Source).HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.Provider, x.ProviderUserId }).IsUnique();
        builder.HasIndex(x => new { x.Provider, x.ProviderChatId });
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
