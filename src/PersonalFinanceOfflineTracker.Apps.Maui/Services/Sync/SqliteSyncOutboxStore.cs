using Microsoft.Data.Sqlite;
using PersonalFinanceOfflineTracker.Apps.Maui.Models.Sync;

namespace PersonalFinanceOfflineTracker.Apps.Maui.Services.Sync;

public sealed class SqliteSyncOutboxStore : ISyncOutboxStore
{
    private readonly string _connectionString;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _initialized;

    public SqliteSyncOutboxStore()
    {
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "local_sync.db");
        _connectionString = $"Data Source={dbPath}";
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_initialized)
            {
                return;
            }

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var createSql = """
                CREATE TABLE IF NOT EXISTS OutboxItems (
                    Id TEXT PRIMARY KEY,
                    EntityType TEXT NOT NULL,
                    EntityId TEXT NOT NULL,
                    Operation TEXT NOT NULL,
                    PayloadJson TEXT NOT NULL,
                    AttemptCount INTEGER NOT NULL,
                    NextAttemptAtUtc TEXT NOT NULL,
                    LastError TEXT NULL,
                    CreatedAtUtc TEXT NOT NULL,
                    UpdatedAtUtc TEXT NOT NULL,
                    Status TEXT NOT NULL
                );
                CREATE INDEX IF NOT EXISTS IX_OutboxItems_Status_NextAttempt
                    ON OutboxItems(Status, NextAttemptAtUtc);
                """;

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = createSql;
            await cmd.ExecuteNonQueryAsync(cancellationToken);

            _initialized = true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task EnqueueAsync(LocalOutboxItem item, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO OutboxItems
            (Id, EntityType, EntityId, Operation, PayloadJson, AttemptCount, NextAttemptAtUtc, LastError, CreatedAtUtc, UpdatedAtUtc, Status)
            VALUES
            ($id, $entityType, $entityId, $operation, $payloadJson, $attemptCount, $nextAttemptAtUtc, $lastError, $createdAtUtc, $updatedAtUtc, $status);
            """;

        cmd.Parameters.AddWithValue("$id", item.Id);
        cmd.Parameters.AddWithValue("$entityType", item.EntityType);
        cmd.Parameters.AddWithValue("$entityId", item.EntityId);
        cmd.Parameters.AddWithValue("$operation", item.Operation);
        cmd.Parameters.AddWithValue("$payloadJson", item.PayloadJson);
        cmd.Parameters.AddWithValue("$attemptCount", item.AttemptCount);
        cmd.Parameters.AddWithValue("$nextAttemptAtUtc", item.NextAttemptAtUtc.ToString("O"));
        cmd.Parameters.AddWithValue("$lastError", (object?)item.LastError ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$createdAtUtc", item.CreatedAtUtc.ToString("O"));
        cmd.Parameters.AddWithValue("$updatedAtUtc", item.UpdatedAtUtc.ToString("O"));
        cmd.Parameters.AddWithValue("$status", item.Status);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LocalOutboxItem>> LeasePendingBatchAsync(
        int maxItems,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);

        var leased = new List<LocalOutboxItem>(capacity: maxItems);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var tx = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        var select = connection.CreateCommand();
        select.Transaction = tx;
        select.CommandText = """
            SELECT Id, EntityType, EntityId, Operation, PayloadJson, AttemptCount, NextAttemptAtUtc, LastError, CreatedAtUtc, UpdatedAtUtc, Status
            FROM OutboxItems
            WHERE Status = $pending
              AND NextAttemptAtUtc <= $now
            ORDER BY CreatedAtUtc
            LIMIT $limit;
            """;
        select.Parameters.AddWithValue("$pending", OutboxStatus.Pending);
        select.Parameters.AddWithValue("$now", nowUtc.ToString("O"));
        select.Parameters.AddWithValue("$limit", maxItems);

        await using (var reader = await select.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                leased.Add(new LocalOutboxItem
                {
                    Id = reader.GetString(0),
                    EntityType = reader.GetString(1),
                    EntityId = reader.GetString(2),
                    Operation = reader.GetString(3),
                    PayloadJson = reader.GetString(4),
                    AttemptCount = reader.GetInt32(5),
                    NextAttemptAtUtc = DateTime.Parse(reader.GetString(6), null, System.Globalization.DateTimeStyles.RoundtripKind),
                    LastError = reader.IsDBNull(7) ? null : reader.GetString(7),
                    CreatedAtUtc = DateTime.Parse(reader.GetString(8), null, System.Globalization.DateTimeStyles.RoundtripKind),
                    UpdatedAtUtc = DateTime.Parse(reader.GetString(9), null, System.Globalization.DateTimeStyles.RoundtripKind),
                    Status = reader.GetString(10),
                });
            }
        }

        if (leased.Count > 0)
        {
            var update = connection.CreateCommand();
            update.Transaction = tx;
            update.CommandText = """
                UPDATE OutboxItems
                SET Status = $processing,
                    UpdatedAtUtc = $updatedAtUtc
                WHERE Id = $id;
                """;
            var idParam = update.CreateParameter();
            idParam.ParameterName = "$id";
            update.Parameters.Add(idParam);
            update.Parameters.AddWithValue("$processing", OutboxStatus.Processing);
            update.Parameters.AddWithValue("$updatedAtUtc", nowUtc.ToString("O"));

            foreach (var item in leased)
            {
                idParam.Value = item.Id;
                await update.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        await tx.CommitAsync(cancellationToken);
        return leased;
    }

    public async Task MarkSucceededAsync(IEnumerable<string> itemIds, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var tx = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "DELETE FROM OutboxItems WHERE Id = $id;";
        var idParam = cmd.CreateParameter();
        idParam.ParameterName = "$id";
        cmd.Parameters.Add(idParam);

        foreach (var id in itemIds)
        {
            idParam.Value = id;
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        await tx.CommitAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(string itemId, string error, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var getAttemptCmd = connection.CreateCommand();
        getAttemptCmd.CommandText = "SELECT AttemptCount FROM OutboxItems WHERE Id = $id;";
        getAttemptCmd.Parameters.AddWithValue("$id", itemId);
        var attemptObj = await getAttemptCmd.ExecuteScalarAsync(cancellationToken);
        if (attemptObj is null || attemptObj == DBNull.Value)
        {
            return;
        }

        var attempt = Convert.ToInt32(attemptObj) + 1;
        var nextAttempt = nowUtc.AddSeconds(Math.Min(Math.Pow(2, attempt), 300));

        var cmd = connection.CreateCommand();
        cmd.CommandText = """
            UPDATE OutboxItems
            SET AttemptCount = $attemptCount,
                NextAttemptAtUtc = $nextAttemptAtUtc,
                LastError = $lastError,
                UpdatedAtUtc = $updatedAtUtc,
                Status = $status
            WHERE Id = $id;
            """;
        cmd.Parameters.AddWithValue("$attemptCount", attempt);
        cmd.Parameters.AddWithValue("$nextAttemptAtUtc", nextAttempt.ToString("O"));
        cmd.Parameters.AddWithValue("$lastError", error);
        cmd.Parameters.AddWithValue("$updatedAtUtc", nowUtc.ToString("O"));
        cmd.Parameters.AddWithValue("$status", OutboxStatus.Pending);
        cmd.Parameters.AddWithValue("$id", itemId);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
