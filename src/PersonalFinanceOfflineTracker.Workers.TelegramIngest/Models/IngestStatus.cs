namespace PersonalFinanceOfflineTracker.Workers.TelegramIngest.Models;

public enum IngestStatus
{
    Parsed = 1,
    Rejected = 2,
    Duplicate = 3,
    Failed = 4
}
