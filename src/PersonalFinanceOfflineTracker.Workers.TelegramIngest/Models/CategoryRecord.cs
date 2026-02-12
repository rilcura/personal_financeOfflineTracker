namespace PersonalFinanceOfflineTracker.Workers.TelegramIngest.Models;

public sealed class CategoryRecord
{
    public required string Id { get; init; }
    public required string UserId { get; init; }
    public required string Name { get; init; }
    public required string NormalizedName { get; init; }
    public required string Source { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
