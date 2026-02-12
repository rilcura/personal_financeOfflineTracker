namespace PersonalFinanceOfflineTracker.Apps.Maui.Models.Ledger;

public sealed record LocalTransactionRecord
{
    public string Id { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string? CategoryId { get; init; }
    public decimal Amount { get; init; }
    public string Description { get; init; } = string.Empty;
    public DateOnly TransactionDate { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
    public bool IsDeleted { get; init; }
    public DateTime? DeletedAtUtc { get; init; }
    public string Source { get; init; } = "App";
}
