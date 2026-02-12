namespace PersonalFinanceOfflineTracker.Workers.TelegramIngest.Models;

public sealed class ExternalIdentityRecord
{
    public required string Id { get; init; }
    public required string UserId { get; init; }
    public required string Provider { get; init; }
    public required string ProviderUserId { get; init; }
    public string? ProviderChatId { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }
}
