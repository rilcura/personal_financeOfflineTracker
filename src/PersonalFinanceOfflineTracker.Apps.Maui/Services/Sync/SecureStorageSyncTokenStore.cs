namespace PersonalFinanceOfflineTracker.Apps.Maui.Services.Sync;

public sealed class SecureStorageSyncTokenStore : ISyncTokenStore
{
    private const string AccessTokenKey = "sync_access_token";
    private const string UserIdKey = "sync_user_id";

    public Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        return SecureStorage.Default.GetAsync(AccessTokenKey);
    }

    public Task<string?> GetUserIdAsync(CancellationToken cancellationToken = default)
    {
        return SecureStorage.Default.GetAsync(UserIdKey);
    }

    public async Task SetSessionAsync(string accessToken, string userId, CancellationToken cancellationToken = default)
    {
        await SecureStorage.Default.SetAsync(AccessTokenKey, accessToken);
        await SecureStorage.Default.SetAsync(UserIdKey, userId);
    }
}
