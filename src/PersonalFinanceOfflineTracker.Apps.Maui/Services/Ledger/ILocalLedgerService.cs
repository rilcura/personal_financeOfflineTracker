using PersonalFinanceOfflineTracker.Apps.Maui.Models.Ledger;

namespace PersonalFinanceOfflineTracker.Apps.Maui.Services.Ledger;

public interface ILocalLedgerService
{
    Task<IReadOnlyList<LocalCategoryRecord>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LocalTransactionRecord>> GetTransactionsAsync(CancellationToken cancellationToken = default);
    Task<LocalCategoryRecord> SaveCategoryAsync(string name, string? existingCategoryId = null, CancellationToken cancellationToken = default);
    Task DeleteCategoryAsync(string categoryId, CancellationToken cancellationToken = default);
    Task<LocalTransactionRecord> SaveTransactionAsync(
        decimal amount,
        string description,
        DateOnly transactionDate,
        string? categoryId = null,
        string? existingTransactionId = null,
        CancellationToken cancellationToken = default);
    Task DeleteTransactionAsync(string transactionId, CancellationToken cancellationToken = default);
}
