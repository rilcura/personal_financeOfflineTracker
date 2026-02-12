namespace PersonalFinanceOfflineTracker.Sync.Models;

public sealed record SyncPushRejectedItemDto
{
    public string ChangeId { get; init; } = string.Empty;
    public SyncEntityType EntityType { get; init; }
    public string EntityId { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
}
