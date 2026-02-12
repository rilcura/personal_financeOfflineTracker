namespace PersonalFinanceOfflineTracker.Sync.Models;

public sealed record SyncEntityStateDto
{
    public SyncEntityType EntityType { get; init; }
    public string EntityId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public DateTime UpdatedAt { get; init; }
    public bool IsDeleted { get; init; }
    public DateTime? DeletedAt { get; init; }
    public TransactionSyncDto? Transaction { get; init; }
    public CategorySyncDto? Category { get; init; }
}
