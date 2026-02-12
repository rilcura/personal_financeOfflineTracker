using System.Globalization;
using PersonalFinanceOfflineTracker.Workers.TelegramIngest.Abstractions;
using PersonalFinanceOfflineTracker.Workers.TelegramIngest.Models;

namespace PersonalFinanceOfflineTracker.Workers.TelegramIngest.Services;

public sealed class TelegramIngestService(
    ITelegramCommandParser parser,
    InMemoryTelegramIngestStore store,
    ILogger<TelegramIngestService> logger) : ITelegramIngestService
{
    public Task<IngestOutcome> IngestAsync(TelegramUpdate update, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (store.HasIngestedUpdate(update.UpdateId))
        {
            return Task.FromResult(new IngestOutcome(
                IngestStatus.Duplicate,
                TelegramContract.Response.DuplicateUpdate,
                TelegramContract.ErrorCode.DuplicateUpdate));
        }

        DateTimeOffset nowUtc = DateTimeOffset.UtcNow;
        IngestedMessageRecord ingestRecord = new()
        {
            Id = Guid.NewGuid().ToString("N"),
            Provider = "Telegram",
            TelegramUpdateId = update.UpdateId,
            TelegramMessageId = update.MessageId,
            RawText = update.Text,
            ParseStatus = IngestStatus.Failed,
            ReceivedAt = update.ReceivedAtUtc,
            CreatedAt = nowUtc,
            UpdatedAt = nowUtc
        };

        if (!store.TryAddIngestRecord(ingestRecord))
        {
            return Task.FromResult(new IngestOutcome(
                IngestStatus.Duplicate,
                TelegramContract.Response.DuplicateUpdate,
                TelegramContract.ErrorCode.DuplicateUpdate));
        }

        try
        {
            DateOnly todayLocal = DateOnly.FromDateTime(DateTime.Now);
            CommandParseResult parseResult = parser.Parse(update.Text, todayLocal);

            if (parseResult.CommandKind == ParsedCommandKind.Help && parseResult.IsSuccess)
            {
                ingestRecord.ParseStatus = IngestStatus.Parsed;
                ingestRecord.NormalizedCommand = parseResult.NormalizedCommand;
                ingestRecord.ProcessedAt = DateTimeOffset.UtcNow;
                ingestRecord.UpdatedAt = ingestRecord.ProcessedAt.Value;
                store.UpdateIngestRecord(ingestRecord);

                return Task.FromResult(new IngestOutcome(IngestStatus.Parsed, parseResult.ResponseMessage));
            }

            if (!parseResult.IsSuccess || parseResult.AddCommand is null)
            {
                ingestRecord.ParseStatus = IngestStatus.Rejected;
                ingestRecord.ErrorCode = parseResult.ErrorCode;
                ingestRecord.ProcessedAt = DateTimeOffset.UtcNow;
                ingestRecord.UpdatedAt = ingestRecord.ProcessedAt.Value;
                store.UpdateIngestRecord(ingestRecord);

                return Task.FromResult(new IngestOutcome(
                    IngestStatus.Rejected,
                    parseResult.ResponseMessage,
                    parseResult.ErrorCode));
            }

            string userId = store.ResolveOrCreateUser(update.FromUserId, update.ChatId, DateTimeOffset.UtcNow);
            CategoryRecord category = store.ResolveOrCreateCategory(userId, parseResult.AddCommand.Category, DateTimeOffset.UtcNow);
            string transactionId = store.CreateTransaction(userId, category, parseResult.AddCommand, DateTimeOffset.UtcNow);

            ingestRecord.ParseStatus = IngestStatus.Parsed;
            ingestRecord.NormalizedCommand = parseResult.NormalizedCommand;
            ingestRecord.UserId = userId;
            ingestRecord.CreatedTransactionId = transactionId;
            ingestRecord.ProcessedAt = DateTimeOffset.UtcNow;
            ingestRecord.UpdatedAt = ingestRecord.ProcessedAt.Value;
            store.UpdateIngestRecord(ingestRecord);

            string successMessage = BuildSuccessMessage(parseResult.AddCommand);
            return Task.FromResult(new IngestOutcome(
                IngestStatus.Parsed,
                successMessage,
                null,
                transactionId));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed processing telegram update {UpdateId}", update.UpdateId);

            ingestRecord.ParseStatus = IngestStatus.Failed;
            ingestRecord.ErrorCode = TelegramContract.ErrorCode.Internal;
            ingestRecord.ProcessedAt = DateTimeOffset.UtcNow;
            ingestRecord.UpdatedAt = ingestRecord.ProcessedAt.Value;
            store.UpdateIngestRecord(ingestRecord);

            return Task.FromResult(new IngestOutcome(
                IngestStatus.Failed,
                TelegramContract.Response.Internal,
                TelegramContract.ErrorCode.Internal));
        }
    }

    private static string BuildSuccessMessage(ParsedAddCommandDto add)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"Saved: {add.Amount:0.00} {add.Description} [{add.Category}] on {add.TransactionDate:yyyy-MM-dd}");
    }
}
