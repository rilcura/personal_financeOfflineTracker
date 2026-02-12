using System.Net.Http.Headers;
using System.Net.Http.Json;
using PersonalFinanceOfflineTracker.Sync.Models;

namespace PersonalFinanceOfflineTracker.Apps.Maui.Services.Sync;

public sealed class SyncApiClient : ISyncApiClient
{
    private readonly HttpClient _httpClient;

    public SyncApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<SyncPushResponseDto> PushAsync(
        string accessToken,
        string userId,
        IReadOnlyList<SyncChangeDto> changes,
        CancellationToken cancellationToken = default)
    {
        var request = new SyncPushRequestDto
        {
            UserId = userId,
            Changes = changes,
        };

        using var message = new HttpRequestMessage(HttpMethod.Post, "/api/sync/push")
        {
            Content = JsonContent.Create(request),
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<SyncPushResponseDto>(cancellationToken: cancellationToken);
        return payload ?? new SyncPushResponseDto();
    }
}
