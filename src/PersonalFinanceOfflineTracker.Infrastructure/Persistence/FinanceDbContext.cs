using Microsoft.EntityFrameworkCore;
using PersonalFinanceOfflineTracker.Domain.Models;
using PersonalFinanceOfflineTracker.Infrastructure.Persistence.Configurations;

namespace PersonalFinanceOfflineTracker.Infrastructure.Persistence;

public sealed class FinanceDbContext : DbContext
{
    public FinanceDbContext(DbContextOptions<FinanceDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Transaction> Transactions => Set<Transaction>();

    public DbSet<ExternalIdentity> ExternalIdentities => Set<ExternalIdentity>();

    public DbSet<IngestedMessage> IngestedMessages => Set<IngestedMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new CategoryConfiguration());
        modelBuilder.ApplyConfiguration(new TransactionConfiguration());
        modelBuilder.ApplyConfiguration(new ExternalIdentityConfiguration());
        modelBuilder.ApplyConfiguration(new IngestedMessageConfiguration());
    }
}
