using PersonalFinanceOfflineTracker.Apps.Maui.Models.Ledger;
using PersonalFinanceOfflineTracker.Apps.Maui.Services.Sync;
using PersonalFinanceOfflineTracker.Sync.Models;

namespace PersonalFinanceOfflineTracker.Apps.Maui.Services.Ledger;

public sealed class LocalLedgerService : ILocalLedgerService
{
    private readonly ILocalLedgerStore _ledgerStore;
    private readonly ISyncOutboxService _outboxService;
    private readonly ISyncTokenStore _tokenStore;

    public LocalLedgerService(
        ILocalLedgerStore ledgerStore,
        ISyncOutboxService outboxService,
        ISyncTokenStore tokenStore)
    {
        _ledgerStore = ledgerStore;
        _outboxService = outboxService;
        _tokenStore = tokenStore;
    }

    public async Task<IReadOnlyList<LocalCategoryRecord>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var userId = await GetRequiredUserIdAsync(cancellationToken);
        return await _ledgerStore.ListCategoriesAsync(userId, includeDeleted: false, cancellationToken);
    }

    public async Task<IReadOnlyList<LocalTransactionRecord>> GetTransactionsAsync(CancellationToken cancellationToken = default)
    {
        var userId = await GetRequiredUserIdAsync(cancellationToken);
        return await _ledgerStore.ListTransactionsAsync(userId, includeDeleted: false, cancellationToken);
    }

    public async Task<LocalCategoryRecord> SaveCategoryAsync(string name, string? existingCategoryId = null, CancellationToken cancellationToken = default)
    {
        var userId = await GetRequiredUserIdAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Category name is required.", nameof(name));
        }

        var now = DateTime.UtcNow;
        var id = string.IsNullOrWhiteSpace(existingCategoryId) ? Guid.NewGuid().ToString() : existingCategoryId.Trim();
        var existing = await _ledgerStore.GetCategoryByIdAsync(id, cancellationToken);
        var createdAt = existing?.CreatedAtUtc ?? now;

        var category = new LocalCategoryRecord
        {
            Id = id,
            UserId = userId,
            Name = name.Trim(),
            NormalizedName = name.Trim().ToUpperInvariant(),
            CreatedAtUtc = createdAt,
            UpdatedAtUtc = now,
            IsDeleted = false,
            DeletedAtUtc = null,
            Source = "App"
        };

        await _ledgerStore.UpsertCategoryAsync(category, cancellationToken);

        var change = new SyncChangeDto
        {
            ChangeId = Guid.NewGuid().ToString(),
            EntityType = SyncEntityType.Category,
            Operation = SyncOperationType.Upsert,
            EntityId = category.Id,
            UpdatedAt = now,
            Category = new CategorySyncDto
            {
                Id = category.Id,
                UserId = category.UserId,
                Name = category.Name,
                NormalizedName = category.NormalizedName,
                CreatedAt = category.CreatedAtUtc,
                UpdatedAt = category.UpdatedAtUtc,
                IsDeleted = category.IsDeleted,
                DeletedAt = category.DeletedAtUtc,
                Source = category.Source
            }
        };

        await _outboxService.EnqueueChangeAsync(change, cancellationToken);
        return category;
    }

    public async Task DeleteCategoryAsync(string categoryId, CancellationToken cancellationToken = default)
    {
        var userId = await GetRequiredUserIdAsync(cancellationToken);
        var now = DateTime.UtcNow;

        await _ledgerStore.SoftDeleteCategoryAsync(categoryId, now, cancellationToken);

        var change = new SyncChangeDto
        {
            ChangeId = Guid.NewGuid().ToString(),
            EntityType = SyncEntityType.Category,
            Operation = SyncOperationType.Delete,
            EntityId = categoryId,
            UpdatedAt = now,
            DeletedAt = now,
            Category = new CategorySyncDto
            {
                Id = categoryId,
                UserId = userId,
                Name = string.Empty,
                NormalizedName = string.Empty,
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = true,
                DeletedAt = now,
                Source = "App"
            }
        };

        await _outboxService.EnqueueChangeAsync(change, cancellationToken);
    }

    public async Task<LocalTransactionRecord> SaveTransactionAsync(
        decimal amount,
        string description,
        DateOnly transactionDate,
        string? categoryId = null,
        string? existingTransactionId = null,
        CancellationToken cancellationToken = default)
    {
        var userId = await GetRequiredUserIdAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description is required.", nameof(description));
        }

        var now = DateTime.UtcNow;
        var id = string.IsNullOrWhiteSpace(existingTransactionId) ? Guid.NewGuid().ToString() : existingTransactionId.Trim();
        var existing = await _ledgerStore.GetTransactionByIdAsync(id, cancellationToken);
        var createdAt = existing?.CreatedAtUtc ?? now;

        var tx = new LocalTransactionRecord
        {
            Id = id,
            UserId = userId,
            CategoryId = string.IsNullOrWhiteSpace(categoryId) ? null : categoryId.Trim(),
            Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero),
            Description = description.Trim(),
            TransactionDate = transactionDate,
            CreatedAtUtc = createdAt,
            UpdatedAtUtc = now,
            IsDeleted = false,
            DeletedAtUtc = null,
            Source = "App"
        };

        await _ledgerStore.UpsertTransactionAsync(tx, cancellationToken);

        var change = new SyncChangeDto
        {
            ChangeId = Guid.NewGuid().ToString(),
            EntityType = SyncEntityType.Transaction,
            Operation = SyncOperationType.Upsert,
            EntityId = tx.Id,
            UpdatedAt = now,
            Transaction = new TransactionSyncDto
            {
                Id = tx.Id,
                UserId = tx.UserId,
                CategoryId = tx.CategoryId,
                Amount = tx.Amount,
                Description = tx.Description,
                TransactionDate = tx.TransactionDate,
                CreatedAt = tx.CreatedAtUtc,
                UpdatedAt = tx.UpdatedAtUtc,
                IsDeleted = tx.IsDeleted,
                DeletedAt = tx.DeletedAtUtc,
                Source = tx.Source
            }
        };

        await _outboxService.EnqueueChangeAsync(change, cancellationToken);
        return tx;
    }

    public async Task DeleteTransactionAsync(string transactionId, CancellationToken cancellationToken = default)
    {
        var userId = await GetRequiredUserIdAsync(cancellationToken);
        var now = DateTime.UtcNow;
        await _ledgerStore.SoftDeleteTransactionAsync(transactionId, now, cancellationToken);

        var change = new SyncChangeDto
        {
            ChangeId = Guid.NewGuid().ToString(),
            EntityType = SyncEntityType.Transaction,
            Operation = SyncOperationType.Delete,
            EntityId = transactionId,
            UpdatedAt = now,
            DeletedAt = now,
            Transaction = new TransactionSyncDto
            {
                Id = transactionId,
                UserId = userId,
                CategoryId = null,
                Amount = 0,
                Description = string.Empty,
                TransactionDate = DateOnly.FromDateTime(now),
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = true,
                DeletedAt = now,
                Source = "App"
            }
        };

        await _outboxService.EnqueueChangeAsync(change, cancellationToken);
    }

    private async Task<string> GetRequiredUserIdAsync(CancellationToken cancellationToken)
    {
        var userId = await _tokenStore.GetUserIdAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new InvalidOperationException("No active session. Please login in Sync Lab first.");
        }

        await _ledgerStore.InitializeAsync(cancellationToken);
        return userId;
    }
}
