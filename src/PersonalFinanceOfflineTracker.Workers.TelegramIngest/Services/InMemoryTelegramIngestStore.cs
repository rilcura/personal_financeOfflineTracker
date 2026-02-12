using System.Collections.Concurrent;
using PersonalFinanceOfflineTracker.Workers.TelegramIngest.Models;

namespace PersonalFinanceOfflineTracker.Workers.TelegramIngest.Services;

public sealed class InMemoryTelegramIngestStore
{
    private readonly object _transactionGate = new();
    private readonly ConcurrentDictionary<long, IngestedMessageRecord> _ingestedByUpdateId = new();
    private readonly ConcurrentDictionary<string, UserRecord> _usersById = new();
    private readonly ConcurrentDictionary<string, ExternalIdentityRecord> _externalIdentityByProviderAndUserId = new();
    private readonly ConcurrentDictionary<string, CategoryRecord> _categoriesByUserAndNormalizedName = new();
    private readonly List<TransactionRecord> _transactions = [];

    public bool TryAddIngestRecord(IngestedMessageRecord record)
    {
        return _ingestedByUpdateId.TryAdd(record.TelegramUpdateId, record);
    }

    public bool HasIngestedUpdate(long telegramUpdateId)
    {
        return _ingestedByUpdateId.ContainsKey(telegramUpdateId);
    }

    public void UpdateIngestRecord(IngestedMessageRecord record)
    {
        _ingestedByUpdateId[record.TelegramUpdateId] = record;
    }

    public string ResolveOrCreateUser(long providerUserId, long? providerChatId, DateTimeOffset nowUtc)
    {
        string providerKey = $"Telegram:{providerUserId}";
        ExternalIdentityRecord identity = _externalIdentityByProviderAndUserId.GetOrAdd(
            providerKey,
            _ =>
            {
                string userId = Guid.NewGuid().ToString("N");
                UserRecord user = new()
                {
                    Id = userId,
                    DisplayName = $"telegram-{providerUserId}",
                    Source = "System",
                    CreatedAt = nowUtc,
                    UpdatedAt = nowUtc
                };
                _usersById[userId] = user;

                return new ExternalIdentityRecord
                {
                    Id = Guid.NewGuid().ToString("N"),
                    UserId = userId,
                    Provider = "Telegram",
                    ProviderUserId = providerUserId.ToString(),
                    ProviderChatId = providerChatId?.ToString(),
                    CreatedAt = nowUtc,
                    UpdatedAt = nowUtc
                };
            });

        if (providerChatId.HasValue && identity.ProviderChatId != providerChatId.Value.ToString())
        {
            identity.ProviderChatId = providerChatId.Value.ToString();
            identity.UpdatedAt = nowUtc;
        }

        return identity.UserId;
    }

    public CategoryRecord ResolveOrCreateCategory(string userId, string categoryName, DateTimeOffset nowUtc)
    {
        string normalizedName = categoryName.Trim().ToUpperInvariant();
        string categoryKey = $"{userId}:{normalizedName}";

        return _categoriesByUserAndNormalizedName.GetOrAdd(
            categoryKey,
            _ => new CategoryRecord
            {
                Id = Guid.NewGuid().ToString("N"),
                UserId = userId,
                Name = categoryName,
                NormalizedName = normalizedName,
                Source = "Telegram",
                CreatedAt = nowUtc,
                UpdatedAt = nowUtc
            });
    }

    public string CreateTransaction(
        string userId,
        CategoryRecord category,
        ParsedAddCommandDto command,
        DateTimeOffset nowUtc)
    {
        lock (_transactionGate)
        {
            string transactionId = Guid.NewGuid().ToString("N");
            _transactions.Add(new TransactionRecord
            {
                Id = transactionId,
                UserId = userId,
                CategoryId = category.Id,
                Amount = command.Amount,
                Description = command.Description,
                TransactionDate = command.TransactionDate,
                Source = "Telegram",
                CreatedAt = nowUtc,
                UpdatedAt = nowUtc
            });

            return transactionId;
        }
    }
}
