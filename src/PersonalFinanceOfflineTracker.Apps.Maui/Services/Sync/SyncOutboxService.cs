using System.Text.Json;
using PersonalFinanceOfflineTracker.Apps.Maui.Models.Sync;
using PersonalFinanceOfflineTracker.Sync.Models;

namespace PersonalFinanceOfflineTracker.Apps.Maui.Services.Sync;

public sealed class SyncOutboxService : ISyncOutboxService
{
    private readonly ISyncOutboxStore _store;

    public SyncOutboxService(ISyncOutboxStore store)
    {
        _store = store;
    }

    public async Task EnqueueChangeAsync(SyncChangeDto change, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(change);

        var now = DateTime.UtcNow;
        var item = new LocalOutboxItem
        {
            Id = Guid.NewGuid().ToString(),
            EntityType = change.EntityType.ToString(),
            EntityId = change.EntityId,
            Operation = change.Operation.ToString(),
            PayloadJson = JsonSerializer.Serialize(change),
            AttemptCount = 0,
            NextAttemptAtUtc = now,
            LastError = null,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Status = OutboxStatus.Pending,
        };

        await _store.EnqueueAsync(item, cancellationToken);
    }
}
