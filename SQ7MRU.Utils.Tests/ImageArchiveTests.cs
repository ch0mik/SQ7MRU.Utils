using System.IO.Compression;
using System.Text;
using System.Security.Cryptography;
using System.Text.Json;
using SQ7MRU.Utils.Infrastructure;

namespace SQ7MRU.Utils.Tests;

public sealed class ImageArchiveTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "sq7mru-archive-" + Guid.NewGuid().ToString("N"));
    private readonly ArtifactStore store = new();
    private readonly DownloadItem item = new(Path.Combine("EPC", "TEST", "Group", "Award.jpg"), "", AllowImageArchive: true);

    internal static byte[] Zip(params (string Name, byte[] Content)[] entries)
    {
        using var buffer = new MemoryStream();
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
            foreach (var entry in entries)
            {
                using var output = archive.CreateEntry(entry.Name).Open();
                output.Write(entry.Content);
            }
        return buffer.ToArray();
    }

    [Fact]
    public async Task SavesEveryImageAndOnlySkipsCompleteGroups()
    {
        Assert.False(await store.IsValidAsync(root, item, default));
        var folder = await store.SaveAsync(root, item, Zip(("TEST-100.jpg", TestImages.Jpeg), ("TEST-200.jpg", TestImages.Jpeg)), default);
        Assert.Equal(Path.GetDirectoryName(item.RelativePath), folder);
        Assert.False(Directory.Exists(Path.Combine(root, folder, "Award")));
        Assert.True(File.Exists(Path.Combine(root, folder, "TEST-100.jpg")));
        Assert.True(File.Exists(Path.Combine(root, folder, "TEST-200.jpg")));
        Assert.Empty(Directory.GetFiles(Path.Combine(root, folder), "*.json"));
        Assert.Empty(Directory.GetFiles(root, "*.json", SearchOption.AllDirectories));
        var database = Assert.Single(Directory.GetFiles(root, "*.sqlite", SearchOption.AllDirectories));
        Assert.Equal("SQLite format 3\0", Encoding.ASCII.GetString((await File.ReadAllBytesAsync(database))[..16]));
        Assert.True(await store.IsValidAsync(root, item, default));
        File.Delete(Path.Combine(root, folder, "TEST-200.jpg"));
        Assert.False(await store.IsValidAsync(root, item, default));
    }

    [Theory]
    [InlineData("../escape.jpg")]
    [InlineData("..\\escape.jpg")]
    [InlineData("/absolute.jpg")]
    [InlineData("nested/image.jpg")]
    [InlineData("C:escape.jpg")]
    [InlineData(".")]
    [InlineData(" . ")]
    public async Task RejectsUnsafeNamesBeforeWritingAnything(string name)
    {
        var error = await Assert.ThrowsAsync<ServiceException>(() => store.SaveAsync(root, item,
            Zip(("valid.jpg", TestImages.Jpeg), (name, TestImages.Jpeg)), default));
        Assert.Equal(FailureKind.InvalidResponse, error.Kind);
        Assert.False(Directory.Exists(root));
    }

    [Fact]
    public async Task RejectsInvalidImagesDuplicatesEmptyAndOversizedArchives()
    {
        foreach (var bytes in new[]
        {
            Zip(),
            Zip(("valid.jpg", TestImages.Jpeg), ("invalid.jpg", Encoding.UTF8.GetBytes("<html>Log in</html>"))),
            Zip(("one.jpg", TestImages.Jpeg), ("ONE.jpg", TestImages.Jpeg)),
            Zip(Enumerable.Range(0, 129).Select(i => ($"{i}.jpg", TestImages.Jpeg)).ToArray()),
            Zip(("large.jpg", new byte[32 * 1024 * 1024 + 1]))
        })
        {
            var error = await Assert.ThrowsAsync<ServiceException>(() => store.SaveAsync(root, item, bytes, default));
            Assert.Equal(FailureKind.InvalidResponse, error.Kind);
            Assert.False(Directory.Exists(root));
        }
    }

    [Fact]
    public async Task ArchiveHandlingIsOptInAndSingleImagesStillWork()
    {
        await Assert.ThrowsAsync<ServiceException>(() => store.SaveAsync(root, item with { AllowImageArchive = false },
            Zip(("one.jpg", TestImages.Jpeg)), default));
        Assert.Equal(item.RelativePath, await store.SaveAsync(root, item, TestImages.Jpeg, default));
        Assert.True(await store.IsValidAsync(root, item, default));
    }

    [Fact]
    public async Task UnsafeManifestCannotEscapeOutputDirectory()
    {
        await store.SaveAsync(root, item, Zip(("one.jpg", TestImages.Jpeg)), default);
        await ArchiveIndex.WriteAsync(root, item, ["../outside.jpg"], default);
        Assert.False(await store.IsValidAsync(root, item, default));
    }

    [Fact]
    public async Task CancellationPreventsArchiveWrites()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.SaveAsync(root, item,
            Zip(("one.jpg", TestImages.Jpeg)), cancellation.Token));
        Assert.False(Directory.Exists(root));
    }

    [Fact]
    public async Task BundlesInTheSameDirectoryHaveIndependentCompletionManifests()
    {
        var other = item with { RelativePath = Path.Combine("EPC", "TEST", "Group", "Other.jpg") };
        await store.SaveAsync(root, item, Zip(("TEST-AWARD-100.jpg", TestImages.Jpeg)), default);
        await store.SaveAsync(root, other, Zip(("TEST-OTHER-100.jpg", TestImages.Jpeg)), default);
        Assert.True(await store.IsValidAsync(root, item, default));
        Assert.True(await store.IsValidAsync(root, other, default));
        File.Delete(Path.Combine(root, "EPC", "TEST", "Group", "TEST-AWARD-100.jpg"));
        Assert.False(await store.IsValidAsync(root, item, default));
        Assert.True(await store.IsValidAsync(root, other, default));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task LegacyManifestIsMigratedOnlyForCompleteImageSets(bool missingImage, bool metadataSubfolder)
    {
        var folder = Path.GetDirectoryName(item.RelativePath)!;
        if (!missingImage)
            await store.SaveAsync(root, new(Path.Combine(folder, "TEST-100.jpg"), ""), TestImages.Jpeg, default);
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFileName(item.RelativePath).ToUpperInvariant())));
        var metadata = Path.Combine(root, folder, metadataSubfolder ? ".sq7mru" : ".");
        Directory.CreateDirectory(metadata);
        var legacy = Path.Combine(metadata, $".sq7mru-{key}.images.json");
        await File.WriteAllTextAsync(legacy, JsonSerializer.Serialize(new[] { "TEST-100.jpg" }));
        Assert.Equal(!missingImage, await store.IsValidAsync(root, item, default));
        Assert.Equal(missingImage, File.Exists(legacy));
        Assert.Equal(!missingImage, File.Exists(Path.Combine(root, ArchiveIndex.RelativePath)));
        if (missingImage)
        {
            await store.SaveAsync(root, item, Zip(("TEST-100.jpg", TestImages.Jpeg)), default);
            Assert.False(File.Exists(legacy));
            Assert.True(await store.IsValidAsync(root, item, default));
        }
    }

    [Fact]
    public async Task ConcurrentUpdatesRetainAllBundlesInOneDatabase()
    {
        var items = Enumerable.Range(0, 12).Select(i => new DownloadItem($"group-{i}/award.jpg", "", AllowImageArchive: true)).ToArray();
        await Task.WhenAll(items.Select((entry, i) => ArchiveIndex.WriteAsync(root, entry, [$"image-{i}.jpg"], default)));
        for (var i = 0; i < items.Length; i++)
            Assert.Equal(new[] { $"image-{i}.jpg" }, await ArchiveIndex.ReadAsync(root, items[i], default));
        Assert.Single(Directory.GetFiles(root, "*.sqlite", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task CorruptDatabaseIsReportedWithoutOverwritingIt()
    {
        var path = Path.Combine(root, ArchiveIndex.RelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var corrupt = Encoding.UTF8.GetBytes("not a SQLite database");
        await File.WriteAllBytesAsync(path, corrupt);
        var error = await Assert.ThrowsAsync<ServiceException>(() => ArchiveIndex.WriteAsync(root, item, ["one.jpg"], default));
        Assert.Equal(FailureKind.Storage, error.Kind);
        Assert.Equal(corrupt, await File.ReadAllBytesAsync(path));
    }

    [Fact]
    public async Task WaitingForTheIndexLockCanBeCancelled()
    {
        await ArchiveIndex.WriteAsync(root, item, ["one.jpg"], default);
        using var parent = new SecureArtifactDirectory(Path.Combine(root, ".sq7mru"), create: false);
        using var lease = parent.TryAcquireLock("downloads.lock");
        Assert.NotNull(lease);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ArchiveIndex.ReadAsync(root, item, cancellation.Token));
        lease.Dispose();
        Assert.Equal(new[] { "one.jpg" }, await ArchiveIndex.ReadAsync(root, item, default));
    }

    [Fact]
    public async Task RejectsSymbolicLinkEntries()
    {
        using var buffer = new MemoryStream();
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("link.jpg");
            entry.ExternalAttributes = unchecked((int)0xa1ff0000);
            using var output = entry.Open();
            output.Write(TestImages.Jpeg);
        }
        var error = await Assert.ThrowsAsync<ServiceException>(() => store.SaveAsync(root, item, buffer.ToArray(), default));
        Assert.Equal(FailureKind.InvalidResponse, error.Kind);
        Assert.False(Directory.Exists(root));
    }

    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); }
}
