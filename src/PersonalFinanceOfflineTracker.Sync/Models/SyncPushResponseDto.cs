namespace PersonalFinanceOfflineTracker.Sync.Models;

public sealed record SyncPushResponseDto
{
    public IReadOnlyList<SyncPushAcceptedItemDto> AcceptedItems { get; init; } = Array.Empty<SyncPushAcceptedItemDto>();
    public IReadOnlyList<SyncPushRejectedItemDto> RejectedItems { get; init; } = Array.Empty<SyncPushRejectedItemDto>();
    public IReadOnlyList<SyncConflictItemDto> Conflicts { get; init; } = Array.Empty<SyncConflictItemDto>();
    public string ServerCursor { get; init; } = string.Empty;
}
