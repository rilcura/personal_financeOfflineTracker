using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PersonalFinanceOfflineTracker.Api.Models.Auth;
using PersonalFinanceOfflineTracker.Api.Services;
using PersonalFinanceOfflineTracker.Infrastructure.Persistence;

namespace PersonalFinanceOfflineTracker.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", LoginAsync)
            .AllowAnonymous()
            .WithName("Login");

        group.MapGet("/me", MeAsync)
            .RequireAuthorization()
            .WithName("Me");

        return endpoints;
    }

    private static async Task<IResult> LoginAsync(
        [FromBody] LoginRequestDto request,
        FinanceDbContext dbContext,
        IJwtTokenService tokenService,
        IOptions<JwtOptions> jwtOptionsAccessor,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest("Email and password are required.");
        }

        var email = request.Email.Trim();
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Results.Unauthorized();
        }

        var jwtOptions = jwtOptionsAccessor.Value;
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(jwtOptions.AccessTokenMinutes);
        var token = tokenService.CreateAccessToken(user, expiresAtUtc);

        return Results.Ok(new LoginResponseDto
        {
            AccessToken = token,
            ExpiresAtUtc = expiresAtUtc,
            UserId = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
        });
    }

    private static async Task<IResult> MeAsync(
        FinanceDbContext dbContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userId = httpContext.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(new
        {
            user.Id,
            user.Email,
            user.DisplayName,
        });
    }
}
