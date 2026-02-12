namespace PersonalFinanceOfflineTracker.Domain.Models;

public sealed class ExternalIdentity : AuditableEntity
{
    public ExternalIdentity(
        string id,
        string userId,
        ExternalIdentityProvider provider,
        string providerUserId,
        DateTime createdAtUtc,
        string? providerChatId = null)
        : base(id, EntitySource.System, createdAtUtc)
    {
        EnsureId(userId);
        EnsureRequired(providerUserId, nameof(providerUserId), 100);
        EnsureOptional(providerChatId, nameof(providerChatId), 100);

        UserId = userId;
        Provider = provider;
        ProviderUserId = NormalizeTrimmed(providerUserId);
        ProviderChatId = providerChatId is null ? null : NormalizeTrimmed(providerChatId);
    }

    private ExternalIdentity()
    {
        UserId = string.Empty;
        ProviderUserId = string.Empty;
    }

    public string UserId { get; private set; }

    public ExternalIdentityProvider Provider { get; private set; }

    public string ProviderUserId { get; private set; }

    public string? ProviderChatId { get; private set; }

    public void SetProviderChatId(string? providerChatId, DateTime updatedAtUtc)
    {
        EnsureOptional(providerChatId, nameof(providerChatId), 100);
        ProviderChatId = providerChatId is null ? null : NormalizeTrimmed(providerChatId);
        MarkUpdated(updatedAtUtc);
    }
}
