namespace PersonalFinanceOfflineTracker.Sync.Models;

public sealed record SyncChangeDto
{
    public string ChangeId { get; init; } = string.Empty;
    public SyncEntityType EntityType { get; init; }
    public SyncOperationType Operation { get; init; }
    public string EntityId { get; init; } = string.Empty;
    public DateTime UpdatedAt { get; init; }
    public DateTime? DeletedAt { get; init; }
    public TransactionSyncDto? Transaction { get; init; }
    public CategorySyncDto? Category { get; init; }
}
