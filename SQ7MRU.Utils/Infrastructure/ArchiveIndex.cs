using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace SQ7MRU.Utils.Infrastructure;

// Run SQLite in memory and persist its standard database bytes through the same
// confined, atomic storage as images. Native SQLite never opens untrusted paths.
internal static class ArchiveIndex
{
    internal static readonly string RelativePath = Path.Combine(ArtifactStorageOptions.DefaultTechnicalDirectoryName, "downloads.sqlite");
    private const int MaxBytes = 16 * 1024 * 1024;
    private static string Key(DownloadItem item) => item.RelativePath.Replace('\\', '/').ToUpperInvariant();

    internal static Task<string[]?> ReadAsync(string directory, DownloadItem item, CancellationToken token,
        string technicalDirectoryName = ArtifactStorageOptions.DefaultTechnicalDirectoryName) =>
        AccessAsync(directory, item, null, token, technicalDirectoryName);

    internal static async Task WriteAsync(string directory, DownloadItem item, string[] names, CancellationToken token,
        string technicalDirectoryName = ArtifactStorageOptions.DefaultTechnicalDirectoryName) =>
        _ = await AccessAsync(directory, item, names, token, technicalDirectoryName).ConfigureAwait(false);

    private static async Task<string[]?> AccessAsync(string directory, DownloadItem item, string[]? names, CancellationToken token,
        string technicalDirectoryName)
    {
        token.ThrowIfCancellationRequested();
        ArtifactStorageOptions.ValidateDirectoryName(technicalDirectoryName);
        var relativePath = Path.Combine(technicalDirectoryName, "downloads.sqlite");
        // A read-only miss does not create output directories or a database.
        if (names is null && await ArtifactStore.ReadFileAsync(directory, relativePath, MaxBytes, token, rejectOversize: true).ConfigureAwait(false) is null)
            return null;
        using var parent = new SecureArtifactDirectory(Path.Combine(Path.GetFullPath(directory), technicalDirectoryName), create: true);
        FileStream? lease;
        var waiting = Stopwatch.StartNew();
        while ((lease = parent.TryAcquireLock("downloads.lock")) is null)
        {
            if (waiting.Elapsed > TimeSpan.FromSeconds(30))
                throw new ServiceException(FailureKind.Storage, "Timed out waiting for the download index.");
            await Task.Delay(50, token).ConfigureAwait(false);
        }
        using (lease)
        {
            var bytes = await ArtifactStore.ReadFileAsync(directory, relativePath, MaxBytes, token, rejectOversize: true).ConfigureAwait(false);
            // Microsoft.Data.Sqlite executes synchronously; keep that work off the UI thread.
            var result = await Task.Run(() => Execute(bytes, Key(item), names, token), token).ConfigureAwait(false);
            if (result.Bytes is not null)
                await ArtifactStore.WriteFileAsync(directory, relativePath, result.Bytes, token).ConfigureAwait(false);
            return result.Names;
        }
    }

    private static (string[]? Names, byte[]? Bytes) Execute(byte[]? bytes, string key, string[]? names, CancellationToken token)
    {
        try
        {
            using var connection = new SqliteConnection("Data Source=:memory:;Pooling=False");
            connection.Open();
            var execution = Stopwatch.StartNew();
            raw.sqlite3_progress_handler(connection.Handle, 1000,
                _ => token.IsCancellationRequested || execution.Elapsed > TimeSpan.FromSeconds(5) ? 1 : 0, null);
            if (bytes is not null)
            {
                if (bytes.Length < 100 || !bytes.AsSpan(0, 16).SequenceEqual("SQLite format 3\0"u8)) throw InvalidIndex();
                // SQLite advises padding deserialization buffers for untrusted input.
                var memory = raw.sqlite3_malloc(bytes.Length + 32);
                if (memory == IntPtr.Zero) throw new OutOfMemoryException();
                Marshal.Copy(new byte[32], 0, memory + bytes.Length, 32);
                Marshal.Copy(bytes, 0, memory, bytes.Length);
                // FREEONCLOSE | RESIZEABLE; SQLite also frees the buffer on failure.
                if (raw.sqlite3_deserialize(connection.Handle, "main", memory, bytes.Length, bytes.Length + 32, 3) != raw.SQLITE_OK)
                    throw InvalidIndex();
            }
            using var command = connection.CreateCommand();
            command.CommandText = """
                PRAGMA trusted_schema=OFF;
                PRAGMA max_page_count=4096;
                CREATE TABLE IF NOT EXISTS archive_images (
                    bundle TEXT NOT NULL, file_name TEXT NOT NULL,
                    PRIMARY KEY(bundle, file_name)) WITHOUT ROWID;
                """;
            command.ExecuteNonQuery();
            token.ThrowIfCancellationRequested();
            if (names is null)
            {
                command.CommandText = "SELECT file_name FROM archive_images WHERE bundle=$bundle ORDER BY file_name LIMIT 129;";
                command.Parameters.AddWithValue("$bundle", key);
                using var reader = command.ExecuteReader();
                var found = new List<string>();
                while (reader.Read()) { token.ThrowIfCancellationRequested(); found.Add(reader.GetString(0)); }
                return (found.Count == 0 ? null : found.ToArray(), null);
            }
            using (var transaction = connection.BeginTransaction())
            {
                command.Transaction = transaction;
                command.CommandText = "DELETE FROM archive_images WHERE bundle=$bundle;";
                command.Parameters.AddWithValue("$bundle", key);
                command.ExecuteNonQuery();
                command.CommandText = "INSERT INTO archive_images(bundle, file_name) VALUES ($bundle, $name);";
                var parameter = command.Parameters.Add("$name", SqliteType.Text);
                foreach (var name in names)
                {
                    token.ThrowIfCancellationRequested();
                    parameter.Value = name;
                    command.ExecuteNonQuery();
                }
                token.ThrowIfCancellationRequested();
                transaction.Commit();
            }
            var serialized = raw.sqlite3_serialize(connection.Handle, "main", out var size, 0);
            if (serialized == IntPtr.Zero) throw new OutOfMemoryException();
            try
            {
                if (size > MaxBytes) throw InvalidIndex();
                var output = new byte[(int)size];
                Marshal.Copy(serialized, output, 0, output.Length);
                return (names, output);
            }
            finally { raw.sqlite3_free(serialized); }
        }
        catch (SqliteException)
        {
            token.ThrowIfCancellationRequested();
            throw InvalidIndex();
        }
        catch (InvalidCastException) { throw InvalidIndex(); }
    }

    private static ServiceException InvalidIndex() => new(FailureKind.Storage,
        "The SQLite download index is invalid or exceeds its size limit.");
}
