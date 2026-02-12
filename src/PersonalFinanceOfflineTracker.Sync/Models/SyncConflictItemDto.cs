namespace PersonalFinanceOfflineTracker.Sync.Models;

public sealed record SyncConflictItemDto
{
    public string ChangeId { get; init; } = string.Empty;
    public SyncEntityType EntityType { get; init; }
    public string EntityId { get; init; } = string.Empty;
    public string Winner { get; init; } = string.Empty;
    public SyncEntityStateDto? ServerVersion { get; init; }
}
