using PersonalFinanceOfflineTracker.Domain.Models;
using PersonalFinanceOfflineTracker.Sync.Models;
using PersonalFinanceOfflineTracker.Sync.Services;

namespace PersonalFinanceOfflineTracker.Tests;

public sealed class DbSyncServiceTests
{
    [Fact]
    public async Task PushAsync_OlderVersion_IsRejectedByLww()
    {
        var (dbContext, connection) = TestDbContextFactory.CreateSqliteContext();
        try
        {
            var userId = Guid.NewGuid().ToString();
            dbContext.Users.Add(new User(
                id: userId,
                email: "lww@test.local",
                displayName: "Lww User",
                passwordHash: "hash",
                createdAtUtc: DateTime.UtcNow));
            await dbContext.SaveChangesAsync();

            var service = new DbSyncService(dbContext);
            var entityId = Guid.NewGuid().ToString();
            var created = DateTime.UtcNow.AddMinutes(-5);
            var newer = DateTime.UtcNow;
            var older = created.AddMinutes(1);

            var first = await service.PushAsync(new SyncPushRequestDto
            {
                UserId = userId,
                Changes =
                [
                    new SyncChangeDto
                    {
                        ChangeId = Guid.NewGuid().ToString(),
                        EntityType = SyncEntityType.Transaction,
                        Operation = SyncOperationType.Upsert,
                        EntityId = entityId,
                        UpdatedAt = newer,
                        Transaction = new TransactionSyncDto
                        {
                            Id = entityId,
                            UserId = userId,
                            Amount = 100,
                            Description = "new",
                            TransactionDate = DateOnly.FromDateTime(DateTime.UtcNow),
                            CreatedAt = created,
                            UpdatedAt = newer,
                            IsDeleted = false,
                            Source = "App"
                        }
                    }
                ]
            });

            var second = await service.PushAsync(new SyncPushRequestDto
            {
                UserId = userId,
                Changes =
                [
                    new SyncChangeDto
                    {
                        ChangeId = Guid.NewGuid().ToString(),
                        EntityType = SyncEntityType.Transaction,
                        Operation = SyncOperationType.Upsert,
                        EntityId = entityId,
                        UpdatedAt = older,
                        Transaction = new TransactionSyncDto
                        {
                            Id = entityId,
                            UserId = userId,
                            Amount = 10,
                            Description = "old",
                            TransactionDate = DateOnly.FromDateTime(DateTime.UtcNow),
                            CreatedAt = created,
                            UpdatedAt = older,
                            IsDeleted = false,
                            Source = "App"
                        }
                    }
                ]
            });

            Assert.Single(first.AcceptedItems);
            Assert.Single(second.RejectedItems);
            Assert.Single(second.Conflicts);
        }
        finally
        {
            dbContext.Dispose();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task PullAsync_AfterDelete_ReturnsTombstone()
    {
        var (dbContext, connection) = TestDbContextFactory.CreateSqliteContext();
        try
        {
            var userId = Guid.NewGuid().ToString();
            dbContext.Users.Add(new User(
                id: userId,
                email: "tombstone@test.local",
                displayName: "Tomb User",
                passwordHash: "hash",
                createdAtUtc: DateTime.UtcNow));
            await dbContext.SaveChangesAsync();

            var service = new DbSyncService(dbContext);
            var categoryId = Guid.NewGuid().ToString();
            var now = DateTime.UtcNow;

            await service.PushAsync(new SyncPushRequestDto
            {
                UserId = userId,
                Changes =
                [
                    new SyncChangeDto
                    {
                        ChangeId = Guid.NewGuid().ToString(),
                        EntityType = SyncEntityType.Category,
                        Operation = SyncOperationType.Upsert,
                        EntityId = categoryId,
                        UpdatedAt = now,
                        Category = new CategorySyncDto
                        {
                            Id = categoryId,
                            UserId = userId,
                            Name = "Food",
                            NormalizedName = "FOOD",
                            CreatedAt = now,
                            UpdatedAt = now,
                            IsDeleted = false,
                            Source = "App"
                        }
                    }
                ]
            });

            await service.PushAsync(new SyncPushRequestDto
            {
                UserId = userId,
                Changes =
                [
                    new SyncChangeDto
                    {
                        ChangeId = Guid.NewGuid().ToString(),
                        EntityType = SyncEntityType.Category,
                        Operation = SyncOperationType.Delete,
                        EntityId = categoryId,
                        UpdatedAt = now.AddMinutes(1),
                        DeletedAt = now.AddMinutes(1),
                    }
                ]
            });

            var pull = await service.PullAsync(userId, cursor: null);
            var state = pull.Changes.Single(x => x.EntityId == categoryId && x.EntityType == SyncEntityType.Category);
            Assert.True(state.IsDeleted);
            Assert.NotNull(state.DeletedAt);
        }
        finally
        {
            dbContext.Dispose();
            connection.Dispose();
        }
    }
}
