using PersonalFinanceOfflineTracker.Workers.TelegramIngest.Models;

namespace PersonalFinanceOfflineTracker.Workers.TelegramIngest.Abstractions;

public interface ITelegramUpdateSource
{
    Task<IReadOnlyList<TelegramUpdate>> GetUpdatesAsync(long? offsetExclusive, CancellationToken cancellationToken);
}
