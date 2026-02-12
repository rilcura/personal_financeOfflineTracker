namespace PersonalFinanceOfflineTracker.Domain.Models;

public enum IngestedMessageParseStatus
{
    Parsed = 0,
    Rejected = 1,
    Duplicate = 2,
    Failed = 3,
}
