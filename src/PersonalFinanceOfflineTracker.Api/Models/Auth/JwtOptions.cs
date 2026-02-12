namespace PersonalFinanceOfflineTracker.Api.Models.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "PersonalFinanceOfflineTracker.Api";

    public string Audience { get; init; } = "PersonalFinanceOfflineTracker.Clients";

    public string SigningKey { get; init; } = "CHANGE_ME_FOR_PROD__MIN_32_CHARS";

    public int AccessTokenMinutes { get; init; } = 15;
}
