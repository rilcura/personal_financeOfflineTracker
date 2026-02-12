using Microsoft.Data.Sqlite;
using PersonalFinanceOfflineTracker.Apps.Maui.Models.Ledger;
using PersonalFinanceOfflineTracker.Sync.Models;

namespace PersonalFinanceOfflineTracker.Apps.Maui.Services.Ledger;

public sealed class SqliteLocalLedgerStore : ILocalLedgerStore
{
    private readonly string _connectionString;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _initialized;

    public SqliteLocalLedgerStore()
    {
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "local_ledger.db");
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
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS LocalCategories (
                    Id TEXT PRIMARY KEY,
                    UserId TEXT NOT NULL,
                    Name TEXT NOT NULL,
                    NormalizedName TEXT NOT NULL,
                    CreatedAtUtc TEXT NOT NULL,
                    UpdatedAtUtc TEXT NOT NULL,
                    IsDeleted INTEGER NOT NULL,
                    DeletedAtUtc TEXT NULL,
                    Source TEXT NOT NULL
                );
                CREATE INDEX IF NOT EXISTS IX_LocalCategories_User ON LocalCategories(UserId);

                CREATE TABLE IF NOT EXISTS LocalTransactions (
                    Id TEXT PRIMARY KEY,
                    UserId TEXT NOT NULL,
                    CategoryId TEXT NULL,
                    Amount TEXT NOT NULL,
                    Description TEXT NOT NULL,
                    TransactionDate TEXT NOT NULL,
                    CreatedAtUtc TEXT NOT NULL,
                    UpdatedAtUtc TEXT NOT NULL,
                    IsDeleted INTEGER NOT NULL,
                    DeletedAtUtc TEXT NULL,
                    Source TEXT NOT NULL
                );
                CREATE INDEX IF NOT EXISTS IX_LocalTransactions_UserDate ON LocalTransactions(UserId, TransactionDate);

                CREATE TABLE IF NOT EXISTS LocalSyncMetadata (
                    UserId TEXT PRIMARY KEY,
                    Cursor TEXT NOT NULL,
                    UpdatedAtUtc TEXT NOT NULL
                );
                """;
            await cmd.ExecuteNonQueryAsync(cancellationToken);

            _initialized = true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<LocalCategoryRecord?> GetCategoryByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT Id, UserId, Name, NormalizedName, CreatedAtUtc, UpdatedAtUtc, IsDeleted, DeletedAtUtc, Source
            FROM LocalCategories
            WHERE Id = $id;
            """;
        cmd.Parameters.AddWithValue("$id", id);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return ReadCategory(reader);
    }

    public async Task<LocalTransactionRecord?> GetTransactionByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT Id, UserId, CategoryId, Amount, Description, TransactionDate, CreatedAtUtc, UpdatedAtUtc, IsDeleted, DeletedAtUtc, Source
            FROM LocalTransactions
            WHERE Id = $id;
            """;
        cmd.Parameters.AddWithValue("$id", id);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return ReadTransaction(reader);
    }

    public async Task<IReadOnlyList<LocalCategoryRecord>> ListCategoriesAsync(
        string userId,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        var result = new List<LocalCategoryRecord>();
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = includeDeleted
            ? """
                SELECT Id, UserId, Name, NormalizedName, CreatedAtUtc, UpdatedAtUtc, IsDeleted, DeletedAtUtc, Source
                FROM LocalCategories
                WHERE UserId = $userId
                ORDER BY Name;
                """
            : """
                SELECT Id, UserId, Name, NormalizedName, CreatedAtUtc, UpdatedAtUtc, IsDeleted, DeletedAtUtc, Source
                FROM LocalCategories
                WHERE UserId = $userId AND IsDeleted = 0
                ORDER BY Name;
                """;
        cmd.Parameters.AddWithValue("$userId", userId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(ReadCategory(reader));
        }

        return result;
    }

    public async Task<IReadOnlyList<LocalTransactionRecord>> ListTransactionsAsync(
        string userId,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        var result = new List<LocalTransactionRecord>();
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = includeDeleted
            ? """
                SELECT Id, UserId, CategoryId, Amount, Description, TransactionDate, CreatedAtUtc, UpdatedAtUtc, IsDeleted, DeletedAtUtc, Source
                FROM LocalTransactions
                WHERE UserId = $userId
                ORDER BY TransactionDate DESC, UpdatedAtUtc DESC;
                """
            : """
                SELECT Id, UserId, CategoryId, Amount, Description, TransactionDate, CreatedAtUtc, UpdatedAtUtc, IsDeleted, DeletedAtUtc, Source
                FROM LocalTransactions
                WHERE UserId = $userId AND IsDeleted = 0
                ORDER BY TransactionDate DESC, UpdatedAtUtc DESC;
                """;
        cmd.Parameters.AddWithValue("$userId", userId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(ReadTransaction(reader));
        }

        return result;
    }

    public async Task UpsertCategoryAsync(LocalCategoryRecord category, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO LocalCategories (Id, UserId, Name, NormalizedName, CreatedAtUtc, UpdatedAtUtc, IsDeleted, DeletedAtUtc, Source)
            VALUES ($id, $userId, $name, $normalizedName, $createdAtUtc, $updatedAtUtc, $isDeleted, $deletedAtUtc, $source)
            ON CONFLICT(Id) DO UPDATE SET
                UserId = excluded.UserId,
                Name = excluded.Name,
                NormalizedName = excluded.NormalizedName,
                CreatedAtUtc = excluded.CreatedAtUtc,
                UpdatedAtUtc = excluded.UpdatedAtUtc,
                IsDeleted = excluded.IsDeleted,
                DeletedAtUtc = excluded.DeletedAtUtc,
                Source = excluded.Source;
            """;
        MapCategoryParams(cmd, category);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpsertTransactionAsync(LocalTransactionRecord transaction, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO LocalTransactions (Id, UserId, CategoryId, Amount, Description, TransactionDate, CreatedAtUtc, UpdatedAtUtc, IsDeleted, DeletedAtUtc, Source)
            VALUES ($id, $userId, $categoryId, $amount, $description, $transactionDate, $createdAtUtc, $updatedAtUtc, $isDeleted, $deletedAtUtc, $source)
            ON CONFLICT(Id) DO UPDATE SET
                UserId = excluded.UserId,
                CategoryId = excluded.CategoryId,
                Amount = excluded.Amount,
                Description = excluded.Description,
                TransactionDate = excluded.TransactionDate,
                CreatedAtUtc = excluded.CreatedAtUtc,
                UpdatedAtUtc = excluded.UpdatedAtUtc,
                IsDeleted = excluded.IsDeleted,
                DeletedAtUtc = excluded.DeletedAtUtc,
                Source = excluded.Source;
            """;
        MapTransactionParams(cmd, transaction);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task SoftDeleteCategoryAsync(string id, DateTime deletedAtUtc, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            UPDATE LocalCategories
            SET IsDeleted = 1, DeletedAtUtc = $deletedAtUtc, UpdatedAtUtc = $updatedAtUtc
            WHERE Id = $id;
            """;
        cmd.Parameters.AddWithValue("$deletedAtUtc", deletedAtUtc.ToString("O"));
        cmd.Parameters.AddWithValue("$updatedAtUtc", deletedAtUtc.ToString("O"));
        cmd.Parameters.AddWithValue("$id", id);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task SoftDeleteTransactionAsync(string id, DateTime deletedAtUtc, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            UPDATE LocalTransactions
            SET IsDeleted = 1, DeletedAtUtc = $deletedAtUtc, UpdatedAtUtc = $updatedAtUtc
            WHERE Id = $id;
            """;
        cmd.Parameters.AddWithValue("$deletedAtUtc", deletedAtUtc.ToString("O"));
        cmd.Parameters.AddWithValue("$updatedAtUtc", deletedAtUtc.ToString("O"));
        cmd.Parameters.AddWithValue("$id", id);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<string?> GetSyncCursorAsync(string userId, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Cursor FROM LocalSyncMetadata WHERE UserId = $userId;";
        cmd.Parameters.AddWithValue("$userId", userId);
        var value = await cmd.ExecuteScalarAsync(cancellationToken);
        return value is null || value == DBNull.Value ? null : Convert.ToString(value);
    }

    public async Task SetSyncCursorAsync(string userId, string cursor, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO LocalSyncMetadata (UserId, Cursor, UpdatedAtUtc)
            VALUES ($userId, $cursor, $updatedAtUtc)
            ON CONFLICT(UserId) DO UPDATE SET
                Cursor = excluded.Cursor,
                UpdatedAtUtc = excluded.UpdatedAtUtc;
            """;
        cmd.Parameters.AddWithValue("$userId", userId);
        cmd.Parameters.AddWithValue("$cursor", cursor);
        cmd.Parameters.AddWithValue("$updatedAtUtc", DateTime.UtcNow.ToString("O"));
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task ApplySyncChangesAsync(
        string userId,
        IReadOnlyList<SyncEntityStateDto> changes,
        string nextCursor,
        CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var tx = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        foreach (var change in changes.Where(x => x.UserId == userId))
        {
            if (change.EntityType == SyncEntityType.Category && change.Category is not null)
            {
                var c = change.Category;
                var local = new LocalCategoryRecord
                {
                    Id = c.Id,
                    UserId = c.UserId,
                    Name = c.Name,
                    NormalizedName = c.NormalizedName,
                    CreatedAtUtc = EnsureUtc(c.CreatedAt),
                    UpdatedAtUtc = EnsureUtc(c.UpdatedAt),
                    IsDeleted = c.IsDeleted,
                    DeletedAtUtc = c.DeletedAt is null ? null : EnsureUtc(c.DeletedAt.Value),
                    Source = c.Source
                };

                var cmd = connection.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = """
                    INSERT INTO LocalCategories (Id, UserId, Name, NormalizedName, CreatedAtUtc, UpdatedAtUtc, IsDeleted, DeletedAtUtc, Source)
                    VALUES ($id, $userId, $name, $normalizedName, $createdAtUtc, $updatedAtUtc, $isDeleted, $deletedAtUtc, $source)
                    ON CONFLICT(Id) DO UPDATE SET
                        UserId = excluded.UserId,
                        Name = excluded.Name,
                        NormalizedName = excluded.NormalizedName,
                        CreatedAtUtc = excluded.CreatedAtUtc,
                        UpdatedAtUtc = excluded.UpdatedAtUtc,
                        IsDeleted = excluded.IsDeleted,
                        DeletedAtUtc = excluded.DeletedAtUtc,
                        Source = excluded.Source;
                    """;
                MapCategoryParams(cmd, local);
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            else if (change.EntityType == SyncEntityType.Transaction && change.Transaction is not null)
            {
                var t = change.Transaction;
                var local = new LocalTransactionRecord
                {
                    Id = t.Id,
                    UserId = t.UserId,
                    CategoryId = t.CategoryId,
                    Amount = t.Amount,
                    Description = t.Description,
                    TransactionDate = t.TransactionDate,
                    CreatedAtUtc = EnsureUtc(t.CreatedAt),
                    UpdatedAtUtc = EnsureUtc(t.UpdatedAt),
                    IsDeleted = t.IsDeleted,
                    DeletedAtUtc = t.DeletedAt is null ? null : EnsureUtc(t.DeletedAt.Value),
                    Source = t.Source
                };

                var cmd = connection.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = """
                    INSERT INTO LocalTransactions (Id, UserId, CategoryId, Amount, Description, TransactionDate, CreatedAtUtc, UpdatedAtUtc, IsDeleted, DeletedAtUtc, Source)
                    VALUES ($id, $userId, $categoryId, $amount, $description, $transactionDate, $createdAtUtc, $updatedAtUtc, $isDeleted, $deletedAtUtc, $source)
                    ON CONFLICT(Id) DO UPDATE SET
                        UserId = excluded.UserId,
                        CategoryId = excluded.CategoryId,
                        Amount = excluded.Amount,
                        Description = excluded.Description,
                        TransactionDate = excluded.TransactionDate,
                        CreatedAtUtc = excluded.CreatedAtUtc,
                        UpdatedAtUtc = excluded.UpdatedAtUtc,
                        IsDeleted = excluded.IsDeleted,
                        DeletedAtUtc = excluded.DeletedAtUtc,
                        Source = excluded.Source;
                    """;
                MapTransactionParams(cmd, local);
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        var meta = connection.CreateCommand();
        meta.Transaction = tx;
        meta.CommandText = """
            INSERT INTO LocalSyncMetadata (UserId, Cursor, UpdatedAtUtc)
            VALUES ($userId, $cursor, $updatedAtUtc)
            ON CONFLICT(UserId) DO UPDATE SET
                Cursor = excluded.Cursor,
                UpdatedAtUtc = excluded.UpdatedAtUtc;
            """;
        meta.Parameters.AddWithValue("$userId", userId);
        meta.Parameters.AddWithValue("$cursor", nextCursor);
        meta.Parameters.AddWithValue("$updatedAtUtc", DateTime.UtcNow.ToString("O"));
        await meta.ExecuteNonQueryAsync(cancellationToken);

        await tx.CommitAsync(cancellationToken);
    }

    private static LocalCategoryRecord ReadCategory(SqliteDataReader reader)
    {
        return new LocalCategoryRecord
        {
            Id = reader.GetString(0),
            UserId = reader.GetString(1),
            Name = reader.GetString(2),
            NormalizedName = reader.GetString(3),
            CreatedAtUtc = DateTime.Parse(reader.GetString(4), null, System.Globalization.DateTimeStyles.RoundtripKind),
            UpdatedAtUtc = DateTime.Parse(reader.GetString(5), null, System.Globalization.DateTimeStyles.RoundtripKind),
            IsDeleted = reader.GetInt32(6) == 1,
            DeletedAtUtc = reader.IsDBNull(7) ? null : DateTime.Parse(reader.GetString(7), null, System.Globalization.DateTimeStyles.RoundtripKind),
            Source = reader.GetString(8),
        };
    }

    private static LocalTransactionRecord ReadTransaction(SqliteDataReader reader)
    {
        return new LocalTransactionRecord
        {
            Id = reader.GetString(0),
            UserId = reader.GetString(1),
            CategoryId = reader.IsDBNull(2) ? null : reader.GetString(2),
            Amount = decimal.Parse(reader.GetString(3), System.Globalization.CultureInfo.InvariantCulture),
            Description = reader.GetString(4),
            TransactionDate = DateOnly.Parse(reader.GetString(5), System.Globalization.CultureInfo.InvariantCulture),
            CreatedAtUtc = DateTime.Parse(reader.GetString(6), null, System.Globalization.DateTimeStyles.RoundtripKind),
            UpdatedAtUtc = DateTime.Parse(reader.GetString(7), null, System.Globalization.DateTimeStyles.RoundtripKind),
            IsDeleted = reader.GetInt32(8) == 1,
            DeletedAtUtc = reader.IsDBNull(9) ? null : DateTime.Parse(reader.GetString(9), null, System.Globalization.DateTimeStyles.RoundtripKind),
            Source = reader.GetString(10),
        };
    }

    private static void MapCategoryParams(SqliteCommand cmd, LocalCategoryRecord category)
    {
        cmd.Parameters.AddWithValue("$id", category.Id);
        cmd.Parameters.AddWithValue("$userId", category.UserId);
        cmd.Parameters.AddWithValue("$name", category.Name);
        cmd.Parameters.AddWithValue("$normalizedName", category.NormalizedName);
        cmd.Parameters.AddWithValue("$createdAtUtc", category.CreatedAtUtc.ToString("O"));
        cmd.Parameters.AddWithValue("$updatedAtUtc", category.UpdatedAtUtc.ToString("O"));
        cmd.Parameters.AddWithValue("$isDeleted", category.IsDeleted ? 1 : 0);
        cmd.Parameters.AddWithValue("$deletedAtUtc", (object?)category.DeletedAtUtc?.ToString("O") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$source", category.Source);
    }

    private static void MapTransactionParams(SqliteCommand cmd, LocalTransactionRecord transaction)
    {
        cmd.Parameters.AddWithValue("$id", transaction.Id);
        cmd.Parameters.AddWithValue("$userId", transaction.UserId);
        cmd.Parameters.AddWithValue("$categoryId", (object?)transaction.CategoryId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$amount", transaction.Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
        cmd.Parameters.AddWithValue("$description", transaction.Description);
        cmd.Parameters.AddWithValue("$transactionDate", transaction.TransactionDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
        cmd.Parameters.AddWithValue("$createdAtUtc", transaction.CreatedAtUtc.ToString("O"));
        cmd.Parameters.AddWithValue("$updatedAtUtc", transaction.UpdatedAtUtc.ToString("O"));
        cmd.Parameters.AddWithValue("$isDeleted", transaction.IsDeleted ? 1 : 0);
        cmd.Parameters.AddWithValue("$deletedAtUtc", (object?)transaction.DeletedAtUtc?.ToString("O") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$source", transaction.Source);
    }

    private static DateTime EnsureUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
    }
}
