using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PersonalFinanceOfflineTracker.Infrastructure.Persistence;

namespace PersonalFinanceOfflineTracker.Tests;

internal static class TestDbContextFactory
{
    public static (FinanceDbContext DbContext, SqliteConnection Connection) CreateSqliteContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseSqlite(connection)
            .Options;

        var dbContext = new FinanceDbContext(options);
        dbContext.Database.EnsureCreated();
        return (dbContext, connection);
    }
}
