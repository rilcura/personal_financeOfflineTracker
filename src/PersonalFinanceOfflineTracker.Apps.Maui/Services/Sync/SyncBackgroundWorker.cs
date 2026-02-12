using System.Text.Json;
using Microsoft.Extensions.Logging;
using PersonalFinanceOfflineTracker.Apps.Maui.Services.Ledger;
using PersonalFinanceOfflineTracker.Sync.Models;

namespace PersonalFinanceOfflineTracker.Apps.Maui.Services.Sync;

public sealed class SyncBackgroundWorker : ISyncBackgroundWorker
{
    private readonly ISyncOutboxStore _outboxStore;
    private readonly ILocalLedgerStore _ledgerStore;
    private readonly ISyncTokenStore _tokenStore;
    private readonly ISyncApiClient _syncApiClient;
    private readonly SyncClientOptions _options;
    private readonly ILogger<SyncBackgroundWorker> _logger;
    private readonly SemaphoreSlim _cycleGate = new(1, 1);

    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private bool _started;

    public SyncBackgroundWorker(
        ISyncOutboxStore outboxStore,
        ILocalLedgerStore ledgerStore,
        ISyncTokenStore tokenStore,
        ISyncApiClient syncApiClient,
        SyncClientOptions options,
        ILogger<SyncBackgroundWorker> logger)
    {
        _outboxStore = outboxStore;
        _ledgerStore = ledgerStore;
        _tokenStore = tokenStore;
        _syncApiClient = syncApiClient;
        _options = options;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_started)
        {
            return Task.CompletedTask;
        }

        _started = true;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _loopTask = Task.Run(() => RunLoopAsync(_cts.Token), CancellationToken.None);
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_cts is null)
        {
            return;
        }

        _cts.Cancel();
        if (_loopTask is not null)
        {
            try
            {
                await _loopTask;
            }
            catch (OperationCanceledException)
            {
            }
        }

        _cts.Dispose();
        _cts = null;
    }

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        await _outboxStore.InitializeAsync(cancellationToken);
        await _ledgerStore.InitializeAsync(cancellationToken);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.PollIntervalSeconds));
        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            try
            {
                await RunCycleAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Sync background loop cycle failed.");
            }
        }
    }

    private async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        if (!await _cycleGate.WaitAsync(0, cancellationToken))
        {
            return;
        }

        try
        {
            var accessToken = await _tokenStore.GetAccessTokenAsync(cancellationToken);
            var userId = await _tokenStore.GetUserIdAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(userId))
            {
                return;
            }

            var now = DateTime.UtcNow;
            var leased = await _outboxStore.LeasePendingBatchAsync(_options.PushBatchSize, now, cancellationToken);
            if (leased.Count > 0)
            {
                IReadOnlyList<SyncChangeDto> changes;
                try
                {
                    changes = leased.Select(x => JsonSerializer.Deserialize<SyncChangeDto>(x.PayloadJson))
                        .Where(x => x is not null)
                        .Cast<SyncChangeDto>()
                        .ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize outbox payload.");
                    foreach (var item in leased)
                    {
                        await _outboxStore.MarkFailedAsync(item.Id, "Invalid payload", DateTime.UtcNow, cancellationToken);
                    }

                    return;
                }

                try
                {
                    var response = await _syncApiClient.PushAsync(accessToken, userId, changes, cancellationToken);

                    var acceptedChangeIds = response.AcceptedItems.Select(x => x.ChangeId).ToHashSet(StringComparer.Ordinal);
                    var leasedByChangeId = leased.ToDictionary(
                        x => JsonSerializer.Deserialize<SyncChangeDto>(x.PayloadJson)?.ChangeId ?? string.Empty,
                        x => x,
                        StringComparer.Ordinal);

                    var succeededIds = acceptedChangeIds
                        .Where(leasedByChangeId.ContainsKey)
                        .Select(changeId => leasedByChangeId[changeId].Id)
                        .ToList();

                    if (succeededIds.Count > 0)
                    {
                        await _outboxStore.MarkSucceededAsync(succeededIds, cancellationToken);
                    }

                    var rejected = response.RejectedItems.Where(x => leasedByChangeId.ContainsKey(x.ChangeId));
                    foreach (var item in rejected)
                    {
                        await _outboxStore.MarkFailedAsync(
                            leasedByChangeId[item.ChangeId].Id,
                            item.Reason,
                            DateTime.UtcNow,
                            cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Sync push failed.");
                    foreach (var item in leased)
                    {
                        await _outboxStore.MarkFailedAsync(item.Id, ex.Message, DateTime.UtcNow, cancellationToken);
                    }
                }
            }

            await PullAndApplyAsync(accessToken, userId, cancellationToken);
        }
        finally
        {
            _cycleGate.Release();
        }
    }

    private async Task PullAndApplyAsync(string accessToken, string userId, CancellationToken cancellationToken)
    {
        try
        {
            var cursor = await _ledgerStore.GetSyncCursorAsync(userId, cancellationToken);
            var pull = await _syncApiClient.PullAsync(accessToken, cursor, cancellationToken);
            await _ledgerStore.ApplySyncChangesAsync(userId, pull.Changes, pull.NextCursor, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sync pull/apply failed.");
        }
    }
}
