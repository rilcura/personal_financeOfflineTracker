using Microsoft.AspNetCore.Mvc;
using PersonalFinanceOfflineTracker.Api.Services;
using PersonalFinanceOfflineTracker.Sync.Abstractions;
using PersonalFinanceOfflineTracker.Sync.Models;

namespace PersonalFinanceOfflineTracker.Api.Endpoints;

public static class SyncEndpoints
{
    public static IEndpointRouteBuilder MapSyncEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/sync")
            .WithTags("Sync")
            .RequireAuthorization();

        group.MapPost("/push", PushAsync)
            .WithName("PushSyncChanges");

        group.MapGet("/pull", PullAsync)
            .WithName("PullSyncChanges");

        return endpoints;
    }

    private static async Task<IResult> PushAsync(
        [FromBody] SyncPushRequestDto request,
        HttpContext httpContext,
        ISyncService syncService,
        CancellationToken cancellationToken)
    {
        var userId = httpContext.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        if (!string.IsNullOrWhiteSpace(request.UserId) && !string.Equals(request.UserId, userId, StringComparison.Ordinal))
        {
            return Results.Forbid();
        }

        var normalizedRequest = request with { UserId = userId };
        var response = await syncService.PushAsync(normalizedRequest, cancellationToken);
        return Results.Ok(response);
    }

    private static async Task<IResult> PullAsync(
        [FromQuery] string? cursor,
        HttpContext httpContext,
        ISyncService syncService,
        CancellationToken cancellationToken)
    {
        var userId = httpContext.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        var response = await syncService.PullAsync(userId, cursor, cancellationToken);
        return Results.Ok(response);
    }
}
