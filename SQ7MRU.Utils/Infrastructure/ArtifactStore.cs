using System.Text;

namespace SQ7MRU.Utils.Infrastructure;

public sealed class ArtifactStore : IArtifactStore
{
    public static string SafeName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars().Concat("<>:\"/\\|?*").ToHashSet();
        var safe = new string(name.Select(c => invalid.Contains(c) || char.IsControl(c) ? '_' : c).ToArray()).Trim().Trim('.');
        if (safe.Length == 0) throw new ServiceException(FailureKind.InvalidResponse, "The service returned an empty filename.");
        return safe;
    }

    private static string Resolve(string directory, string relative)
    {
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));
        if (!Path.EndsInDirectorySeparator(root)) root += Path.DirectorySeparatorChar;
        var path = Path.GetFullPath(Path.Combine(root, relative));
        if (!path.StartsWith(root, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            throw new ServiceException(FailureKind.Storage, "Output path escapes the selected directory.");
        return path;
    }

    public async Task<bool> IsValidAsync(string directory, DownloadItem item, CancellationToken cancellationToken)
    {
        IEnumerable<string> candidates = item.IsAdif ? new[] { item.RelativePath } : new[]
        {
            item.RelativePath, Path.ChangeExtension(item.RelativePath, ".png"),
            Path.ChangeExtension(item.RelativePath, ".gif"), Path.ChangeExtension(item.RelativePath, ".jpeg")
        };
        if (!item.IsAdif && item.AllowPdf) candidates = candidates.Append(Path.ChangeExtension(item.RelativePath, ".pdf"));
        foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var path = Resolve(directory, candidate);
            try
            {
                using var parent = new SecureArtifactDirectory(Path.GetDirectoryName(path)!, create: false);
                await using var stream = parent.OpenRead(Path.GetFileName(path));
                if (!stream.CanSeek) throw new ServiceException(FailureKind.Storage, "The output entry is not a regular file.");
                var length = stream.Length;
                if (length > 32 * 1024 * 1024) continue;
                var bytes = new byte[(int)length];
                await stream.ReadExactlyAsync(bytes, cancellationToken).ConfigureAwait(false);
                if (stream.Length == length && Valid(bytes, item.IsAdif, item.AllowPdf)) return true;
            }
            catch (FileNotFoundException) { }
            catch (EndOfStreamException) { }
        }
        return item.AllowImageArchive && !item.IsAdif &&
            await ImageArchiveStore.IsValidAsync(this, directory, item, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> SaveAsync(string directory, DownloadItem item, byte[] content, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (item.AllowImageArchive && !item.IsAdif && ImageArchiveStore.IsZip(content))
            return await ImageArchiveStore.SaveAsync(this, directory, item, content, cancellationToken).ConfigureAwait(false);
        var extension = item.IsAdif ? null : DocumentExtension(content, item.AllowPdf);
        if (item.IsAdif ? !Valid(content, true) : extension is null)
            throw new ServiceException(FailureKind.InvalidResponse, item.IsAdif ? "The service returned invalid ADIF content."
                : item.AllowPdf ? "The response is not a valid image or structurally complete PDF certificate."
                : "The response is not a complete JPEG, PNG or GIF image (it may be HTML or a truncated file).");
        var relativePath = item.IsAdif ? item.RelativePath : Path.ChangeExtension(item.RelativePath, extension);
        await WriteFileAsync(directory, relativePath, content, cancellationToken).ConfigureAwait(false);
        return relativePath;
    }

    internal static async Task WriteFileAsync(string directory, string relativePath, byte[] content, CancellationToken cancellationToken)
    {
        var path = Resolve(directory, relativePath);
        using var parent = new SecureArtifactDirectory(Path.GetDirectoryName(path)!, create: true);
        var name = Path.GetFileName(path);
        var temporary = ".sq7mru-" + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var stream = parent.CreateTemporary(temporary))
                await stream.WriteAsync(content, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            parent.Commit(temporary, name);
        }
        finally { parent.RemoveTemporary(temporary); }
    }

    internal static async Task<byte[]?> ReadFileAsync(string directory, string relativePath, int limit, CancellationToken token, bool rejectOversize = false)
    {
        var path = Resolve(directory, relativePath);
        try
        {
            using var parent = new SecureArtifactDirectory(Path.GetDirectoryName(path)!, create: false);
            await using var stream = parent.OpenRead(Path.GetFileName(path));
            if (!stream.CanSeek || stream.Length > limit)
            {
                if (rejectOversize) throw new ServiceException(FailureKind.Storage, "The metadata file is not a bounded regular file.");
                return null;
            }
            var bytes = new byte[(int)stream.Length];
            await stream.ReadExactlyAsync(bytes, token).ConfigureAwait(false);
            return bytes;
        }
        catch (FileNotFoundException) { return null; }
        catch (EndOfStreamException) { return null; }
    }

    internal static void RemoveFile(string directory, string relativePath)
    {
        var path = Resolve(directory, relativePath);
        try
        {
            using var parent = new SecureArtifactDirectory(Path.GetDirectoryName(path)!, create: false);
            parent.RemoveTemporary(Path.GetFileName(path));
        }
        catch (FileNotFoundException) { }
    }

    private static bool Valid(byte[] bytes, bool adif, bool allowPdf = false)
    {
        if (adif)
        {
            var text = Encoding.UTF8.GetString(bytes);
            return !text.Contains("<html", StringComparison.OrdinalIgnoreCase) &&
                (text.Contains("<EOH>", StringComparison.OrdinalIgnoreCase) || text.Contains("<EOR>", StringComparison.OrdinalIgnoreCase));
        }
        return DocumentExtension(bytes, allowPdf) is not null;
    }

    internal static string? DocumentExtension(byte[] bytes, bool allowPdf) =>
        allowPdf && PdfCertificateValidator.IsValid(bytes) ? ".pdf" : CardImageValidator.Extension(bytes);
}
