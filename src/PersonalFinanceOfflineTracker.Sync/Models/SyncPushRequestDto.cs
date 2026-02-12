namespace PersonalFinanceOfflineTracker.Sync.Models;

public sealed record SyncPushRequestDto
{
    public string UserId { get; init; } = string.Empty;
    public IReadOnlyList<SyncChangeDto> Changes { get; init; } = Array.Empty<SyncChangeDto>();
}
