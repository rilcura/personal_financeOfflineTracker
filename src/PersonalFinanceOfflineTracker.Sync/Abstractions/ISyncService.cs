using PersonalFinanceOfflineTracker.Sync.Models;

namespace PersonalFinanceOfflineTracker.Sync.Abstractions;

public interface ISyncService
{
    Task<SyncPushResponseDto> PushAsync(SyncPushRequestDto request, CancellationToken cancellationToken = default);
    Task<SyncPullResponseDto> PullAsync(string userId, string? cursor, CancellationToken cancellationToken = default);
}
