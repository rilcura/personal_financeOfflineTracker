using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalFinanceOfflineTracker.Api.Models.Transactions;
using PersonalFinanceOfflineTracker.Api.Services;
using PersonalFinanceOfflineTracker.Domain.Models;
using PersonalFinanceOfflineTracker.Infrastructure.Persistence;

namespace PersonalFinanceOfflineTracker.Api.Endpoints;

public static class TransactionEndpoints
{
    public static IEndpointRouteBuilder MapTransactionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/transactions")
            .WithTags("Transactions")
            .RequireAuthorization();

        group.MapGet("/", ListAsync).WithName("ListTransactions");
        group.MapPost("/", CreateAsync).WithName("CreateTransaction");
        group.MapPut("/{id}", UpdateAsync).WithName("UpdateTransaction");
        group.MapDelete("/{id}", DeleteAsync).WithName("DeleteTransaction");

        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? month,
        FinanceDbContext dbContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userId = httpContext.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        var query = dbContext.Transactions.Where(x => x.UserId == userId);

        if (!string.IsNullOrWhiteSpace(month))
        {
            if (!DateOnly.TryParse($"{month}-01", out var monthStart))
            {
                return Results.BadRequest("month must be yyyy-MM.");
            }

            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            query = query.Where(x => x.TransactionDate >= monthStart && x.TransactionDate <= monthEnd);
        }

        if (from.HasValue)
        {
            query = query.Where(x => x.TransactionDate >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(x => x.TransactionDate <= to.Value);
        }

        var items = await query
            .OrderByDescending(x => x.TransactionDate)
            .ThenByDescending(x => x.UpdatedAt)
            .Select(x => new TransactionDto
            {
                Id = x.Id,
                UserId = x.UserId,
                CategoryId = x.CategoryId,
                Amount = x.Amount,
                Description = x.Description,
                TransactionDate = x.TransactionDate,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(items);
    }

    private static async Task<IResult> CreateAsync(
        [FromBody] CreateTransactionRequestDto request,
        FinanceDbContext dbContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userId = httpContext.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        if (!string.IsNullOrWhiteSpace(request.CategoryId))
        {
            var categoryExists = await dbContext.Categories.AnyAsync(
                x => x.Id == request.CategoryId && x.UserId == userId,
                cancellationToken);
            if (!categoryExists)
            {
                return Results.BadRequest("Category does not exist.");
            }
        }

        var now = DateTime.UtcNow;
        var transaction = new Transaction(
            id: Guid.NewGuid().ToString(),
            userId: userId,
            amount: request.Amount,
            description: request.Description,
            transactionDate: request.TransactionDate,
            source: EntitySource.Web,
            createdAtUtc: now,
            categoryId: request.CategoryId);

        dbContext.Transactions.Add(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/transactions/{transaction.Id}", new TransactionDto
        {
            Id = transaction.Id,
            UserId = transaction.UserId,
            CategoryId = transaction.CategoryId,
            Amount = transaction.Amount,
            Description = transaction.Description,
            TransactionDate = transaction.TransactionDate,
            CreatedAt = transaction.CreatedAt,
            UpdatedAt = transaction.UpdatedAt,
        });
    }

    private static async Task<IResult> UpdateAsync(
        [FromRoute] string id,
        [FromBody] UpdateTransactionRequestDto request,
        FinanceDbContext dbContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userId = httpContext.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        var transaction = await dbContext.Transactions.FirstOrDefaultAsync(
            x => x.Id == id && x.UserId == userId,
            cancellationToken);
        if (transaction is null)
        {
            return Results.NotFound();
        }

        if (!string.IsNullOrWhiteSpace(request.CategoryId))
        {
            var categoryExists = await dbContext.Categories.AnyAsync(
                x => x.Id == request.CategoryId && x.UserId == userId,
                cancellationToken);
            if (!categoryExists)
            {
                return Results.BadRequest("Category does not exist.");
            }
        }

        transaction.Update(
            amount: request.Amount,
            description: request.Description,
            transactionDate: request.TransactionDate,
            updatedAtUtc: DateTime.UtcNow,
            categoryId: request.CategoryId);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteAsync(
        [FromRoute] string id,
        FinanceDbContext dbContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userId = httpContext.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        var transaction = await dbContext.Transactions.FirstOrDefaultAsync(
            x => x.Id == id && x.UserId == userId,
            cancellationToken);
        if (transaction is null)
        {
            return Results.NotFound();
        }

        transaction.SoftDelete(DateTime.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }
}
