namespace PersonalFinanceOfflineTracker.Workers.TelegramIngest.Models;

public sealed record TelegramUpdate(
    long UpdateId,
    long? MessageId,
    long FromUserId,
    long? ChatId,
    string? Text,
    DateTimeOffset ReceivedAtUtc
);
