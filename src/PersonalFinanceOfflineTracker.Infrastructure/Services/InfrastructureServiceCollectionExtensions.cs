using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PersonalFinanceOfflineTracker.Infrastructure.Persistence;

namespace PersonalFinanceOfflineTracker.Infrastructure.Services;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureSqlite(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<FinanceDbContext>(options =>
            options.UseSqlite(connectionString));

        return services;
    }
}
