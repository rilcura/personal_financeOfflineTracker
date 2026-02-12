using PersonalFinanceOfflineTracker.Workers.TelegramIngest.Models;

namespace PersonalFinanceOfflineTracker.Workers.TelegramIngest.Abstractions;

public interface ITelegramIngestService
{
    Task<IngestOutcome> IngestAsync(TelegramUpdate update, CancellationToken cancellationToken);
}
