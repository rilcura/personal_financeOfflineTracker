namespace PersonalFinanceOfflineTracker.Apps.Maui.Models.Sync;

public sealed record LocalOutboxItem
{
    public string Id { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public string EntityId { get; init; } = string.Empty;
    public string Operation { get; init; } = string.Empty;
    public string PayloadJson { get; init; } = string.Empty;
    public int AttemptCount { get; init; }
    public DateTime NextAttemptAtUtc { get; init; }
    public string? LastError { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
    public string Status { get; init; } = string.Empty;
}
