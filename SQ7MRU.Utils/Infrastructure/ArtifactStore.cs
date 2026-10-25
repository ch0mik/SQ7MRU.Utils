using System.Text;
using Microsoft.Extensions.Options;

namespace SQ7MRU.Utils.Infrastructure;

/// <summary>Validates certificate/ADIF payloads and persists them through confined atomic filesystem operations.</summary>
/// <remarks>State is stored in files, not this instance. Certificate ZIPs use a bounded SQLite index beneath the selected root. Images are decoded; opted-in PDFs receive structural checks, not rendering or active-content sanitization.</remarks>
public sealed class ArtifactStore : IArtifactStore
{
    internal string TechnicalDirectoryName { get; }

    /// <summary>Creates a store using the default technical directory.</summary>
    public ArtifactStore() : this(Options.Create(new ArtifactStorageOptions())) { }

    /// <summary>Creates a store using the configured shared technical directory.</summary>
    /// <param name="options">Storage options captured when the store is created.</param>
    /// <exception cref="ArgumentNullException">Options are null.</exception>
    /// <exception cref="ArgumentException">The technical directory is not a safe single name.</exception>
    /// <exception cref="OptionsValidationException">Registered storage-option validation fails.</exception>
    public ArtifactStore(IOptions<ArtifactStorageOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        TechnicalDirectoryName = ArtifactStorageOptions.ValidateDirectoryName(options.Value.TechnicalDirectoryName);
    }

    /// <summary>Replaces invalid filename characters and trims surrounding spaces and dots.</summary>
    /// <param name="name">One service-provided filename component, not an arbitrary trusted output path.</param>
    /// <returns>A nonempty sanitized component; this is not a uniqueness guarantee.</returns>
    /// <exception cref="ServiceException">Sanitization leaves no usable filename.</exception>
    /// <remarks>Both slash forms, control characters and Windows-invalid characters are replaced on every OS. Storage separately enforces path confinement and link rejection.</remarks>
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

    /// <summary>Checks a saved file or every certificate in an indexed bundle before allowing a skip.</summary>
    /// <param name="directory">Selected output root; it need not exist for a negative result.</param>
    /// <param name="item">Logical path and explicitly permitted content formats.</param>
    /// <param name="cancellationToken">Cancels payload reads, index-lock waiting and bundle validation.</param>
    /// <returns>True if suitable local content passes validation; false when it is absent or its content is invalid.</returns>
    /// <exception cref="ServiceException">A path is unsafe or SQLite metadata is invalid.</exception>
    /// <exception cref="IOException">A filesystem or index-lock operation fails.</exception>
    /// <exception cref="UnauthorizedAccessException">Local content cannot be accessed.</exception>
    /// <exception cref="OperationCanceledException">Caller cancellation is requested.</exception>
    /// <remarks>Detected PNG/GIF/JPEG and permitted PDF alternatives are considered. Legacy JSON migration can write metadata here. ADIF validation checks markers rather than parsing records; download orchestration still refreshes ADIF regardless of this result.</remarks>
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

    /// <summary>Validates and saves an artifact, detecting its real extension or extracting an opted-in ZIP.</summary>
    /// <param name="directory">Selected output root; required subdirectories are created beneath it.</param>
    /// <param name="item">Logical output path, with ADIF/archive/PDF behavior explicitly permitted by its flags.</param>
    /// <param name="content">Complete encoded response bytes; HTML and invalid expected content are rejected.</param>
    /// <param name="cancellationToken">Cancels pending writes and prevents replacement if observed before commit.</param>
    /// <returns>The root-relative saved filename, or the containing group directory for a ZIP selection.</returns>
    /// <exception cref="ServiceException">Validation, confinement or bundle-index checks fail.</exception>
    /// <exception cref="IOException">Writing, replacement or index locking fails.</exception>
    /// <exception cref="UnauthorizedAccessException">Output is not writable.</exception>
    /// <exception cref="OperationCanceledException">Caller cancellation is requested.</exception>
    /// <remarks>Encoded files are limited to 32 MiB. ZIPs permit 1–128 flat regular entries and at most 64 MiB expanded content. Every entry is validated before writes; files are replaced individually and SQLite metadata is committed last, so cancellation can leave committed members. Existing destination files are replaced only after valid content has been prepared.</remarks>
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
