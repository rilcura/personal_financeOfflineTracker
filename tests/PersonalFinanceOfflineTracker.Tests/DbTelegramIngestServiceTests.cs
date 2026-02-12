using Microsoft.Extensions.Logging.Abstractions;
using PersonalFinanceOfflineTracker.Workers.TelegramIngest.Models;
using PersonalFinanceOfflineTracker.Workers.TelegramIngest.Services;

namespace PersonalFinanceOfflineTracker.Tests;

public sealed class DbTelegramIngestServiceTests
{
    [Fact]
    public async Task IngestAsync_SameUpdateTwice_IsIdempotent()
    {
        var (dbContext, connection) = TestDbContextFactory.CreateSqliteContext();
        try
        {
            var parser = new TelegramCommandParser();
            var service = new DbTelegramIngestService(parser, dbContext, NullLogger<DbTelegramIngestService>.Instance);

            var update = new TelegramUpdate(
                UpdateId: 2001,
                MessageId: 1,
                FromUserId: 555,
                ChatId: 777,
                Text: "/add 120 groceries food",
                ReceivedAtUtc: DateTimeOffset.UtcNow);

            var first = await service.IngestAsync(update, CancellationToken.None);
            var second = await service.IngestAsync(update, CancellationToken.None);

            Assert.Equal(IngestStatus.Parsed, first.Status);
            Assert.Equal(IngestStatus.Duplicate, second.Status);
            Assert.Equal(1, dbContext.Transactions.Count());
        }
        finally
        {
            dbContext.Dispose();
            connection.Dispose();
        }
    }
}
