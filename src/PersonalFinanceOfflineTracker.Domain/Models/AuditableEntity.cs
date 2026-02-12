namespace PersonalFinanceOfflineTracker.Domain.Models;

public abstract class AuditableEntity
{
    protected AuditableEntity(string id, EntitySource source, DateTime createdAtUtc)
    {
        EnsureId(id);
        EnsureUtc(createdAtUtc, nameof(createdAtUtc));

        Id = id;
        Source = source;
        CreatedAt = createdAtUtc;
        UpdatedAt = createdAtUtc;
    }

    protected AuditableEntity()
    {
        Id = string.Empty;
    }

    public string Id { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTime? DeletedAt { get; private set; }

    public EntitySource Source { get; private set; }

    public void MarkUpdated(DateTime updatedAtUtc)
    {
        EnsureUtc(updatedAtUtc, nameof(updatedAtUtc));
        if (updatedAtUtc < CreatedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(updatedAtUtc), "UpdatedAt cannot be earlier than CreatedAt.");
        }

        UpdatedAt = updatedAtUtc;
    }

    public void SoftDelete(DateTime deletedAtUtc)
    {
        EnsureUtc(deletedAtUtc, nameof(deletedAtUtc));
        IsDeleted = true;
        DeletedAt = deletedAtUtc;
        MarkUpdated(deletedAtUtc);
    }

    public void Restore(DateTime restoredAtUtc)
    {
        EnsureUtc(restoredAtUtc, nameof(restoredAtUtc));
        IsDeleted = false;
        DeletedAt = null;
        MarkUpdated(restoredAtUtc);
    }

    protected static void EnsureId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Id is required.", nameof(id));
        }

        _ = Guid.Parse(id);
    }

    protected static void EnsureRequired(string value, string paramName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{paramName} is required.", paramName);
        }

        if (value.Trim().Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, $"{paramName} must be <= {maxLength} chars.");
        }
    }

    protected static void EnsureOptional(string? value, string paramName, int maxLength)
    {
        if (value is null)
        {
            return;
        }

        if (value.Trim().Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, $"{paramName} must be <= {maxLength} chars.");
        }
    }

    protected static void EnsureUtc(DateTime value, string paramName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException($"{paramName} must be UTC.", paramName);
        }
    }

    protected static string NormalizeName(string value)
    {
        return value.Trim().ToUpperInvariant();
    }

    protected static string NormalizeTrimmed(string value)
    {
        return value.Trim();
    }

    protected static decimal NormalizeMoney(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");
        }

        var normalized = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        if (normalized != amount)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must have at most 2 decimal places.");
        }

        if (normalized > 9999999.99m)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount exceeds v1 max range.");
        }

        return normalized;
    }

    protected static DateOnly EnsureDateRange(DateOnly date)
    {
        var min = new DateOnly(2000, 1, 1);
        if (date < min)
        {
            throw new ArgumentOutOfRangeException(nameof(date), "Date is out of allowed range.");
        }

        return date;
    }
}
