using Akinci.Application.Ports;
using Akinci.Domain.Entities;
using Akinci.Domain.Enums;
using Akinci.Domain.ValueObjects;
using Microsoft.Data.Sqlite;

namespace Akinci.Infrastructure.Persistence;

/// <summary>
/// Microsoft.Data.Sqlite kullanarak WAL modunda, transaction-safe ve yüksek eşzamanlı kalıcı SQLite deposu.
/// </summary>
public sealed class SqliteDownloadRepository : IDownloadRepository, IAsyncDisposable
{
    private readonly string _connectionString;
    private bool _initialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public SqliteDownloadRepository(string databasePath)
    {
        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized) return;

        await _initLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized) return;

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            // WAL (Write-Ahead Logging) ve performans PRAGMA'ları
            await using (var pragmaCmd = connection.CreateCommand())
            {
                pragmaCmd.CommandText = @"
                    PRAGMA journal_mode = WAL;
                    PRAGMA synchronous = NORMAL;
                    PRAGMA busy_timeout = 5000;
                    PRAGMA foreign_keys = ON;
                ";
                await pragmaCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            // Tabloları oluştur
            await using (var createCmd = connection.CreateCommand())
            {
                createCmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS downloads (
                        id TEXT PRIMARY KEY,
                        url TEXT NOT NULL,
                        destination_file_path TEXT NOT NULL,
                        total_bytes INTEGER,
                        supports_range INTEGER NOT NULL,
                        status INTEGER NOT NULL,
                        created_at TEXT NOT NULL,
                        started_at TEXT,
                        paused_at TEXT,
                        completed_at TEXT,
                        failure_reason TEXT,
                        sha256_hash TEXT,
                        auth_type INTEGER NOT NULL,
                        auth_username TEXT,
                        auth_password TEXT,
                        auth_domain TEXT,
                        auth_use_default_credentials INTEGER NOT NULL,
                        session_cookies TEXT,
                        session_referer TEXT,
                        session_user_agent TEXT,
                        vt_is_scanned INTEGER,
                        vt_malicious INTEGER,
                        vt_suspicious INTEGER,
                        vt_harmless INTEGER,
                        vt_undetected INTEGER,
                        vt_scan_url TEXT,
                        vt_custom_message TEXT
                    );

                    CREATE TABLE IF NOT EXISTS chunks (
                        id TEXT PRIMARY KEY,
                        task_id TEXT NOT NULL,
                        chunk_index INTEGER NOT NULL,
                        start_offset INTEGER NOT NULL,
                        end_offset INTEGER NOT NULL,
                        current_offset INTEGER NOT NULL,
                        FOREIGN KEY (task_id) REFERENCES downloads(id) ON DELETE CASCADE
                    );

                    CREATE INDEX IF NOT EXISTS idx_chunks_task_id ON chunks(task_id);
                ";
                await createCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            // Mevcut veritabanı şeması için güvenli kolon genişletmeleri
            try
            {
                await using var alterCmd = connection.CreateCommand();
                alterCmd.CommandText = @"
                    ALTER TABLE downloads ADD COLUMN session_cookies TEXT;
                    ALTER TABLE downloads ADD COLUMN session_referer TEXT;
                    ALTER TABLE downloads ADD COLUMN session_user_agent TEXT;
                ";
                await alterCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Kolonlar zaten mevcutsa SQLite hata fırlatır, yoksay
            }

            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task AddAsync(DownloadTask task, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = connection.BeginTransaction();

        await using (var cmd = connection.CreateCommand())
        {
            cmd.Transaction = transaction;
            cmd.CommandText = @"
                INSERT INTO downloads (
                    id, url, destination_file_path, total_bytes, supports_range, status,
                    created_at, started_at, paused_at, completed_at, failure_reason,
                    sha256_hash, auth_type, auth_username, auth_password, auth_domain,
                    auth_use_default_credentials, session_cookies, session_referer, session_user_agent,
                    vt_is_scanned, vt_malicious, vt_suspicious,
                    vt_harmless, vt_undetected, vt_scan_url, vt_custom_message
                ) VALUES (
                    $id, $url, $destination_file_path, $total_bytes, $supports_range, $status,
                    $created_at, $started_at, $paused_at, $completed_at, $failure_reason,
                    $sha256_hash, $auth_type, $auth_username, $auth_password, $auth_domain,
                    $auth_use_default_credentials, $session_cookies, $session_referer, $session_user_agent,
                    $vt_is_scanned, $vt_malicious, $vt_suspicious,
                    $vt_harmless, $vt_undetected, $vt_scan_url, $vt_custom_message
                );
            ";
            BindTaskParameters(cmd, task);
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        // Chunk'ları ekle
        foreach (var chunk in task.Chunks)
        {
            await using var chunkCmd = connection.CreateCommand();
            chunkCmd.Transaction = transaction;
            chunkCmd.CommandText = @"
                INSERT INTO chunks (id, task_id, chunk_index, start_offset, end_offset, current_offset)
                VALUES ($id, $task_id, $chunk_index, $start_offset, $end_offset, $current_offset);
            ";
            chunkCmd.Parameters.AddWithValue("$id", chunk.Id.ToString());
            chunkCmd.Parameters.AddWithValue("$task_id", task.Id.ToString());
            chunkCmd.Parameters.AddWithValue("$chunk_index", chunk.Index);
            chunkCmd.Parameters.AddWithValue("$start_offset", chunk.Range.Start);
            chunkCmd.Parameters.AddWithValue("$end_offset", chunk.Range.End);
            chunkCmd.Parameters.AddWithValue("$current_offset", chunk.CurrentOffset);

            await chunkCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(DownloadTask task, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = connection.BeginTransaction();

        await using (var cmd = connection.CreateCommand())
        {
            cmd.Transaction = transaction;
            cmd.CommandText = @"
                UPDATE downloads SET
                    destination_file_path = $destination_file_path,
                    total_bytes = $total_bytes,
                    supports_range = $supports_range,
                    status = $status,
                    started_at = $started_at,
                    paused_at = $paused_at,
                    completed_at = $completed_at,
                    failure_reason = $failure_reason,
                    sha256_hash = $sha256_hash,
                    auth_type = $auth_type,
                    auth_username = $auth_username,
                    auth_password = $auth_password,
                    auth_domain = $auth_domain,
                    auth_use_default_credentials = $auth_use_default_credentials,
                    session_cookies = $session_cookies,
                    session_referer = $session_referer,
                    session_user_agent = $session_user_agent,
                    vt_is_scanned = $vt_is_scanned,
                    vt_malicious = $vt_malicious,
                    vt_suspicious = $vt_suspicious,
                    vt_harmless = $vt_harmless,
                    vt_undetected = $vt_undetected,
                    vt_scan_url = $vt_scan_url,
                    vt_custom_message = $vt_custom_message
                WHERE id = $id;
            ";
            BindTaskParameters(cmd, task);
            await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        // Chunk ofsetlerini ve dinamik olarak eklenen/bölünen parçaları güncelle (Work Stealing uyumluluğu)
        foreach (var chunk in task.Chunks)
        {
            await using var chunkCmd = connection.CreateCommand();
            chunkCmd.Transaction = transaction;
            chunkCmd.CommandText = @"
                INSERT INTO chunks (id, task_id, chunk_index, start_offset, end_offset, current_offset)
                VALUES ($id, $task_id, $chunk_index, $start_offset, $end_offset, $current_offset)
                ON CONFLICT(id) DO UPDATE SET
                    start_offset = excluded.start_offset,
                    end_offset = excluded.end_offset,
                    current_offset = excluded.current_offset;
            ";
            chunkCmd.Parameters.AddWithValue("$id", chunk.Id.ToString());
            chunkCmd.Parameters.AddWithValue("$task_id", task.Id.ToString());
            chunkCmd.Parameters.AddWithValue("$chunk_index", chunk.Index);
            chunkCmd.Parameters.AddWithValue("$start_offset", chunk.Range.Start);
            chunkCmd.Parameters.AddWithValue("$end_offset", chunk.Range.End);
            chunkCmd.Parameters.AddWithValue("$current_offset", chunk.CurrentOffset);

            await chunkCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateChunkOffsetAsync(Guid taskId, int chunkIndex, long newOffset, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            UPDATE chunks SET current_offset = $current_offset
            WHERE task_id = $task_id AND chunk_index = $chunk_index;
        ";
        cmd.Parameters.AddWithValue("$current_offset", newOffset);
        cmd.Parameters.AddWithValue("$task_id", taskId.ToString());
        cmd.Parameters.AddWithValue("$chunk_index", chunkIndex);

        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<DownloadTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        DownloadTask? task = null;

        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT * FROM downloads WHERE id = $id;";
            cmd.Parameters.AddWithValue("$id", id.ToString());

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                task = MapTaskFromReader(reader);
            }
        }

        if (task == null) return null;

        // Chunk'ları yükle
        var chunks = new List<Chunk>();
        await using (var chunkCmd = connection.CreateCommand())
        {
            chunkCmd.CommandText = "SELECT * FROM chunks WHERE task_id = $task_id ORDER BY chunk_index ASC;";
            chunkCmd.Parameters.AddWithValue("$task_id", id.ToString());

            await using var chunkReader = await chunkCmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await chunkReader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var chunkId = Guid.Parse(chunkReader.GetString(chunkReader.GetOrdinal("id")));
                var chunkIndex = chunkReader.GetInt32(chunkReader.GetOrdinal("chunk_index"));
                var startOffset = chunkReader.GetInt64(chunkReader.GetOrdinal("start_offset"));
                var endOffset = chunkReader.GetInt64(chunkReader.GetOrdinal("end_offset"));
                var currentOffset = chunkReader.GetInt64(chunkReader.GetOrdinal("current_offset"));

                var range = new ByteRange(startOffset, endOffset);
                chunks.Add(new Chunk(chunkId, id, chunkIndex, range, currentOffset));
            }
        }

        task.LoadChunks(chunks);
        return task;
    }

    public async Task<IReadOnlyList<DownloadTask>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var tasks = new List<DownloadTask>();

        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT * FROM downloads ORDER BY created_at DESC;";
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                tasks.Add(MapTaskFromReader(reader));
            }
        }

        // Tüm chunk'ları tek seferde alıp ilişkilendir
        foreach (var task in tasks)
        {
            await using var chunkCmd = connection.CreateCommand();
            chunkCmd.CommandText = "SELECT * FROM chunks WHERE task_id = $task_id ORDER BY chunk_index ASC;";
            chunkCmd.Parameters.AddWithValue("$task_id", task.Id.ToString());

            var chunks = new List<Chunk>();
            await using var chunkReader = await chunkCmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await chunkReader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var chunkId = Guid.Parse(chunkReader.GetString(chunkReader.GetOrdinal("id")));
                var chunkIndex = chunkReader.GetInt32(chunkReader.GetOrdinal("chunk_index"));
                var startOffset = chunkReader.GetInt64(chunkReader.GetOrdinal("start_offset"));
                var endOffset = chunkReader.GetInt64(chunkReader.GetOrdinal("end_offset"));
                var currentOffset = chunkReader.GetInt64(chunkReader.GetOrdinal("current_offset"));

                var range = new ByteRange(startOffset, endOffset);
                chunks.Add(new Chunk(chunkId, task.Id, chunkIndex, range, currentOffset));
            }

            task.LoadChunks(chunks);
        }

        return tasks;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM downloads WHERE id = $id;";
        cmd.Parameters.AddWithValue("$id", id.ToString());

        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void BindTaskParameters(SqliteCommand cmd, DownloadTask task)
    {
        cmd.Parameters.AddWithValue("$id", task.Id.ToString());
        cmd.Parameters.AddWithValue("$url", task.Url.Value);
        cmd.Parameters.AddWithValue("$destination_file_path", task.DestinationFilePath);
        cmd.Parameters.AddWithValue("$total_bytes", (object?)task.TotalBytes ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$supports_range", task.SupportsRangeRequests ? 1 : 0);
        cmd.Parameters.AddWithValue("$status", (int)task.Status);
        cmd.Parameters.AddWithValue("$created_at", task.CreatedAt.ToString("O"));
        cmd.Parameters.AddWithValue("$started_at", (object?)task.StartedAt?.ToString("O") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$paused_at", (object?)task.PausedAt?.ToString("O") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$completed_at", (object?)task.CompletedAt?.ToString("O") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$failure_reason", (object?)task.FailureReason ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$sha256_hash", (object?)task.Sha256Hash ?? DBNull.Value);

        // Kimlik bilgileri
        if (task.Credentials != null)
        {
            cmd.Parameters.AddWithValue("$auth_type", (int)task.Credentials.Type);
            cmd.Parameters.AddWithValue("$auth_username", (object?)task.Credentials.UserName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$auth_password", (object?)task.Credentials.Password ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$auth_domain", (object?)task.Credentials.Domain ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$auth_use_default_credentials", task.Credentials.UseDefaultCredentials ? 1 : 0);
        }
        else
        {
            cmd.Parameters.AddWithValue("$auth_type", (int)AuthenticationType.None);
            cmd.Parameters.AddWithValue("$auth_username", DBNull.Value);
            cmd.Parameters.AddWithValue("$auth_password", DBNull.Value);
            cmd.Parameters.AddWithValue("$auth_domain", DBNull.Value);
            cmd.Parameters.AddWithValue("$auth_use_default_credentials", 0);
        }

        // Tarayıcı oturum ve çerez bilgileri
        cmd.Parameters.AddWithValue("$session_cookies", (object?)task.Credentials?.CookieHeader ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$session_referer", (object?)task.Credentials?.Referer ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$session_user_agent", (object?)task.Credentials?.UserAgent ?? DBNull.Value);

        // VirusTotal güvenlik bilgileri
        if (task.SecurityVerdict != null)
        {
            var v = task.SecurityVerdict;
            cmd.Parameters.AddWithValue("$vt_is_scanned", v.IsScanned ? 1 : 0);
            cmd.Parameters.AddWithValue("$vt_malicious", v.MaliciousVotes);
            cmd.Parameters.AddWithValue("$vt_suspicious", v.SuspiciousVotes);
            cmd.Parameters.AddWithValue("$vt_harmless", v.HarmlessVotes);
            cmd.Parameters.AddWithValue("$vt_undetected", v.UndetectedVotes);
            cmd.Parameters.AddWithValue("$vt_scan_url", (object?)v.ScanUrl ?? DBNull.Value);
            cmd.Parameters.AddWithValue("$vt_custom_message", (object?)v.CustomMessage ?? DBNull.Value);
        }
        else
        {
            cmd.Parameters.AddWithValue("$vt_is_scanned", DBNull.Value);
            cmd.Parameters.AddWithValue("$vt_malicious", DBNull.Value);
            cmd.Parameters.AddWithValue("$vt_suspicious", DBNull.Value);
            cmd.Parameters.AddWithValue("$vt_harmless", DBNull.Value);
            cmd.Parameters.AddWithValue("$vt_undetected", DBNull.Value);
            cmd.Parameters.AddWithValue("$vt_scan_url", DBNull.Value);
            cmd.Parameters.AddWithValue("$vt_custom_message", DBNull.Value);
        }
    }

    private static DownloadTask MapTaskFromReader(SqliteDataReader reader)
    {
        var id = Guid.Parse(reader.GetString(reader.GetOrdinal("id")));
        var url = DownloadUrl.Create(reader.GetString(reader.GetOrdinal("url")));
        var destPath = reader.GetString(reader.GetOrdinal("destination_file_path"));
        var totalBytes = reader.IsDBNull(reader.GetOrdinal("total_bytes")) ? (long?)null : reader.GetInt64(reader.GetOrdinal("total_bytes"));
        var supportsRange = reader.GetInt32(reader.GetOrdinal("supports_range")) == 1;
        var status = (DownloadStatus)reader.GetInt32(reader.GetOrdinal("status"));

        DownloadCredentials? creds = null;
        var authType = (AuthenticationType)reader.GetInt32(reader.GetOrdinal("auth_type"));
        if (authType != AuthenticationType.None)
        {
            var user = reader.IsDBNull(reader.GetOrdinal("auth_username")) ? null : reader.GetString(reader.GetOrdinal("auth_username"));
            var pass = reader.IsDBNull(reader.GetOrdinal("auth_password")) ? null : reader.GetString(reader.GetOrdinal("auth_password"));
            var domain = reader.IsDBNull(reader.GetOrdinal("auth_domain")) ? null : reader.GetString(reader.GetOrdinal("auth_domain"));
            var useDefault = reader.GetInt32(reader.GetOrdinal("auth_use_default_credentials")) == 1;

            if (useDefault)
            {
                creds = DownloadCredentials.WindowsDefault(authType);
            }
            else
            {
                creds = authType switch
                {
                    AuthenticationType.Ntlm => DownloadCredentials.Ntlm(user ?? "", pass ?? "", domain),
                    AuthenticationType.Kerberos => DownloadCredentials.Kerberos(user ?? "", pass ?? "", domain),
                    AuthenticationType.Negotiate => DownloadCredentials.Negotiate(user ?? "", pass ?? "", domain),
                    _ => DownloadCredentials.Basic(user ?? "", pass ?? "")
                };
            }
        }

        // Tarayıcı oturum ve çerez bilgilerini oku
        var cookiesOrdinal = GetOrdinalOrDefault(reader, "session_cookies");
        var refererOrdinal = GetOrdinalOrDefault(reader, "session_referer");
        var userAgentOrdinal = GetOrdinalOrDefault(reader, "session_user_agent");

        string? cookies = cookiesOrdinal >= 0 && !reader.IsDBNull(cookiesOrdinal) ? reader.GetString(cookiesOrdinal) : null;
        string? referer = refererOrdinal >= 0 && !reader.IsDBNull(refererOrdinal) ? reader.GetString(refererOrdinal) : null;
        string? userAgent = userAgentOrdinal >= 0 && !reader.IsDBNull(userAgentOrdinal) ? reader.GetString(userAgentOrdinal) : null;

        if (!string.IsNullOrEmpty(cookies) || !string.IsNullOrEmpty(referer) || !string.IsNullOrEmpty(userAgent))
        {
            creds = creds != null
                ? creds.WithSessionInfo(cookies, referer, userAgent)
                : DownloadCredentials.WithSession(cookies, referer, userAgent);
        }

        var task = new DownloadTask(id, url, destPath, totalBytes, supportsRange, status, creds);

        if (!reader.IsDBNull(reader.GetOrdinal("failure_reason")))
        {
            task.Fail(reader.GetString(reader.GetOrdinal("failure_reason")));
        }

        if (!reader.IsDBNull(reader.GetOrdinal("sha256_hash")))
        {
            task.SetSha256(reader.GetString(reader.GetOrdinal("sha256_hash")));
        }

        if (!reader.IsDBNull(reader.GetOrdinal("vt_is_scanned")) && !string.IsNullOrEmpty(task.Sha256Hash))
        {
            var isScanned = reader.GetInt32(reader.GetOrdinal("vt_is_scanned")) == 1;
            var mal = reader.IsDBNull(reader.GetOrdinal("vt_malicious")) ? 0 : reader.GetInt32(reader.GetOrdinal("vt_malicious"));
            var susp = reader.IsDBNull(reader.GetOrdinal("vt_suspicious")) ? 0 : reader.GetInt32(reader.GetOrdinal("vt_suspicious"));
            var harm = reader.IsDBNull(reader.GetOrdinal("vt_harmless")) ? 0 : reader.GetInt32(reader.GetOrdinal("vt_harmless"));
            var undet = reader.IsDBNull(reader.GetOrdinal("vt_undetected")) ? 0 : reader.GetInt32(reader.GetOrdinal("vt_undetected"));
            var scanUrl = reader.IsDBNull(reader.GetOrdinal("vt_scan_url")) ? null : reader.GetString(reader.GetOrdinal("vt_scan_url"));
            var customMsg = reader.IsDBNull(reader.GetOrdinal("vt_custom_message")) ? null : reader.GetString(reader.GetOrdinal("vt_custom_message"));

            var verdict = new SecurityVerdict(task.Sha256Hash, isScanned, mal, susp, harm, undet, scanUrl, customMsg);
            task.SetSecurityVerdict(verdict);
        }

        return task;
    }

    private static int GetOrdinalOrDefault(SqliteDataReader reader, string columnName)
    {
        try
        {
            return reader.GetOrdinal(columnName);
        }
        catch
        {
            return -1;
        }
    }

    public ValueTask DisposeAsync()
    {
        _initLock.Dispose();
        return ValueTask.CompletedTask;
    }
}
