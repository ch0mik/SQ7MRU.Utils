using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace SQ7MRU.Build;

internal static partial class DesktopChecksums
{
    internal static IReadOnlyDictionary<string, string> Formats { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["win-x86"] = "zip", ["win-x64"] = "zip", ["win-arm64"] = "zip",
        ["linux-x64"] = "tar.gz", ["linux-arm64"] = "tar.gz", ["linux-arm"] = "tar.gz",
        ["osx-x64"] = "dmg", ["osx-arm64"] = "dmg"
    };

    [GeneratedRegex(@"\ASQ7MRU\.Downloader-(\d+\.\d+\.\d+(?:-[0-9A-Za-z]+(?:[.-][0-9A-Za-z]+)*)?)-(win-x86|win-x64|win-arm64|linux-x64|linux-arm64|linux-arm|osx-x64|osx-arm64)\.(zip|tar\.gz|dmg)\z", RegexOptions.CultureInvariant)]
    private static partial Regex PackageName();

    internal static async Task WriteManifestAsync(string directory, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var entries = Directory.GetFileSystemEntries(directory).Order(StringComparer.Ordinal).ToArray();
        var packages = entries.Where(p => Path.GetFileName(p) != "SHA256SUMS" && !p.EndsWith(".sha256", StringComparison.Ordinal)).ToArray();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var versions = new HashSet<string>(StringComparer.Ordinal);
        var sidecars = new HashSet<string>(StringComparer.Ordinal);
        var lines = new List<string>();
        foreach (var package in packages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var name = Path.GetFileName(package);
            var match = PackageName().Match(name);
            if (!match.Success || !RegularFile(package)) throw new InvalidDataException($"Unexpected release asset: {name}");
            var version = match.Groups[1].Value;
            var rid = match.Groups[2].Value;
            if (!seen.Add(rid) || match.Groups[3].Value != Formats[rid])
                throw new InvalidDataException($"Duplicate platform or incorrect format: {name}");
            versions.Add(version);
            await using var stream = new FileStream(package, FileMode.Open, FileAccess.Read, FileShare.Read,
                81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
            var digest = Convert.ToHexStringLower(await SHA256.HashDataAsync(stream, cancellationToken));
            var expected = $"{digest}  {name}";
            var sidecar = package + ".sha256";
            sidecars.Add(sidecar);
            if (!RegularFile(sidecar) || new FileInfo(sidecar).Length > 1024 ||
                (await File.ReadAllTextAsync(sidecar, Encoding.ASCII, cancellationToken)).Trim() != expected)
                throw new InvalidDataException($"Checksum mismatch: {name}");
            lines.Add(expected);
        }
        if (!seen.SetEquals(Formats.Keys) || versions.Count != 1)
            throw new InvalidDataException("Expected exactly eight platform packages from the same version.");
        if (!sidecars.SetEquals(entries.Where(p => p.EndsWith(".sha256", StringComparison.Ordinal))))
            throw new InvalidDataException("Unexpected checksum sidecars.");
        var temporary = Path.Combine(directory, $".checksums-{Guid.NewGuid():N}.tmp");
        try
        {
            await File.WriteAllTextAsync(temporary, string.Join('\n', lines) + "\n", Encoding.ASCII, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporary, Path.Combine(directory, "SHA256SUMS"), overwrite: true);
        }
        finally { File.Delete(temporary); }
    }

    private static bool RegularFile(string path) => File.Exists(path) &&
        (File.GetAttributes(path) & (FileAttributes.Directory | FileAttributes.ReparsePoint)) == 0;
}
