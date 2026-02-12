namespace PersonalFinanceOfflineTracker.Apps.WebDashboard.Services;

public sealed class AuthSession
{
    public event Action? Changed;

    public string AccessToken { get; private set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;

    public bool IsAuthenticated =>
        !string.IsNullOrWhiteSpace(AccessToken)
        && ExpiresAtUtc > DateTime.UtcNow;

    public void SetLogin(LoginResponseDto response)
    {
        AccessToken = response.AccessToken;
        ExpiresAtUtc = response.ExpiresAtUtc;
        UserId = response.UserId;
        Email = response.Email;
        DisplayName = response.DisplayName;
        Changed?.Invoke();
    }

    public void Clear()
    {
        AccessToken = string.Empty;
        ExpiresAtUtc = default;
        UserId = string.Empty;
        Email = string.Empty;
        DisplayName = string.Empty;
        Changed?.Invoke();
    }
}
