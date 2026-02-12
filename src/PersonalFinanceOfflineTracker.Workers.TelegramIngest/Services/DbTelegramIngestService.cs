using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PersonalFinanceOfflineTracker.Domain.Models;
using PersonalFinanceOfflineTracker.Infrastructure.Persistence;
using PersonalFinanceOfflineTracker.Workers.TelegramIngest.Abstractions;
using PersonalFinanceOfflineTracker.Workers.TelegramIngest.Models;

namespace PersonalFinanceOfflineTracker.Workers.TelegramIngest.Services;

public sealed class DbTelegramIngestService(
    ITelegramCommandParser parser,
    FinanceDbContext dbContext,
    ILogger<DbTelegramIngestService> logger) : ITelegramIngestService
{
    public async Task<IngestOutcome> IngestAsync(TelegramUpdate update, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var duplicate = await dbContext.IngestedMessages
            .IgnoreQueryFilters()
            .AnyAsync(
                x => x.Provider == ExternalIdentityProvider.Telegram && x.TelegramUpdateId == update.UpdateId,
                cancellationToken);
        if (duplicate)
        {
            return new IngestOutcome(
                IngestStatus.Duplicate,
                TelegramContract.Response.DuplicateUpdate,
                TelegramContract.ErrorCode.DuplicateUpdate);
        }

        var now = DateTime.UtcNow;
        var ingestRecord = new IngestedMessage(
            id: Guid.NewGuid().ToString(),
            telegramUpdateId: update.UpdateId,
            receivedAtUtc: EnsureUtc(update.ReceivedAtUtc.UtcDateTime),
            parseStatus: IngestedMessageParseStatus.Failed,
            createdAtUtc: now,
            telegramMessageId: update.MessageId,
            rawText: update.Text);

        dbContext.IngestedMessages.Add(ingestRecord);

        try
        {
            var todayLocal = DateOnly.FromDateTime(DateTime.Now);
            var parse = parser.Parse(update.Text, todayLocal);

            if (parse.CommandKind == ParsedCommandKind.Help && parse.IsSuccess)
            {
                ingestRecord.MarkProcessed(
                    IngestedMessageParseStatus.Parsed,
                    now,
                    now,
                    normalizedCommand: parse.NormalizedCommand);
                await dbContext.SaveChangesAsync(cancellationToken);
                return new IngestOutcome(IngestStatus.Parsed, parse.ResponseMessage);
            }

            if (!parse.IsSuccess || parse.AddCommand is null)
            {
                ingestRecord.MarkProcessed(
                    IngestedMessageParseStatus.Rejected,
                    now,
                    now,
                    errorCode: parse.ErrorCode);
                await dbContext.SaveChangesAsync(cancellationToken);
                return new IngestOutcome(IngestStatus.Rejected, parse.ResponseMessage, parse.ErrorCode);
            }

            var userId = await ResolveOrCreateUserAsync(update.FromUserId, update.ChatId, now, cancellationToken);
            var category = await ResolveOrCreateCategoryAsync(userId, parse.AddCommand.Category, now, cancellationToken);
            var transaction = new Transaction(
                id: Guid.NewGuid().ToString(),
                userId: userId,
                amount: parse.AddCommand.Amount,
                description: parse.AddCommand.Description,
                transactionDate: parse.AddCommand.TransactionDate,
                source: EntitySource.Telegram,
                createdAtUtc: now,
                categoryId: category.Id);

            dbContext.Transactions.Add(transaction);

            ingestRecord.MarkProcessed(
                IngestedMessageParseStatus.Parsed,
                now,
                now,
                userId: userId,
                createdTransactionId: transaction.Id,
                normalizedCommand: parse.NormalizedCommand);

            await dbContext.SaveChangesAsync(cancellationToken);
            return new IngestOutcome(
                IngestStatus.Parsed,
                BuildSuccessMessage(parse.AddCommand),
                null,
                transaction.Id);
        }
        catch (DbUpdateException ex) when (IsDuplicateUpdateException(ex))
        {
            logger.LogInformation("Duplicate telegram update encountered for update_id={UpdateId}", update.UpdateId);
            return new IngestOutcome(
                IngestStatus.Duplicate,
                TelegramContract.Response.DuplicateUpdate,
                TelegramContract.ErrorCode.DuplicateUpdate);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed processing telegram update {UpdateId}", update.UpdateId);
            try
            {
                ingestRecord.MarkProcessed(
                    IngestedMessageParseStatus.Failed,
                    DateTime.UtcNow,
                    DateTime.UtcNow,
                    errorCode: TelegramContract.ErrorCode.Internal);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch
            {
            }

            return new IngestOutcome(
                IngestStatus.Failed,
                TelegramContract.Response.Internal,
                TelegramContract.ErrorCode.Internal);
        }
    }

    private async Task<string> ResolveOrCreateUserAsync(long providerUserId, long? providerChatId, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var providerUser = providerUserId.ToString(CultureInfo.InvariantCulture);
        var identity = await dbContext.ExternalIdentities
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                x => x.Provider == ExternalIdentityProvider.Telegram && x.ProviderUserId == providerUser,
                cancellationToken);

        if (identity is not null)
        {
            if (identity.IsDeleted)
            {
                identity.Restore(nowUtc);
            }

            if (providerChatId.HasValue)
            {
                identity.SetProviderChatId(providerChatId.Value.ToString(CultureInfo.InvariantCulture), nowUtc);
            }

            return identity.UserId;
        }

        var user = new User(
            id: Guid.NewGuid().ToString(),
            email: $"tg-{providerUserId}@telegram.local",
            displayName: $"telegram-{providerUserId}",
            passwordHash: "external-telegram",
            createdAtUtc: nowUtc);

        var external = new ExternalIdentity(
            id: Guid.NewGuid().ToString(),
            userId: user.Id,
            provider: ExternalIdentityProvider.Telegram,
            providerUserId: providerUser,
            createdAtUtc: nowUtc,
            providerChatId: providerChatId?.ToString(CultureInfo.InvariantCulture));

        dbContext.Users.Add(user);
        dbContext.ExternalIdentities.Add(external);
        return user.Id;
    }

    private async Task<Category> ResolveOrCreateCategoryAsync(string userId, string categoryName, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var normalized = categoryName.Trim().ToUpperInvariant();
        var existing = await dbContext.Categories
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                x => x.UserId == userId && x.NormalizedName == normalized,
                cancellationToken);
        if (existing is not null)
        {
            if (existing.IsDeleted)
            {
                existing.Restore(nowUtc);
            }

            if (!string.Equals(existing.Name, categoryName.Trim(), StringComparison.Ordinal))
            {
                existing.Rename(categoryName, nowUtc);
            }

            return existing;
        }

        var category = new Category(
            id: Guid.NewGuid().ToString(),
            userId: userId,
            name: categoryName,
            source: EntitySource.Telegram,
            createdAtUtc: nowUtc);
        dbContext.Categories.Add(category);
        return category;
    }

    private static string BuildSuccessMessage(ParsedAddCommandDto add)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"Saved: {add.Amount:0.00} {add.Description} [{add.Category}] on {add.TransactionDate:yyyy-MM-dd}");
    }

    private static bool IsDuplicateUpdateException(DbUpdateException ex)
    {
        return ex.InnerException?.Message.Contains("IX_IngestedMessages_Provider_TelegramUpdateId", StringComparison.OrdinalIgnoreCase) == true
            || ex.InnerException?.Message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static DateTime EnsureUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
    }
}
