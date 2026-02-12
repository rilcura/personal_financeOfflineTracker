namespace PersonalFinanceOfflineTracker.Api.Models.Auth;

public sealed class SeedUserOptions
{
    public const string SectionName = "SeedUser";

    public string Email { get; init; } = "owner@local.dev";

    public string Password { get; init; } = "P@ssword123!";

    public string DisplayName { get; init; } = "Owner";
}
