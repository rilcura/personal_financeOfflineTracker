namespace PersonalFinanceOfflineTracker.Domain.Models;

public sealed class Transaction : AuditableEntity
{
    public Transaction(
        string id,
        string userId,
        decimal amount,
        string description,
        DateOnly transactionDate,
        EntitySource source,
        DateTime createdAtUtc,
        string? categoryId = null)
        : base(id, source, createdAtUtc)
    {
        EnsureTransactionSource(source);
        EnsureId(userId);
        EnsureRequired(description, nameof(description), 50);
        if (categoryId is not null)
        {
            EnsureId(categoryId);
        }

        UserId = userId;
        CategoryId = categoryId;
        Amount = NormalizeMoney(amount);
        Description = NormalizeTrimmed(description);
        TransactionDate = EnsureDateRange(transactionDate);
    }

    private Transaction()
    {
        UserId = string.Empty;
        Description = string.Empty;
    }

    public string UserId { get; private set; }

    public string? CategoryId { get; private set; }

    public decimal Amount { get; private set; }

    public string Description { get; private set; }

    public DateOnly TransactionDate { get; private set; }

    public void Update(
        decimal amount,
        string description,
        DateOnly transactionDate,
        DateTime updatedAtUtc,
        string? categoryId = null)
    {
        EnsureRequired(description, nameof(description), 50);
        if (categoryId is not null)
        {
            EnsureId(categoryId);
        }

        CategoryId = categoryId;
        Amount = NormalizeMoney(amount);
        Description = NormalizeTrimmed(description);
        TransactionDate = EnsureDateRange(transactionDate);
        MarkUpdated(updatedAtUtc);
    }

    private static void EnsureTransactionSource(EntitySource source)
    {
        if (source is EntitySource.App or EntitySource.Telegram or EntitySource.Web)
        {
            return;
        }

        throw new ArgumentOutOfRangeException(nameof(source), "Transaction source must be App, Telegram, or Web.");
    }
}
