using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using PersonalFinanceOfflineTracker.Workers.TelegramIngest.Abstractions;
using PersonalFinanceOfflineTracker.Workers.TelegramIngest.Models;

namespace PersonalFinanceOfflineTracker.Workers.TelegramIngest.Services;

public sealed class TelegramBotApiUpdateSource(
    HttpClient httpClient,
    IOptions<TelegramOptions> options,
    ILogger<TelegramBotApiUpdateSource> logger) : ITelegramUpdateSource
{
    private readonly TelegramOptions _options = options.Value;

    public async Task<IReadOnlyList<TelegramUpdate>> GetUpdatesAsync(long? offsetExclusive, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.BotToken))
        {
            return [];
        }

        var offset = offsetExclusive.HasValue ? offsetExclusive.Value + 1 : 0;
        var url = $"/bot{_options.BotToken}/getUpdates?timeout={_options.LongPollTimeoutSeconds}&offset={offset}";

        try
        {
            var response = await httpClient.GetFromJsonAsync<TelegramGetUpdatesEnvelope>(url, cancellationToken);
            if (response is null || !response.Ok || response.Result is null || response.Result.Count == 0)
            {
                return [];
            }

            var now = DateTimeOffset.UtcNow;
            return response.Result
                .Where(x => x.Message is not null)
                .Select(x => new TelegramUpdate(
                    x.UpdateId,
                    x.Message!.MessageId,
                    x.Message.From?.Id ?? 0,
                    x.Message.Chat?.Id,
                    x.Message.Text,
                    now))
                .Where(x => x.FromUserId > 0)
                .OrderBy(x => x.UpdateId)
                .ToArray();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed calling Telegram getUpdates.");
            return [];
        }
    }

    private sealed record TelegramGetUpdatesEnvelope(bool Ok, List<TelegramUpdateItem>? Result);
    private sealed record TelegramUpdateItem(long UpdateId, TelegramMessageItem? Message);
    private sealed record TelegramMessageItem(long MessageId, TelegramUserItem? From, TelegramChatItem? Chat, string? Text);
    private sealed record TelegramUserItem(long Id);
    private sealed record TelegramChatItem(long Id);
}
