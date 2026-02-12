using PersonalFinanceOfflineTracker.Apps.Maui.Models.Ledger;
using PersonalFinanceOfflineTracker.Sync.Models;

namespace PersonalFinanceOfflineTracker.Apps.Maui.Services.Ledger;

public interface ILocalLedgerStore
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<LocalCategoryRecord?> GetCategoryByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<LocalTransactionRecord?> GetTransactionByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LocalCategoryRecord>> ListCategoriesAsync(string userId, bool includeDeleted = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LocalTransactionRecord>> ListTransactionsAsync(string userId, bool includeDeleted = false, CancellationToken cancellationToken = default);
    Task UpsertCategoryAsync(LocalCategoryRecord category, CancellationToken cancellationToken = default);
    Task UpsertTransactionAsync(LocalTransactionRecord transaction, CancellationToken cancellationToken = default);
    Task SoftDeleteCategoryAsync(string id, DateTime deletedAtUtc, CancellationToken cancellationToken = default);
    Task SoftDeleteTransactionAsync(string id, DateTime deletedAtUtc, CancellationToken cancellationToken = default);
    Task<string?> GetSyncCursorAsync(string userId, CancellationToken cancellationToken = default);
    Task SetSyncCursorAsync(string userId, string cursor, CancellationToken cancellationToken = default);
    Task ApplySyncChangesAsync(string userId, IReadOnlyList<SyncEntityStateDto> changes, string nextCursor, CancellationToken cancellationToken = default);
}
