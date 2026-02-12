namespace PersonalFinanceOfflineTracker.Apps.Maui.Services.Sync;

public interface ISyncTokenStore
{
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
    Task<string?> GetUserIdAsync(CancellationToken cancellationToken = default);
    Task SetSessionAsync(string accessToken, string userId, CancellationToken cancellationToken = default);
}
