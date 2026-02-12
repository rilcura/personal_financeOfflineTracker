using System.Text.Json.Serialization;

namespace PersonalFinanceOfflineTracker.Sync.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SyncEntityType
{
    Transaction = 1,
    Category = 2
}
