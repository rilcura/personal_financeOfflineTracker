namespace PersonalFinanceOfflineTracker.Apps.Maui.Models.Ledger;

public sealed record LocalCategoryRecord
{
    public string Id { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string NormalizedName { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
    public bool IsDeleted { get; init; }
    public DateTime? DeletedAtUtc { get; init; }
    public string Source { get; init; } = "App";
}
