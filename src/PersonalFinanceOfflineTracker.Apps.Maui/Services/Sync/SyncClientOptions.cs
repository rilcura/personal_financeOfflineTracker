namespace PersonalFinanceOfflineTracker.Apps.Maui.Services.Sync;

public sealed class SyncClientOptions
{
    public string ApiBaseUrl { get; init; } = "http://localhost:5089";
    public int PushBatchSize { get; init; } = 25;
    public int PollIntervalSeconds { get; init; } = 30;
}
