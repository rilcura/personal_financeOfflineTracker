namespace PersonalFinanceOfflineTracker.Workers.TelegramIngest.Models;

public sealed class TransactionRecord
{
    public required string Id { get; init; }
    public required string UserId { get; init; }
    public required string CategoryId { get; init; }
    public required decimal Amount { get; init; }
    public required string Description { get; init; }
    public required DateOnly TransactionDate { get; init; }
    public required string Source { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
