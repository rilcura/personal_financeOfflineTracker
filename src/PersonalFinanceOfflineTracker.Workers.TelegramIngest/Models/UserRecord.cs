namespace PersonalFinanceOfflineTracker.Workers.TelegramIngest.Models;

public sealed class UserRecord
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required string Source { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
