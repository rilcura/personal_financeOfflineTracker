using System.Collections.Concurrent;
using PersonalFinanceOfflineTracker.Workers.TelegramIngest.Abstractions;
using PersonalFinanceOfflineTracker.Workers.TelegramIngest.Models;

namespace PersonalFinanceOfflineTracker.Workers.TelegramIngest.Services;

public sealed class InMemoryTelegramUpdateSource : ITelegramUpdateSource
{
    private readonly ConcurrentQueue<TelegramUpdate> _queue = new();

    public InMemoryTelegramUpdateSource()
    {
        _queue.Enqueue(new TelegramUpdate(1001, 501, 90001, 70001, "/add 120 coffee", DateTimeOffset.UtcNow));
        _queue.Enqueue(new TelegramUpdate(1002, 502, 90001, 70001, "/add 120.50 \"coffee beans\" groceries 2026-02-11", DateTimeOffset.UtcNow));
        _queue.Enqueue(new TelegramUpdate(1002, 502, 90001, 70001, "/add 120.50 \"coffee beans\" groceries 2026-02-11", DateTimeOffset.UtcNow));
    }

    public Task<IReadOnlyList<TelegramUpdate>> GetUpdatesAsync(long? offsetExclusive, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        List<TelegramUpdate> updates = [];
        while (_queue.TryDequeue(out TelegramUpdate? update))
        {
            if (offsetExclusive.HasValue && update.UpdateId <= offsetExclusive.Value)
            {
                continue;
            }

            updates.Add(update);
        }

        return Task.FromResult<IReadOnlyList<TelegramUpdate>>(updates);
    }
}
