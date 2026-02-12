namespace PersonalFinanceOfflineTracker.Domain.Models;

public sealed class User : AuditableEntity
{
    public User(string id, string email, string displayName, string passwordHash, DateTime createdAtUtc)
        : base(id, EntitySource.System, createdAtUtc)
    {
        EnsureRequired(email, nameof(email), 256);
        EnsureRequired(displayName, nameof(displayName), 120);
        EnsureRequired(passwordHash, nameof(passwordHash), 512);

        Email = NormalizeTrimmed(email);
        DisplayName = NormalizeTrimmed(displayName);
        PasswordHash = passwordHash.Trim();
    }

    private User()
    {
        Email = string.Empty;
        DisplayName = string.Empty;
        PasswordHash = string.Empty;
    }

    public string Email { get; private set; }

    public string DisplayName { get; private set; }

    public string PasswordHash { get; private set; }

    public void Rename(string displayName, DateTime updatedAtUtc)
    {
        EnsureRequired(displayName, nameof(displayName), 120);
        DisplayName = NormalizeTrimmed(displayName);
        MarkUpdated(updatedAtUtc);
    }

    public void UpdatePasswordHash(string passwordHash, DateTime updatedAtUtc)
    {
        EnsureRequired(passwordHash, nameof(passwordHash), 512);
        PasswordHash = passwordHash.Trim();
        MarkUpdated(updatedAtUtc);
    }
}
