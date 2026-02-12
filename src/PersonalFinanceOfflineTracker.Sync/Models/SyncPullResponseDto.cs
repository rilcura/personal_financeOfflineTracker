namespace PersonalFinanceOfflineTracker.Sync.Models;

public sealed record SyncPullResponseDto
{
    public string? RequestedCursor { get; init; }
    public string NextCursor { get; init; } = string.Empty;
    public IReadOnlyList<SyncEntityStateDto> Changes { get; init; } = Array.Empty<SyncEntityStateDto>();
}
