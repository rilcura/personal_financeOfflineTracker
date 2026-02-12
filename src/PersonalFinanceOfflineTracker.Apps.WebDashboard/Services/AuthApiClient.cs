using System.Net;
using System.Net.Http.Json;

namespace PersonalFinanceOfflineTracker.Apps.WebDashboard.Services;

public sealed class AuthApiClient(HttpClient httpClient, AuthSession authSession)
{
    public async Task<(bool IsSuccess, string ErrorMessage)> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        try
        {
            var request = new LoginRequestDto
            {
                Email = email,
                Password = password,
            };

            var response = await httpClient.PostAsJsonAsync("/api/auth/login", request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return (false, await BuildErrorAsync(response, cancellationToken));
            }

            var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponseDto>(cancellationToken: cancellationToken);
            if (loginResponse is null || string.IsNullOrWhiteSpace(loginResponse.AccessToken))
            {
                return (false, "Login response was empty.");
            }

            authSession.SetLogin(loginResponse);
            return (true, string.Empty);
        }
        catch (HttpRequestException)
        {
            return (false, "Cannot reach API. Start PersonalFinanceOfflineTracker.Api and retry.");
        }
    }

    public async Task<bool> ValidateSessionAsync(CancellationToken cancellationToken)
    {
        if (!authSession.IsAuthenticated)
        {
            return false;
        }

        var response = await httpClient.GetAsync("/api/auth/me", cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            authSession.Clear();
        }

        return false;
    }

    public void Logout()
    {
        authSession.Clear();
    }

    private static async Task<string> BuildErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(body))
        {
            return body;
        }

        return $"Request failed with status {(int)response.StatusCode}.";
    }
}
