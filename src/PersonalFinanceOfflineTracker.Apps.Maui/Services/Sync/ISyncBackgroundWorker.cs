namespace PersonalFinanceOfflineTracker.Apps.Maui.Services.Sync;

public interface ISyncBackgroundWorker : IAsyncDisposable
{
    Task StartAsync(CancellationToken cancellationToken = default);
}
