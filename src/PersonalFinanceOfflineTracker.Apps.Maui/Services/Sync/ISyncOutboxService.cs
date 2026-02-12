using PersonalFinanceOfflineTracker.Sync.Models;

namespace PersonalFinanceOfflineTracker.Apps.Maui.Services.Sync;

public interface ISyncOutboxService
{
    Task EnqueueChangeAsync(SyncChangeDto change, CancellationToken cancellationToken = default);
}
