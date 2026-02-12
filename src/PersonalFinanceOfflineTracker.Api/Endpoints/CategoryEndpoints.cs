using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalFinanceOfflineTracker.Api.Models.Categories;
using PersonalFinanceOfflineTracker.Api.Services;
using PersonalFinanceOfflineTracker.Domain.Models;
using PersonalFinanceOfflineTracker.Infrastructure.Persistence;

namespace PersonalFinanceOfflineTracker.Api.Endpoints;

public static class CategoryEndpoints
{
    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/categories")
            .WithTags("Categories")
            .RequireAuthorization();

        group.MapGet("/", ListAsync).WithName("ListCategories");
        group.MapPost("/", CreateAsync).WithName("CreateCategory");
        group.MapPut("/{id}", UpdateAsync).WithName("UpdateCategory");
        group.MapDelete("/{id}", DeleteAsync).WithName("DeleteCategory");

        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        FinanceDbContext dbContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userId = httpContext.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        var items = await dbContext.Categories
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Name)
            .Select(x => new CategoryDto
            {
                Id = x.Id,
                Name = x.Name,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(items);
    }

    private static async Task<IResult> CreateAsync(
        [FromBody] CreateCategoryRequestDto request,
        FinanceDbContext dbContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userId = httpContext.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest("Name is required.");
        }

        var normalizedName = request.Name.Trim().ToUpperInvariant();
        var exists = await dbContext.Categories.AnyAsync(
            x => x.UserId == userId && x.NormalizedName == normalizedName,
            cancellationToken);
        if (exists)
        {
            return Results.Conflict("Category already exists.");
        }

        var now = DateTime.UtcNow;
        var category = new Category(
            id: Guid.NewGuid().ToString(),
            userId: userId,
            name: request.Name,
            source: EntitySource.Web,
            createdAtUtc: now);

        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/categories/{category.Id}", new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt,
        });
    }

    private static async Task<IResult> UpdateAsync(
        [FromRoute] string id,
        [FromBody] UpdateCategoryRequestDto request,
        FinanceDbContext dbContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var userId = httpContext.User.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        var category = await dbContext.Categories.FirstOrDefaultAsync(
            x => x.Id == id && x.UserId == userId,
            cancellationToken);
        if (category is null)
        {
            return Results.NotFound();
        }

        category.Rename(request.Name, DateTime.UtcNow);
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

        var category = await dbContext.Categories.FirstOrDefaultAsync(
            x => x.Id == id && x.UserId == userId,
            cancellationToken);
        if (category is null)
        {
            return Results.NotFound();
        }

        category.SoftDelete(DateTime.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }
}
