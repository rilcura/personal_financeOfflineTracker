using Microsoft.EntityFrameworkCore;
using PersonalFinanceOfflineTracker.Api.Models.Auth;
using PersonalFinanceOfflineTracker.Domain.Models;
using PersonalFinanceOfflineTracker.Infrastructure.Persistence;

namespace PersonalFinanceOfflineTracker.Api.Services;

public static class DbBootstrapper
{
    public static async Task InitializeAsync(
        IServiceProvider services,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();

        await dbContext.Database.MigrateAsync(cancellationToken);
        await EnsureSeedUserAsync(dbContext, configuration, logger, cancellationToken);
    }

    private static async Task EnsureSeedUserAsync(
        FinanceDbContext dbContext,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var options = configuration.GetSection(SeedUserOptions.SectionName).Get<SeedUserOptions>() ?? new SeedUserOptions();
        var email = options.Email.Trim();

        var existing = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (existing is not null)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var seedUser = new User(
            id: Guid.NewGuid().ToString(),
            email: email,
            displayName: options.DisplayName.Trim(),
            passwordHash: PasswordHasher.Hash(options.Password),
            createdAtUtc: now);

        dbContext.Users.Add(seedUser);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seed user created for development email: {Email}", email);
    }
}
