using System.Net;
using System.Net.Http.Json;

namespace PersonalFinanceOfflineTracker.Apps.WebDashboard.Services;

public sealed class CategoriesApiClient(HttpClient httpClient, AuthSession authSession)
{
    public async Task<IReadOnlyList<CategoryDto>> ListAsync(CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync("/api/categories/", cancellationToken);
        await EnsureAuthorizedAsync(response, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<CategoryDto>>(cancellationToken: cancellationToken) ?? new List<CategoryDto>();
    }

    public async Task<string?> CreateAsync(string name, CancellationToken cancellationToken)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(
                "/api/categories/",
                new CreateCategoryRequestDto { Name = name },
                cancellationToken);

            return await ReadErrorOrNullAsync(response, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return "Cannot reach API. Start PersonalFinanceOfflineTracker.Api and retry.";
        }
    }

    public async Task<string?> UpdateAsync(string id, string name, CancellationToken cancellationToken)
    {
        try
        {
            var response = await httpClient.PutAsJsonAsync(
                $"/api/categories/{Uri.EscapeDataString(id)}",
                new UpdateCategoryRequestDto { Name = name },
                cancellationToken);

            return await ReadErrorOrNullAsync(response, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return "Cannot reach API. Start PersonalFinanceOfflineTracker.Api and retry.";
        }
    }

    public async Task<string?> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        try
        {
            var response = await httpClient.DeleteAsync($"/api/categories/{Uri.EscapeDataString(id)}", cancellationToken);
            return await ReadErrorOrNullAsync(response, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return "Cannot reach API. Start PersonalFinanceOfflineTracker.Api and retry.";
        }
    }

    private async Task<string?> ReadErrorOrNullAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await EnsureAuthorizedAsync(response, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return null;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(body)
            ? $"Request failed with status {(int)response.StatusCode}."
            : body;
    }

    private Task EnsureAuthorizedAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            authSession.Clear();
        }

        return Task.CompletedTask;
    }
}
