using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using PersonalFinanceOfflineTracker.Api.Models.Auth;
using PersonalFinanceOfflineTracker.Api.Services;
using PersonalFinanceOfflineTracker.Domain.Models;

namespace PersonalFinanceOfflineTracker.Tests;

public sealed class AuthServicesTests
{
    [Fact]
    public void PasswordHasher_Hash_And_Verify_Work()
    {
        const string password = "Passw0rd!";
        var hash = PasswordHasher.Hash(password);

        Assert.NotEmpty(hash);
        Assert.True(PasswordHasher.Verify(password, hash));
        Assert.False(PasswordHasher.Verify("wrong-password", hash));
    }

    [Fact]
    public void JwtTokenService_CreatesToken_WithExpectedClaims()
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            SigningKey = "12345678901234567890123456789012",
        });

        var service = new JwtTokenService(options);
        var user = new User(
            id: Guid.NewGuid().ToString(),
            email: "demo@example.com",
            displayName: "Demo User",
            passwordHash: PasswordHasher.Hash("Passw0rd!"),
            createdAtUtc: DateTime.UtcNow);

        var expiresAt = DateTime.UtcNow.AddMinutes(15);
        var token = service.CreateAccessToken(user, expiresAt);

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal("test-issuer", parsed.Issuer);
        Assert.Contains("test-audience", parsed.Audiences);
        Assert.Contains(parsed.Claims, claim => claim.Type == JwtRegisteredClaimNames.Sub && claim.Value == user.Id);
        Assert.Contains(parsed.Claims, claim => claim.Type == JwtRegisteredClaimNames.Email && claim.Value == user.Email);
    }
}
