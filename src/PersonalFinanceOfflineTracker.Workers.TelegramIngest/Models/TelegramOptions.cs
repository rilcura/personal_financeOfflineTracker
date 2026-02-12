namespace PersonalFinanceOfflineTracker.Workers.TelegramIngest.Models;

public sealed class TelegramOptions
{
    public const string SectionName = "Telegram";

    public string BotToken { get; init; } = string.Empty;
    public string ApiBaseUrl { get; init; } = "https://api.telegram.org";
    public int LongPollTimeoutSeconds { get; init; } = 20;
}
