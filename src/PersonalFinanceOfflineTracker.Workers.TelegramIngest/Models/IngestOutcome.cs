namespace PersonalFinanceOfflineTracker.Workers.TelegramIngest.Models;

public sealed record IngestOutcome(
    IngestStatus Status,
    string ResponseMessage,
    string? ErrorCode = null,
    string? TransactionId = null
);
