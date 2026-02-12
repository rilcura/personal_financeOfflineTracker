using Microsoft.AspNetCore.Mvc;
using PersonalFinanceOfflineTracker.Sync.Abstractions;
using PersonalFinanceOfflineTracker.Sync.Models;

namespace PersonalFinanceOfflineTracker.Api.Endpoints;

public static class SyncEndpoints
{
    public static IEndpointRouteBuilder MapSyncEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/sync").WithTags("Sync");

        group.MapPost("/push", PushAsync)
            .WithName("PushSyncChanges");

        group.MapGet("/pull", PullAsync)
            .WithName("PullSyncChanges");

        return endpoints;
    }

    private static async Task<IResult> PushAsync(
        [FromBody] SyncPushRequestDto request,
        ISyncService syncService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return Results.BadRequest("UserId is required.");
        }

        var response = await syncService.PushAsync(request, cancellationToken);
        return Results.Ok(response);
    }

    private static async Task<IResult> PullAsync(
        [FromQuery] string userId,
        [FromQuery] string? cursor,
        ISyncService syncService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.BadRequest("userId is required.");
        }

        var response = await syncService.PullAsync(userId, cursor, cancellationToken);
        return Results.Ok(response);
    }
}
