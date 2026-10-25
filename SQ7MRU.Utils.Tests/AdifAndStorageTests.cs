using System.Text;
using SQ7MRU.Utils.Infrastructure;
namespace SQ7MRU.Utils.Tests;
public class AdifAndStorageTests
{
    [Fact]
    public async Task AcceptsOutputDirectoryWithTrailingSeparator()
    {
        var root = Path.Combine(Path.GetTempPath(), "sq7mru-path-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new ArtifactStore();
            await store.SaveAsync(root + Path.DirectorySeparatorChar, new("test.adif", "", true), "<EOH>"u8.ToArray(), default);
            Assert.True(await store.IsValidAsync(root + Path.DirectorySeparatorChar, new("test.adif", "", true), default));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task RejectsDirectoryLinksBeforeReadingOrWriting()
    {
        var root = Path.Combine(Path.GetTempPath(), "sq7mru-links-" + Guid.NewGuid().ToString("N"));
        var outside = Path.Combine(root, "outside");
        var output = Path.Combine(root, "output");
        Directory.CreateDirectory(outside);
        Directory.CreateDirectory(output);
        var link = Path.Combine(output, "linked");
        try
        {
            if (OperatingSystem.IsWindows())
            {
                var start = new System.Diagnostics.ProcessStartInfo("powershell") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
                start.Environment["SQ7MRU_TEST_LINK"] = link;
                start.Environment["SQ7MRU_TEST_TARGET"] = outside;
                foreach (var argument in new[] { "-NoProfile", "-NonInteractive", "-Command", "New-Item -ItemType Junction -Path $env:SQ7MRU_TEST_LINK -Target $env:SQ7MRU_TEST_TARGET | Out-Null" }) start.ArgumentList.Add(argument);
                using var process = System.Diagnostics.Process.Start(start)!;
                await process.WaitForExitAsync();
                Assert.Equal(0, process.ExitCode);
            }
            else Directory.CreateSymbolicLink(link, outside);
            var store = new ArtifactStore();
            await Assert.ThrowsAsync<ServiceException>(() => store.SaveAsync(output, new("linked/test.adif", "", true), "<EOH>"u8.ToArray(), default));
            await Assert.ThrowsAsync<ServiceException>(() => store.IsValidAsync(output, new("linked/test.adif", "", true), default));
            Assert.Empty(Directory.GetFiles(outside));
        }
        finally
        {
            if (Directory.Exists(link)) Directory.Delete(link);
            Directory.Delete(root, true);
        }
    }

    [Theory]
    [InlineData("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aHZkAAAAASUVORK5CYII=", ".png")]
    [InlineData("R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7", ".gif")]
    public async Task StoresNonJpegCardsWithCorrectExtensionAndFindsThemOnResume(string base64, string extension)
    {
        var root = Path.Combine(Path.GetTempPath(), "sq7mru-images-" + Guid.NewGuid().ToString("N"));
        var store = new ArtifactStore();
        var item = new DownloadItem("account/card.jpg", "");
        try
        {
            var saved = await store.SaveAsync(root, item, Convert.FromBase64String(base64), default);
            Assert.Equal("account/card" + extension, saved.Replace('\\', '/'));
            Assert.True(File.Exists(Path.Combine(root, saved)));
            Assert.False(File.Exists(Path.Combine(root, item.RelativePath)));
            Assert.True(await store.IsValidAsync(root, item, default));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Theory]
    [InlineData("<eoh><call:6>sq7mru<comment:7>a<eor>b<eor>")]
    [InlineData("<call:6>sq7mru<eor>")]
    public void ReadsCaseInsensitiveTagsWithoutSplittingFieldValues(string content)
    {
        using var reader = new AdifReader(content);
        var row = Assert.Single(reader.GetAdifRows());
        Assert.Equal("SQ7MRU", row.CALL);
    }
    [Theory]
    [InlineData("<CALL:99>short<EOR>")]
    [InlineData("<CALL:6>SQ7MRU")]
    public void RejectsTruncatedRecords(string content)
    {
        using var reader = new AdifReader(content);
        Assert.Throws<FormatException>(() => reader.GetAdifRows());
    }
    [Fact]
    public async Task StorageRejectsHtmlAndTraversalAndReplacesInvalidFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), "sq7mru-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var store = new ArtifactStore(); var item = new DownloadItem("qsl.jpg", "");
            await File.WriteAllTextAsync(Path.Combine(root, "qsl.jpg"), "<html>rate limit</html>");
            Assert.False(await store.IsValidAsync(root, item, default));
            await Assert.ThrowsAsync<ServiceException>(() => store.SaveAsync(root, item, Encoding.UTF8.GetBytes("<html>login</html>"), default));
            await Assert.ThrowsAsync<ServiceException>(() => store.SaveAsync(root, item with { RelativePath = "../escape.jpg" }, TestImages.Jpeg, default));
            await store.SaveAsync(root, item, TestImages.Jpeg, default);
            Assert.True(await store.IsValidAsync(root, item, default));
            Assert.Empty(Directory.GetFiles(root, "*.tmp"));
        }
        finally { Directory.Delete(root, true); }
    }
}
