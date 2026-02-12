using System.Net;
using System.Net.Http.Json;

namespace PersonalFinanceOfflineTracker.Apps.WebDashboard.Services;

public sealed class TransactionsApiClient(HttpClient httpClient, AuthSession authSession)
{
    public async Task<IReadOnlyList<TransactionDto>> ListAsync(string? month, CancellationToken cancellationToken)
    {
        var path = "/api/transactions/";
        if (!string.IsNullOrWhiteSpace(month))
        {
            path += $"?month={Uri.EscapeDataString(month)}";
        }

        var response = await httpClient.GetAsync(path, cancellationToken);
        await EnsureAuthorizedAsync(response, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<TransactionDto>>(cancellationToken: cancellationToken)
            ?? new List<TransactionDto>();
    }

    public async Task<string?> CreateAsync(CreateTransactionRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("/api/transactions/", request, cancellationToken);
            return await ReadErrorOrNullAsync(response, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return "Cannot reach API. Start PersonalFinanceOfflineTracker.Api and retry.";
        }
    }

    public async Task<string?> UpdateAsync(string id, UpdateTransactionRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await httpClient.PutAsJsonAsync($"/api/transactions/{Uri.EscapeDataString(id)}", request, cancellationToken);
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
            var response = await httpClient.DeleteAsync($"/api/transactions/{Uri.EscapeDataString(id)}", cancellationToken);
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
