namespace PersonalFinanceOfflineTracker.Api.Models.Transactions;

public sealed record TransactionDto
{
    public string Id { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string? CategoryId { get; init; }
    public decimal Amount { get; init; }
    public string Description { get; init; } = string.Empty;
    public DateOnly TransactionDate { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public sealed record CreateTransactionRequestDto
{
    public decimal Amount { get; init; }
    public string Description { get; init; } = string.Empty;
    public DateOnly TransactionDate { get; init; }
    public string? CategoryId { get; init; }
}

public sealed record UpdateTransactionRequestDto
{
    public decimal Amount { get; init; }
    public string Description { get; init; } = string.Empty;
    public DateOnly TransactionDate { get; init; }
    public string? CategoryId { get; init; }
}
