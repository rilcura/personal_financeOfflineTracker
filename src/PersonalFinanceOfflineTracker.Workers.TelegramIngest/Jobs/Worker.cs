using PersonalFinanceOfflineTracker.Workers.TelegramIngest.Abstractions;
using PersonalFinanceOfflineTracker.Workers.TelegramIngest.Models;

namespace PersonalFinanceOfflineTracker.Workers.TelegramIngest.Jobs;

public sealed class Worker(
    ITelegramUpdateSource updateSource,
    IServiceScopeFactory scopeFactory,
    ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        long? lastSeenUpdateId = null;

        while (!stoppingToken.IsCancellationRequested)
        {
            IReadOnlyList<TelegramUpdate> updates = await updateSource.GetUpdatesAsync(lastSeenUpdateId, stoppingToken);
            if (updates.Count == 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                continue;
            }

            foreach (TelegramUpdate update in updates.OrderBy(x => x.UpdateId))
            {
                using var scope = scopeFactory.CreateScope();
                var ingestService = scope.ServiceProvider.GetRequiredService<ITelegramIngestService>();
                IngestOutcome outcome = await ingestService.IngestAsync(update, stoppingToken);
                logger.LogInformation(
                    "update_id={UpdateId} status={Status} code={Code} message=\"{Message}\"",
                    update.UpdateId,
                    outcome.Status,
                    outcome.ErrorCode ?? "-",
                    outcome.ResponseMessage);

                lastSeenUpdateId = Math.Max(lastSeenUpdateId ?? update.UpdateId, update.UpdateId);
            }
        }
    }
}
