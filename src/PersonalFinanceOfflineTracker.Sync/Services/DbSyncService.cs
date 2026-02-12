using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PersonalFinanceOfflineTracker.Domain.Models;
using PersonalFinanceOfflineTracker.Infrastructure.Persistence;
using PersonalFinanceOfflineTracker.Sync.Abstractions;
using PersonalFinanceOfflineTracker.Sync.Models;

namespace PersonalFinanceOfflineTracker.Sync.Services;

public sealed class DbSyncService : ISyncService
{
    private readonly FinanceDbContext _dbContext;

    public DbSyncService(FinanceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SyncPushResponseDto> PushAsync(SyncPushRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            throw new ArgumentException("UserId is required.", nameof(request));
        }

        var accepted = new List<SyncPushAcceptedItemDto>();
        var rejected = new List<SyncPushRejectedItemDto>();
        var conflicts = new List<SyncConflictItemDto>();

        foreach (var change in request.Changes)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var validationError = ValidateChange(change, request.UserId);
            if (validationError is not null)
            {
                rejected.Add(new SyncPushRejectedItemDto
                {
                    ChangeId = change.ChangeId,
                    EntityType = change.EntityType,
                    EntityId = change.EntityId,
                    Reason = validationError,
                });
                continue;
            }

            var incomingState = BuildIncomingState(change, request.UserId);

            var result = change.EntityType switch
            {
                SyncEntityType.Transaction => await ApplyTransactionChangeAsync(change, incomingState, cancellationToken),
                SyncEntityType.Category => await ApplyCategoryChangeAsync(change, incomingState, cancellationToken),
                _ => ApplyResult.Rejected("Unsupported entity type."),
            };

            if (result.Kind == ApplyResultKind.Accepted)
            {
                accepted.Add(new SyncPushAcceptedItemDto
                {
                    ChangeId = change.ChangeId,
                    EntityType = change.EntityType,
                    EntityId = change.EntityId,
                });
                continue;
            }

            if (result.Kind == ApplyResultKind.Conflict && result.ServerVersion is not null)
            {
                rejected.Add(new SyncPushRejectedItemDto
                {
                    ChangeId = change.ChangeId,
                    EntityType = change.EntityType,
                    EntityId = change.EntityId,
                    Reason = "Conflict: server version is newer or equal according to LWW rules.",
                });

                conflicts.Add(new SyncConflictItemDto
                {
                    ChangeId = change.ChangeId,
                    EntityType = change.EntityType,
                    EntityId = change.EntityId,
                    Winner = "Server",
                    ServerVersion = result.ServerVersion,
                });
                continue;
            }

            rejected.Add(new SyncPushRejectedItemDto
            {
                ChangeId = change.ChangeId,
                EntityType = change.EntityType,
                EntityId = change.EntityId,
                Reason = result.Reason ?? "Rejected by server.",
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        var serverCursor = await BuildCurrentCursorAsync(request.UserId, cancellationToken);

        return new SyncPushResponseDto
        {
            AcceptedItems = accepted,
            RejectedItems = rejected,
            Conflicts = conflicts,
            ServerCursor = serverCursor,
        };
    }

    public async Task<SyncPullResponseDto> PullAsync(string userId, string? cursor, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        var watermark = DecodeCursor(cursor);

        var transactionChanges = await _dbContext.Transactions
            .IgnoreQueryFilters()
            .Where(x => x.UserId == userId)
            .Where(x => watermark == null || CompareVersion(x.UpdatedAt, x.Id, watermark.Value.UpdatedAt, watermark.Value.EntityId) > 0)
            .Select(x => ToState(x))
            .ToListAsync(cancellationToken);

        var categoryChanges = await _dbContext.Categories
            .IgnoreQueryFilters()
            .Where(x => x.UserId == userId)
            .Where(x => watermark == null || CompareVersion(x.UpdatedAt, x.Id, watermark.Value.UpdatedAt, watermark.Value.EntityId) > 0)
            .Select(x => ToState(x))
            .ToListAsync(cancellationToken);

        var merged = transactionChanges
            .Concat(categoryChanges)
            .OrderBy(x => x.UpdatedAt)
            .ThenBy(x => x.EntityId, StringComparer.Ordinal)
            .ToList();

        var nextCursor = merged.Count > 0
            ? EncodeCursor(new CursorWatermark(merged[^1].UpdatedAt, merged[^1].EntityId))
            : await BuildCurrentCursorAsync(userId, cancellationToken);

        return new SyncPullResponseDto
        {
            RequestedCursor = cursor,
            NextCursor = nextCursor,
            Changes = merged,
        };
    }

    private async Task<ApplyResult> ApplyTransactionChangeAsync(
        SyncChangeDto change,
        SyncEntityStateDto incomingState,
        CancellationToken cancellationToken)
    {
        var existing = await _dbContext.Transactions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == change.EntityId, cancellationToken);

        if (existing is null)
        {
            if (change.Operation == SyncOperationType.Delete)
            {
                return ApplyResult.Accepted();
            }

            var payload = change.Transaction!;
            var source = ParseSource(payload.Source);
            var createdAt = EnsureUtc(payload.CreatedAt);
            var updatedAt = EnsureUtc(payload.UpdatedAt);

            var entity = new Transaction(
                id: payload.Id,
                userId: payload.UserId,
                amount: payload.Amount,
                description: payload.Description,
                transactionDate: payload.TransactionDate,
                source: source,
                createdAtUtc: createdAt,
                categoryId: payload.CategoryId);

            if (updatedAt != createdAt)
            {
                entity.Update(payload.Amount, payload.Description, payload.TransactionDate, updatedAt, payload.CategoryId);
            }

            if (payload.IsDeleted || change.Operation == SyncOperationType.Delete)
            {
                entity.SoftDelete(payload.DeletedAt is null ? updatedAt : EnsureUtc(payload.DeletedAt.Value));
            }

            _dbContext.Transactions.Add(entity);
            return ApplyResult.Accepted();
        }

        var compare = CompareVersion(incomingState.UpdatedAt, incomingState.EntityId, existing.UpdatedAt, existing.Id);
        if (compare <= 0)
        {
            return ApplyResult.Conflict(ToState(existing));
        }

        if (change.Operation == SyncOperationType.Delete)
        {
            existing.SoftDelete(incomingState.DeletedAt ?? incomingState.UpdatedAt);
            return ApplyResult.Accepted();
        }

        var upsert = change.Transaction!;
        existing.Update(upsert.Amount, upsert.Description, upsert.TransactionDate, EnsureUtc(upsert.UpdatedAt), upsert.CategoryId);

        if (upsert.IsDeleted)
        {
            existing.SoftDelete(upsert.DeletedAt is null ? EnsureUtc(upsert.UpdatedAt) : EnsureUtc(upsert.DeletedAt.Value));
        }
        else if (existing.IsDeleted)
        {
            existing.Restore(EnsureUtc(upsert.UpdatedAt));
        }

        return ApplyResult.Accepted();
    }

    private async Task<ApplyResult> ApplyCategoryChangeAsync(
        SyncChangeDto change,
        SyncEntityStateDto incomingState,
        CancellationToken cancellationToken)
    {
        var existing = await _dbContext.Categories
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == change.EntityId, cancellationToken);

        if (existing is null)
        {
            if (change.Operation == SyncOperationType.Delete)
            {
                return ApplyResult.Accepted();
            }

            var payload = change.Category!;
            var source = ParseSource(payload.Source);
            var createdAt = EnsureUtc(payload.CreatedAt);
            var updatedAt = EnsureUtc(payload.UpdatedAt);

            var entity = new Category(
                id: payload.Id,
                userId: payload.UserId,
                name: payload.Name,
                source: source,
                createdAtUtc: createdAt);

            if (updatedAt != createdAt)
            {
                entity.Rename(payload.Name, updatedAt);
            }

            if (payload.IsDeleted || change.Operation == SyncOperationType.Delete)
            {
                entity.SoftDelete(payload.DeletedAt is null ? updatedAt : EnsureUtc(payload.DeletedAt.Value));
            }

            _dbContext.Categories.Add(entity);
            return ApplyResult.Accepted();
        }

        var compare = CompareVersion(incomingState.UpdatedAt, incomingState.EntityId, existing.UpdatedAt, existing.Id);
        if (compare <= 0)
        {
            return ApplyResult.Conflict(ToState(existing));
        }

        if (change.Operation == SyncOperationType.Delete)
        {
            existing.SoftDelete(incomingState.DeletedAt ?? incomingState.UpdatedAt);
            return ApplyResult.Accepted();
        }

        var upsert = change.Category!;
        existing.Rename(upsert.Name, EnsureUtc(upsert.UpdatedAt));
        if (upsert.IsDeleted)
        {
            existing.SoftDelete(upsert.DeletedAt is null ? EnsureUtc(upsert.UpdatedAt) : EnsureUtc(upsert.DeletedAt.Value));
        }
        else if (existing.IsDeleted)
        {
            existing.Restore(EnsureUtc(upsert.UpdatedAt));
        }

        return ApplyResult.Accepted();
    }

    private async Task<string> BuildCurrentCursorAsync(string userId, CancellationToken cancellationToken)
    {
        var lastTransaction = await _dbContext.Transactions
            .IgnoreQueryFilters()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.UpdatedAt)
            .ThenByDescending(x => x.Id)
            .Select(x => new { x.UpdatedAt, x.Id })
            .FirstOrDefaultAsync(cancellationToken);

        var lastCategory = await _dbContext.Categories
            .IgnoreQueryFilters()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.UpdatedAt)
            .ThenByDescending(x => x.Id)
            .Select(x => new { x.UpdatedAt, x.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (lastTransaction is null && lastCategory is null)
        {
            return string.Empty;
        }

        if (lastTransaction is null)
        {
            return EncodeCursor(new CursorWatermark(lastCategory!.UpdatedAt, lastCategory.Id));
        }

        if (lastCategory is null)
        {
            return EncodeCursor(new CursorWatermark(lastTransaction.UpdatedAt, lastTransaction.Id));
        }

        var compare = CompareVersion(lastTransaction.UpdatedAt, lastTransaction.Id, lastCategory.UpdatedAt, lastCategory.Id);
        var chosen = compare >= 0
            ? new CursorWatermark(lastTransaction.UpdatedAt, lastTransaction.Id)
            : new CursorWatermark(lastCategory.UpdatedAt, lastCategory.Id);

        return EncodeCursor(chosen);
    }

    private static string? ValidateChange(SyncChangeDto change, string userId)
    {
        if (string.IsNullOrWhiteSpace(change.ChangeId))
        {
            return "ChangeId is required.";
        }

        if (string.IsNullOrWhiteSpace(change.EntityId))
        {
            return "EntityId is required.";
        }

        if (change.Operation == SyncOperationType.Upsert)
        {
            if (change.EntityType == SyncEntityType.Transaction)
            {
                if (change.Transaction is null)
                {
                    return "Transaction payload is required for upsert.";
                }

                if (!string.Equals(change.Transaction.Id, change.EntityId, StringComparison.Ordinal))
                {
                    return "Transaction payload Id must match EntityId.";
                }

                if (!string.Equals(change.Transaction.UserId, userId, StringComparison.Ordinal))
                {
                    return "Transaction payload UserId must match request UserId.";
                }
            }
            else
            {
                if (change.Category is null)
                {
                    return "Category payload is required for upsert.";
                }

                if (!string.Equals(change.Category.Id, change.EntityId, StringComparison.Ordinal))
                {
                    return "Category payload Id must match EntityId.";
                }

                if (!string.Equals(change.Category.UserId, userId, StringComparison.Ordinal))
                {
                    return "Category payload UserId must match request UserId.";
                }
            }
        }

        return null;
    }

    private static SyncEntityStateDto BuildIncomingState(SyncChangeDto change, string userId)
    {
        var updatedAt = EnsureUtc(change.UpdatedAt);
        var deletedAt = change.DeletedAt is null ? (DateTime?)null : EnsureUtc(change.DeletedAt.Value);

        return new SyncEntityStateDto
        {
            EntityType = change.EntityType,
            EntityId = change.EntityId,
            UserId = userId,
            UpdatedAt = updatedAt,
            IsDeleted = change.Operation == SyncOperationType.Delete
                || (change.EntityType == SyncEntityType.Transaction ? change.Transaction?.IsDeleted ?? false : change.Category?.IsDeleted ?? false),
            DeletedAt = deletedAt,
            Transaction = change.Transaction,
            Category = change.Category,
        };
    }

    private static SyncEntityStateDto ToState(Transaction entity)
    {
        return new SyncEntityStateDto
        {
            EntityType = SyncEntityType.Transaction,
            EntityId = entity.Id,
            UserId = entity.UserId,
            UpdatedAt = entity.UpdatedAt,
            IsDeleted = entity.IsDeleted,
            DeletedAt = entity.DeletedAt,
            Transaction = new TransactionSyncDto
            {
                Id = entity.Id,
                UserId = entity.UserId,
                CategoryId = entity.CategoryId,
                Amount = entity.Amount,
                Description = entity.Description,
                TransactionDate = entity.TransactionDate,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                IsDeleted = entity.IsDeleted,
                DeletedAt = entity.DeletedAt,
                Source = entity.Source.ToString(),
            },
        };
    }

    private static SyncEntityStateDto ToState(Category entity)
    {
        return new SyncEntityStateDto
        {
            EntityType = SyncEntityType.Category,
            EntityId = entity.Id,
            UserId = entity.UserId,
            UpdatedAt = entity.UpdatedAt,
            IsDeleted = entity.IsDeleted,
            DeletedAt = entity.DeletedAt,
            Category = new CategorySyncDto
            {
                Id = entity.Id,
                UserId = entity.UserId,
                Name = entity.Name,
                NormalizedName = entity.NormalizedName,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                IsDeleted = entity.IsDeleted,
                DeletedAt = entity.DeletedAt,
                Source = entity.Source.ToString(),
            },
        };
    }

    private static EntitySource ParseSource(string source)
    {
        if (Enum.TryParse<EntitySource>(source, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        return EntitySource.App;
    }

    private static int CompareVersion(DateTime leftUpdatedAt, string leftId, DateTime rightUpdatedAt, string rightId)
    {
        var leftUtc = EnsureUtc(leftUpdatedAt);
        var rightUtc = EnsureUtc(rightUpdatedAt);
        var timeCompare = leftUtc.CompareTo(rightUtc);
        if (timeCompare != 0)
        {
            return timeCompare;
        }

        return string.Compare(leftId, rightId, StringComparison.Ordinal);
    }

    private static DateTime EnsureUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
    }

    private static string EncodeCursor(CursorWatermark watermark)
    {
        var payload = new CursorPayload(watermark.UpdatedAt.Ticks, watermark.EntityId);
        var json = JsonSerializer.Serialize(payload);
        var bytes = Encoding.UTF8.GetBytes(json);
        var token = Convert.ToBase64String(bytes);
        token = token.TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return token;
    }

    private static CursorWatermark? DecodeCursor(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return null;
        }

        try
        {
            var normalized = cursor.Replace('-', '+').Replace('_', '/');
            normalized = normalized.PadRight(normalized.Length + (4 - normalized.Length % 4) % 4, '=');
            var bytes = Convert.FromBase64String(normalized);
            var json = Encoding.UTF8.GetString(bytes);
            var payload = JsonSerializer.Deserialize<CursorPayload>(json);
            if (payload is null || payload.UpdatedAtTicks <= 0 || string.IsNullOrWhiteSpace(payload.EntityId))
            {
                return null;
            }

            return new CursorWatermark(new DateTime(payload.UpdatedAtTicks, DateTimeKind.Utc), payload.EntityId);
        }
        catch
        {
            return null;
        }
    }

    private readonly record struct CursorWatermark(DateTime UpdatedAt, string EntityId);

    private sealed record CursorPayload(long UpdatedAtTicks, string EntityId);

    private sealed class ApplyResult
    {
        public ApplyResultKind Kind { get; init; }
        public string? Reason { get; init; }
        public SyncEntityStateDto? ServerVersion { get; init; }

        public static ApplyResult Accepted() => new() { Kind = ApplyResultKind.Accepted };
        public static ApplyResult Rejected(string reason) => new() { Kind = ApplyResultKind.Rejected, Reason = reason };
        public static ApplyResult Conflict(SyncEntityStateDto server) => new() { Kind = ApplyResultKind.Conflict, ServerVersion = server };
    }

    private enum ApplyResultKind
    {
        Accepted = 1,
        Rejected = 2,
        Conflict = 3,
    }
}
