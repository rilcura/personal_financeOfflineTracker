namespace PersonalFinanceOfflineTracker.Workers.TelegramIngest.Models;

public sealed record ParsedAddCommandDto(
    decimal Amount,
    string Description,
    string Category,
    DateOnly TransactionDate
);
