namespace PersonalFinanceOfflineTracker.Api.Models.Categories;

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
