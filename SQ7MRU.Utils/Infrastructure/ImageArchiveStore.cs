using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SQ7MRU.Utils.Infrastructure;

// ZIP support is opt-in for EPC items; archive paths never reach the filesystem.
internal static class ImageArchiveStore
{
    private const int MaxEntries = 128;
    private const int MaxEntryBytes = 32 * 1024 * 1024;
    private const long MaxExpandedBytes = 64 * 1024 * 1024;
    private const string ManifestName = ".sq7mru-images.json";

    internal static bool IsZip(byte[] content) => content.Length >= 4 &&
        (content.AsSpan(0, 4).SequenceEqual("PK\u0003\u0004"u8) ||
         content.AsSpan(0, 4).SequenceEqual("PK\u0005\u0006"u8));

    private static string Folder(DownloadItem item) => Path.GetDirectoryName(item.RelativePath) is { Length: > 0 } folder ? folder : ".";

    private static string ManifestPath(DownloadItem item, bool legacy = false)
    {
        var key = SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFileName(item.RelativePath).ToUpperInvariant()));
        var name = $".sq7mru-{Convert.ToHexString(key)}.images.json";
        return legacy ? Path.Combine(Folder(item), name) : Path.Combine(Folder(item), ".sq7mru", name);
    }

    private static bool SafeEntryName(string name) => name.Length is > 0 and <= 200 &&
        name.Trim().Trim('.').Length > 0 &&
        !name.Contains('/') && !name.Contains('\\') &&
        name == ArtifactStore.SafeName(name) && name != ManifestName &&
        !name.StartsWith(".sq7mru-", StringComparison.OrdinalIgnoreCase);

    internal static async Task<bool> IsValidAsync(ArtifactStore store, string directory, DownloadItem item, CancellationToken token)
    {
        var folder = Folder(item);
        var names = await ArchiveIndex.ReadAsync(directory, item, token).ConfigureAwait(false);
        var legacy = names is null;
        if (legacy)
        {
            var bytes = await ArtifactStore.ReadFileAsync(directory, ManifestPath(item), 65536, token).ConfigureAwait(false)
                ?? await ArtifactStore.ReadFileAsync(directory, ManifestPath(item, legacy: true), 65536, token).ConfigureAwait(false);
            if (bytes is null) return false;
            try { names = JsonSerializer.Deserialize<string[]>(bytes); }
            catch (JsonException) { return false; }
        }
        if (names is not { Length: > 0 and <= MaxEntries } ||
            names.Any(name => name is null || !SafeEntryName(name)) ||
            names.Distinct(StringComparer.OrdinalIgnoreCase).Count() != names.Length) return false;
        foreach (var name in names)
        {
            token.ThrowIfCancellationRequested();
            if (!await store.IsValidAsync(directory, new(Path.Combine(folder, name), "", AllowPdf: item.AllowPdf), token).ConfigureAwait(false)) return false;
        }
        if (legacy)
            await ArchiveIndex.WriteAsync(directory, item, names, token).ConfigureAwait(false);
        ArtifactStore.RemoveFile(directory, ManifestPath(item));
        ArtifactStore.RemoveFile(directory, ManifestPath(item, legacy: true));
        return true;
    }

    internal static async Task<string> SaveAsync(ArtifactStore store, string directory, DownloadItem item, byte[] content, CancellationToken token)
    {
        if (content.Length > MaxEntryBytes) throw InvalidArchive();
        try
        {
            using var input = new MemoryStream(content, writable: false);
            using var archive = new ZipArchive(input, ZipArchiveMode.Read);
            if (archive.Entries.Count is 0 or > MaxEntries) throw InvalidArchive();
            var images = new List<(string Name, byte[] Bytes)>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            long expanded = 0;
            foreach (var entry in archive.Entries)
            {
                token.ThrowIfCancellationRequested();
                // Only flat, regular image entries are accepted, never directories or symlinks.
                var kind = (entry.ExternalAttributes >> 16) & 0xf000;
                if (!SafeEntryName(entry.FullName) || kind is not (0 or 0x8000) ||
                    (entry.ExternalAttributes & 0x10) != 0 || entry.Length is <= 0 or > MaxEntryBytes)
                    throw InvalidArchive();
                expanded += entry.Length;
                if (expanded > MaxExpandedBytes) throw InvalidArchive();
                await using var source = entry.Open();
                var bytes = new byte[(int)entry.Length];
                await source.ReadExactlyAsync(bytes, token).ConfigureAwait(false);
                if (await source.ReadAsync(new byte[1], token).ConfigureAwait(false) != 0) throw InvalidArchive();
                var extension = ArtifactStore.DocumentExtension(bytes, item.AllowPdf) ?? throw InvalidArchive();
                var name = Path.ChangeExtension(entry.FullName, extension);
                if (!seen.Add(name)) throw InvalidArchive();
                images.Add((name, bytes));
            }

            // Validate the entire archive before writing any image. Commit the index last;
            // incomplete/cancelled groups must never be skipped on the next run.
            var folder = Folder(item);
            foreach (var image in images)
                await store.SaveAsync(directory, new(Path.Combine(folder, image.Name), "", AllowPdf: item.AllowPdf), image.Bytes, token).ConfigureAwait(false);
            await ArchiveIndex.WriteAsync(directory, item, images.Select(image => image.Name).ToArray(), token).ConfigureAwait(false);
            ArtifactStore.RemoveFile(directory, ManifestPath(item));
            ArtifactStore.RemoveFile(directory, ManifestPath(item, legacy: true));
            return folder;
        }
        catch (InvalidDataException) { throw InvalidArchive(); }
        catch (EndOfStreamException) { throw InvalidArchive(); }
    }

    private static ServiceException InvalidArchive() => new(FailureKind.InvalidResponse,
        "The EPC ZIP must contain 1–128 valid certificates in permitted formats with safe flat filenames (maximum 32 MiB per file and 64 MiB total).");
}
