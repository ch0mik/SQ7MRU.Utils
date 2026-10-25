using System.Security.Cryptography;
using System.Text;
using SQ7MRU.Build;

namespace SQ7MRU.Utils.Tests;

public sealed class DesktopChecksumsTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "sq7mru-checksums-" + Guid.NewGuid().ToString("N"));

    public DesktopChecksumsTests()
    {
        Directory.CreateDirectory(directory);
        foreach (var (rid, extension) in DesktopChecksums.Formats) AddPackage(rid, extension, "0.0.0-fixture");
    }

    private string AddPackage(string rid, string extension, string version)
    {
        var path = Path.Combine(directory, $"SQ7MRU.Downloader-{version}-{rid}.{extension}");
        var bytes = Encoding.ASCII.GetBytes($"synthetic payload for {rid}");
        File.WriteAllBytes(path, bytes); // Synthetic bytes, not application archives.
        File.WriteAllText(path + ".sha256", $"{Convert.ToHexStringLower(SHA256.HashData(bytes))}  {Path.GetFileName(path)}\n", Encoding.ASCII);
        return path;
    }

    private string RemoveOne(string pattern)
    {
        var path = Directory.GetFiles(directory, pattern).First();
        File.Delete(path);
        File.Delete(path + ".sha256");
        return path;
    }

    [Fact]
    public async Task CompleteSetProducesReproducibleManifest()
    {
        await DesktopChecksums.WriteManifestAsync(directory);
        var manifest = await File.ReadAllBytesAsync(Path.Combine(directory, "SHA256SUMS"));
        Assert.Equal(8, Encoding.ASCII.GetString(manifest).Split('\n', StringSplitOptions.RemoveEmptyEntries).Length);
        Assert.DoesNotContain((byte)'\r', manifest);
        await DesktopChecksums.WriteManifestAsync(directory);
        Assert.Equal(manifest, await File.ReadAllBytesAsync(Path.Combine(directory, "SHA256SUMS")));
    }

    [Fact]
    public async Task ChangedPackageBlocksManifest()
    {
        await File.WriteAllTextAsync(Directory.GetFiles(directory, "*.zip").First(), "corrupted");
        var error = await Assert.ThrowsAsync<InvalidDataException>(() => DesktopChecksums.WriteManifestAsync(directory));
        Assert.Contains("Checksum mismatch", error.Message);
        Assert.False(File.Exists(Path.Combine(directory, "SHA256SUMS")));
    }

    [Fact]
    public async Task MissingPlatformBlocksManifest()
    {
        RemoveOne("*.dmg");
        var error = await Assert.ThrowsAsync<InvalidDataException>(() => DesktopChecksums.WriteManifestAsync(directory));
        Assert.Contains("exactly eight", error.Message);
    }

    [Fact]
    public async Task DifferentVersionsCannotBePromotedTogether()
    {
        RemoveOne("*-linux-arm.tar.gz");
        AddPackage("linux-arm", "tar.gz", "0.0.0-other-fixture");
        var error = await Assert.ThrowsAsync<InvalidDataException>(() => DesktopChecksums.WriteManifestAsync(directory));
        Assert.Contains("same version", error.Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnexpectedAssetOrDirectoryBlocksManifest(bool isDirectory)
    {
        var unexpected = Path.Combine(directory, "unapproved");
        if (isDirectory) Directory.CreateDirectory(unexpected);
        else await File.WriteAllTextAsync(unexpected, "unexpected");
        var error = await Assert.ThrowsAsync<InvalidDataException>(() => DesktopChecksums.WriteManifestAsync(directory));
        Assert.Contains("Unexpected release asset", error.Message);
    }

    [Fact]
    public async Task WrongPlatformFormatBlocksManifest()
    {
        RemoveOne("*-osx-x64.dmg");
        AddPackage("osx-x64", "zip", "0.0.0-fixture");
        var error = await Assert.ThrowsAsync<InvalidDataException>(() => DesktopChecksums.WriteManifestAsync(directory));
        Assert.Contains("incorrect format", error.Message);
    }

    [Fact]
    public async Task CancelledPromotionDoesNotWriteManifest()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => DesktopChecksums.WriteManifestAsync(directory, cancellation.Token));
        Assert.False(File.Exists(Path.Combine(directory, "SHA256SUMS")));
    }

    public void Dispose() => Directory.Delete(directory, recursive: true);
}
