using PersonalFinanceOfflineTracker.Workers.TelegramIngest.Models;

namespace PersonalFinanceOfflineTracker.Workers.TelegramIngest.Abstractions;

public interface ITelegramCommandParser
{
    CommandParseResult Parse(string? rawText, DateOnly serverLocalToday);
}
