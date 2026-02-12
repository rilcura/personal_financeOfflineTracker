namespace PersonalFinanceOfflineTracker.Domain.Models;

public sealed class Category : AuditableEntity
{
    public Category(string id, string userId, string name, EntitySource source, DateTime createdAtUtc)
        : base(id, source, createdAtUtc)
    {
        EnsureCategorySource(source);
        EnsureId(userId);
        EnsureRequired(name, nameof(name), 30);

        UserId = userId;
        Name = NormalizeTrimmed(name);
        NormalizedName = NormalizeName(name);
    }

    private Category()
    {
        UserId = string.Empty;
        Name = string.Empty;
        NormalizedName = string.Empty;
    }

    public string UserId { get; private set; }

    public string Name { get; private set; }

    public string NormalizedName { get; private set; }

    public void Rename(string name, DateTime updatedAtUtc)
    {
        EnsureRequired(name, nameof(name), 30);
        Name = NormalizeTrimmed(name);
        NormalizedName = NormalizeName(name);
        MarkUpdated(updatedAtUtc);
    }

    private static void EnsureCategorySource(EntitySource source)
    {
        if (source is EntitySource.App or EntitySource.Telegram or EntitySource.Web)
        {
            return;
        }

        throw new ArgumentOutOfRangeException(nameof(source), "Category source must be App, Telegram, or Web.");
    }
}
