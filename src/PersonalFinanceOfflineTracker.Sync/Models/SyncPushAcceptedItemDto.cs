namespace PersonalFinanceOfflineTracker.Sync.Models;

public sealed record SyncPushAcceptedItemDto
{
    public string ChangeId { get; init; } = string.Empty;
    public SyncEntityType EntityType { get; init; }
    public string EntityId { get; init; } = string.Empty;
}
