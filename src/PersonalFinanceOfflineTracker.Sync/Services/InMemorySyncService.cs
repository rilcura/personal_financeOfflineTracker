using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using PersonalFinanceOfflineTracker.Sync.Abstractions;
using PersonalFinanceOfflineTracker.Sync.Models;

namespace PersonalFinanceOfflineTracker.Sync.Services;

public sealed class InMemorySyncService : ISyncService
{
    private readonly ConcurrentDictionary<string, UserSyncStore> _userStores = new(StringComparer.Ordinal);

    public Task<SyncPushResponseDto> PushAsync(SyncPushRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            throw new ArgumentException("UserId is required.", nameof(request));
        }

        var accepted = new List<SyncPushAcceptedItemDto>();
        var rejected = new List<SyncPushRejectedItemDto>();
        var conflicts = new List<SyncConflictItemDto>();

        var store = _userStores.GetOrAdd(request.UserId, _ => new UserSyncStore());

        lock (store.Gate)
        {
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
                        Reason = validationError
                    });

                    continue;
                }

                var incoming = BuildIncomingState(change, request.UserId);
                var key = BuildKey(change.EntityType, change.EntityId);

                if (!store.Entities.TryGetValue(key, out var current))
                {
                    store.Entities[key] = incoming;
                    accepted.Add(new SyncPushAcceptedItemDto
                    {
                        ChangeId = change.ChangeId,
                        EntityType = change.EntityType,
                        EntityId = change.EntityId
                    });
                    continue;
                }

                var comparison = CompareVersion(incoming.UpdatedAt, incoming.EntityId, current.UpdatedAt, current.EntityId);
                if (comparison > 0)
                {
                    store.Entities[key] = incoming;
                    accepted.Add(new SyncPushAcceptedItemDto
                    {
                        ChangeId = change.ChangeId,
                        EntityType = change.EntityType,
                        EntityId = change.EntityId
                    });
                    continue;
                }

                rejected.Add(new SyncPushRejectedItemDto
                {
                    ChangeId = change.ChangeId,
                    EntityType = change.EntityType,
                    EntityId = change.EntityId,
                    Reason = "Conflict: server version is newer or equal according to LWW rules."
                });

                conflicts.Add(new SyncConflictItemDto
                {
                    ChangeId = change.ChangeId,
                    EntityType = change.EntityType,
                    EntityId = change.EntityId,
                    Winner = "Server",
                    ServerVersion = current
                });
            }

            var cursor = BuildCurrentCursor(store);

            return Task.FromResult(new SyncPushResponseDto
            {
                AcceptedItems = accepted,
                RejectedItems = rejected,
                Conflicts = conflicts,
                ServerCursor = cursor
            });
        }
    }

    public Task<SyncPullResponseDto> PullAsync(string userId, string? cursor, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        _userStores.TryGetValue(userId, out var store);
        if (store is null)
        {
            return Task.FromResult(new SyncPullResponseDto
            {
                RequestedCursor = cursor,
                NextCursor = string.Empty,
                Changes = Array.Empty<SyncEntityStateDto>()
            });
        }

        var watermark = DecodeCursor(cursor);

        lock (store.Gate)
        {
            var changes = store.Entities.Values
                .Where(entity => ShouldInclude(entity, watermark))
                .OrderBy(entity => entity.UpdatedAt)
                .ThenBy(entity => entity.EntityId, StringComparer.Ordinal)
                .ToList();

            cancellationToken.ThrowIfCancellationRequested();

            var nextCursor = changes.Count > 0
                ? EncodeCursor(new CursorWatermark(changes[^1].UpdatedAt, changes[^1].EntityId))
                : BuildCurrentCursor(store);

            return Task.FromResult(new SyncPullResponseDto
            {
                RequestedCursor = cursor,
                NextCursor = nextCursor,
                Changes = changes
            });
        }
    }

    private static bool ShouldInclude(SyncEntityStateDto entity, CursorWatermark? watermark)
    {
        if (watermark is null)
        {
            return true;
        }

        return CompareVersion(entity.UpdatedAt, entity.EntityId, watermark.Value.UpdatedAt, watermark.Value.EntityId) > 0;
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

        if (change.Operation == SyncOperationType.Delete)
        {
            var deletedAt = change.DeletedAt is null ? updatedAt : EnsureUtc(change.DeletedAt.Value);

            return new SyncEntityStateDto
            {
                EntityType = change.EntityType,
                EntityId = change.EntityId,
                UserId = userId,
                UpdatedAt = updatedAt,
                IsDeleted = true,
                DeletedAt = deletedAt,
                Transaction = change.EntityType == SyncEntityType.Transaction ? change.Transaction : null,
                Category = change.EntityType == SyncEntityType.Category ? change.Category : null
            };
        }

        var transaction = change.Transaction is null
            ? null
            : change.Transaction with
            {
                UpdatedAt = updatedAt,
                IsDeleted = change.Transaction.IsDeleted || change.Operation == SyncOperationType.Delete,
                DeletedAt = change.Transaction.IsDeleted
                    ? (change.Transaction.DeletedAt is null ? updatedAt : EnsureUtc(change.Transaction.DeletedAt.Value))
                    : null
            };

        var category = change.Category is null
            ? null
            : change.Category with
            {
                UpdatedAt = updatedAt,
                IsDeleted = change.Category.IsDeleted || change.Operation == SyncOperationType.Delete,
                DeletedAt = change.Category.IsDeleted
                    ? (change.Category.DeletedAt is null ? updatedAt : EnsureUtc(change.Category.DeletedAt.Value))
                    : null
            };

        return new SyncEntityStateDto
        {
            EntityType = change.EntityType,
            EntityId = change.EntityId,
            UserId = userId,
            UpdatedAt = updatedAt,
            IsDeleted = change.EntityType switch
            {
                SyncEntityType.Transaction => transaction?.IsDeleted ?? false,
                SyncEntityType.Category => category?.IsDeleted ?? false,
                _ => false
            },
            DeletedAt = change.EntityType switch
            {
                SyncEntityType.Transaction => transaction?.DeletedAt,
                SyncEntityType.Category => category?.DeletedAt,
                _ => null
            },
            Transaction = transaction,
            Category = category
        };
    }

    private static string BuildCurrentCursor(UserSyncStore store)
    {
        if (store.Entities.Count == 0)
        {
            return string.Empty;
        }

        var highWaterMark = store.Entities.Values
            .OrderBy(entity => entity.UpdatedAt)
            .ThenBy(entity => entity.EntityId, StringComparer.Ordinal)
            .Last();

        return EncodeCursor(new CursorWatermark(highWaterMark.UpdatedAt, highWaterMark.EntityId));
    }

    private static string BuildKey(SyncEntityType entityType, string entityId)
        => $"{entityType}:{entityId}";

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
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
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

    private sealed class UserSyncStore
    {
        public object Gate { get; } = new();
        public Dictionary<string, SyncEntityStateDto> Entities { get; } = new(StringComparer.Ordinal);
    }

    private readonly record struct CursorWatermark(DateTime UpdatedAt, string EntityId);

    private sealed record CursorPayload(long UpdatedAtTicks, string EntityId);
}
