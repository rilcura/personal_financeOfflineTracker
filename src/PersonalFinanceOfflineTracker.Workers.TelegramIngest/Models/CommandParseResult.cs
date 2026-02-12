namespace PersonalFinanceOfflineTracker.Workers.TelegramIngest.Models;

public sealed record CommandParseResult(
    bool IsSuccess,
    ParsedCommandKind CommandKind,
    ParsedAddCommandDto? AddCommand,
    string? ErrorCode,
    string ResponseMessage,
    string? NormalizedCommand
);
