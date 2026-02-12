namespace PersonalFinanceOfflineTracker.Apps.WebDashboard.Services;

public sealed record LoginRequestDto
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public sealed record LoginResponseDto
{
    public string AccessToken { get; init; } = string.Empty;
    public DateTime ExpiresAtUtc { get; init; }
    public string UserId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
}

public sealed record CategoryDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public sealed record CreateCategoryRequestDto
{
    public string Name { get; init; } = string.Empty;
}

public sealed record UpdateCategoryRequestDto
{
    public string Name { get; init; } = string.Empty;
}

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
