using PersonalFinanceOfflineTracker.Sync.Models;

namespace PersonalFinanceOfflineTracker.Apps.Maui.Services.Sync;

public interface ISyncApiClient
{
    Task<SyncPushResponseDto> PushAsync(
        string accessToken,
        string userId,
        IReadOnlyList<SyncChangeDto> changes,
        CancellationToken cancellationToken = default);
}
