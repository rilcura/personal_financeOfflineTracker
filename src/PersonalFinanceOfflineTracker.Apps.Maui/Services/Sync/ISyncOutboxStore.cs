using PersonalFinanceOfflineTracker.Apps.Maui.Models.Sync;

namespace PersonalFinanceOfflineTracker.Apps.Maui.Services.Sync;

public interface ISyncOutboxStore
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task EnqueueAsync(LocalOutboxItem item, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LocalOutboxItem>> LeasePendingBatchAsync(int maxItems, DateTime nowUtc, CancellationToken cancellationToken = default);
    Task MarkSucceededAsync(IEnumerable<string> itemIds, CancellationToken cancellationToken = default);
    Task MarkFailedAsync(string itemId, string error, DateTime nowUtc, CancellationToken cancellationToken = default);
}
