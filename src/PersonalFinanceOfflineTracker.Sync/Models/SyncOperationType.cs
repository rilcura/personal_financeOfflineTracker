using System.Text.Json.Serialization;

namespace PersonalFinanceOfflineTracker.Sync.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SyncOperationType
{
    Upsert = 1,
    Delete = 2
}
