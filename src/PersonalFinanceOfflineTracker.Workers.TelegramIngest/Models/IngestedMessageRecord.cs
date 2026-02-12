namespace PersonalFinanceOfflineTracker.Workers.TelegramIngest.Models;

public sealed class IngestedMessageRecord
{
    public required string Id { get; init; }
    public required long TelegramUpdateId { get; init; }
    public required string Provider { get; init; }
    public long? TelegramMessageId { get; init; }
    public string? RawText { get; init; }
    public string? NormalizedCommand { get; set; }
    public IngestStatus ParseStatus { get; set; }
    public string? ErrorCode { get; set; }
    public string? UserId { get; set; }
    public string? CreatedTransactionId { get; set; }
    public DateTimeOffset ReceivedAt { get; init; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }
}
