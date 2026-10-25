using System.Text;
using SQ7MRU.Utils.Infrastructure;

namespace SQ7MRU.Utils.Tests;

public sealed class PdfStorageTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "sq7mru-pdf-" + Guid.NewGuid().ToString("N"));
    private readonly ArtifactStore store = new();
    private readonly DownloadItem item = new("EPC/TEST/Membership.jpg", "", AllowPdf: true);

    [Fact]
    public async Task SavesPdfWithCorrectExtensionAndRecognizesItOnResume()
    {
        var saved = await store.SaveAsync(root, item, TestDocuments.Pdf, default);
        Assert.Equal("EPC/TEST/Membership.pdf", saved.Replace('\\', '/'));
        Assert.Equal(TestDocuments.Pdf, await File.ReadAllBytesAsync(Path.Combine(root, saved)));
        Assert.True(await store.IsValidAsync(root, item, default));
        await File.WriteAllBytesAsync(Path.Combine(root, saved), TestDocuments.Pdf[..80]);
        Assert.False(await store.IsValidAsync(root, item, default));
    }

    [Fact]
    public async Task PdfIsOptIn()
    {
        await Assert.ThrowsAsync<ServiceException>(() => store.SaveAsync(root, item with { AllowPdf = false }, TestDocuments.Pdf, default));
        Assert.False(Directory.Exists(root));
    }

    [Theory]
    [InlineData("%PDF-1.4\n%%EOF")]
    [InlineData("<html>Login</html>")]
    public async Task RejectsIncompletePdfAndHtml(string content)
    {
        await Assert.ThrowsAsync<ServiceException>(() => store.SaveAsync(root, item, Encoding.ASCII.GetBytes(content), default));
        Assert.False(Directory.Exists(root));
    }

    [Fact]
    public async Task PdfInsideZipUsesTheSameIndexAndValidation()
    {
        var bundle = item with { AllowImageArchive = true };
        var folder = await store.SaveAsync(root, bundle, ImageArchiveTests.Zip(("TEST-CERTIFICATE.pdf", TestDocuments.Pdf)), default);
        Assert.True(File.Exists(Path.Combine(root, folder, "TEST-CERTIFICATE.pdf")));
        Assert.True(await store.IsValidAsync(root, bundle, default));
        File.Delete(Path.Combine(root, folder, "TEST-CERTIFICATE.pdf"));
        Assert.False(await store.IsValidAsync(root, bundle, default));
    }

    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); }
}
