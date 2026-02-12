namespace PersonalFinanceOfflineTracker.Domain.Models;

public sealed class IngestedMessage : AuditableEntity
{
    public IngestedMessage(
        string id,
        long telegramUpdateId,
        DateTime receivedAtUtc,
        IngestedMessageParseStatus parseStatus,
        DateTime createdAtUtc,
        long? telegramMessageId = null,
        string? rawText = null,
        string? normalizedCommand = null,
        string? errorCode = null,
        string? userId = null,
        string? createdTransactionId = null,
        DateTime? processedAtUtc = null)
        : base(id, EntitySource.Telegram, createdAtUtc)
    {
        if (telegramUpdateId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(telegramUpdateId), "TelegramUpdateId must be positive.");
        }

        EnsureUtc(receivedAtUtc, nameof(receivedAtUtc));
        EnsureOptional(rawText, nameof(rawText), 4000);
        EnsureOptional(normalizedCommand, nameof(normalizedCommand), 2000);
        EnsureOptional(errorCode, nameof(errorCode), 100);

        if (userId is not null)
        {
            EnsureId(userId);
        }

        if (createdTransactionId is not null)
        {
            EnsureId(createdTransactionId);
        }

        if (processedAtUtc.HasValue)
        {
            EnsureUtc(processedAtUtc.Value, nameof(processedAtUtc));
        }

        Provider = ExternalIdentityProvider.Telegram;
        TelegramUpdateId = telegramUpdateId;
        TelegramMessageId = telegramMessageId;
        RawText = rawText is null ? null : rawText.Trim();
        NormalizedCommand = normalizedCommand is null ? null : normalizedCommand.Trim();
        ParseStatus = parseStatus;
        ErrorCode = errorCode is null ? null : errorCode.Trim();
        UserId = userId;
        CreatedTransactionId = createdTransactionId;
        ReceivedAt = receivedAtUtc;
        ProcessedAt = processedAtUtc;
    }

    private IngestedMessage()
    {
    }

    public ExternalIdentityProvider Provider { get; private set; }

    public long TelegramUpdateId { get; private set; }

    public long? TelegramMessageId { get; private set; }

    public string? RawText { get; private set; }

    public string? NormalizedCommand { get; private set; }

    public IngestedMessageParseStatus ParseStatus { get; private set; }

    public string? ErrorCode { get; private set; }

    public string? UserId { get; private set; }

    public string? CreatedTransactionId { get; private set; }

    public DateTime ReceivedAt { get; private set; }

    public DateTime? ProcessedAt { get; private set; }

    public void MarkProcessed(
        IngestedMessageParseStatus status,
        DateTime processedAtUtc,
        DateTime updatedAtUtc,
        string? errorCode = null,
        string? userId = null,
        string? createdTransactionId = null,
        string? normalizedCommand = null)
    {
        EnsureUtc(processedAtUtc, nameof(processedAtUtc));
        EnsureOptional(errorCode, nameof(errorCode), 100);
        EnsureOptional(normalizedCommand, nameof(normalizedCommand), 2000);

        if (userId is not null)
        {
            EnsureId(userId);
        }

        if (createdTransactionId is not null)
        {
            EnsureId(createdTransactionId);
        }

        ParseStatus = status;
        ProcessedAt = processedAtUtc;
        ErrorCode = errorCode is null ? null : errorCode.Trim();
        UserId = userId;
        CreatedTransactionId = createdTransactionId;
        if (normalizedCommand is not null)
        {
            NormalizedCommand = normalizedCommand.Trim();
        }

        MarkUpdated(updatedAtUtc);
    }
}
