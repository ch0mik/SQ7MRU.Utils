using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using SQ7MRU.Utils.Infrastructure;

namespace SQ7MRU.Utils.Tests;

public class ReviewRegressionTests
{
    [Fact]
    public async Task HeldDirectoryCannotBeRedirectedDuringAWrite()
    {
        var fixture = Path.Combine(Path.GetTempPath(), "sq7mru-race-" + Guid.NewGuid().ToString("N"));
        var output = Path.Combine(fixture, "output");
        var moved = Path.Combine(fixture, "moved");
        var outside = Path.Combine(fixture, "outside");
        Directory.CreateDirectory(output);
        Directory.CreateDirectory(outside);
        try
        {
            using (var directory = new SecureArtifactDirectory(output, create: true))
            {
                if (OperatingSystem.IsWindows()) Assert.Throws<IOException>(() => Directory.Move(output, moved));
                else
                {
                    Directory.Move(output, moved);
                    Directory.CreateSymbolicLink(output, outside);
                }
                await using (var stream = directory.CreateTemporary("test.tmp")) await stream.WriteAsync("test"u8.ToArray());
                directory.Commit("test.tmp", "test.adif");
                Assert.False(File.Exists(Path.Combine(outside, "test.adif")));
                Assert.True(File.Exists(Path.Combine(OperatingSystem.IsWindows() ? output : moved, "test.adif")));
            }
        }
        finally
        {
            if (!OperatingSystem.IsWindows() && Directory.Exists(output)) Directory.Delete(output);
            Directory.Delete(fixture, true);
        }
    }

    private sealed class ExportSession : IDownloadSessionFactory, IDownloadSession
    {
        public QslService Service => QslService.Eqsl;
        public string Content = "<CALL:5>K1ABC<EOR>";
        public bool FailAfterFirst;
        public Task<IDownloadSession> OpenAsync(ServiceCredentials credentials, IProgress<DownloadProgress>? progress, CancellationToken cancellationToken) => Task.FromResult<IDownloadSession>(this);
        public async IAsyncEnumerable<DownloadItem> DiscoverAsync(ServiceAccount? account, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield return new("export.adif", "", true);
            if (FailAfterFirst) throw new ServiceException(FailureKind.InvalidResponse, "Malformed subsequent record.");
        }
        public Task<byte[]> FetchAsync(DownloadItem item, CancellationToken cancellationToken) => Task.FromResult(System.Text.Encoding.UTF8.GetBytes(Content));
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RefreshesExportAndKeepsEarlierValidItemsOnDiscoveryFailure(bool failAfterFirst)
    {
        var root = Path.Combine(Path.GetTempPath(), "sq7mru-regression-" + Guid.NewGuid().ToString("N"));
        var session = new ExportSession();
        var service = new DownloadService([session], new ArtifactStore(), Options.Create(new DownloadOptions()), NullLogger<DownloadService>.Instance);
        var request = new DownloadRequest(QslService.Eqsl, new("test", "secret"), root);
        try
        {
            await service.DownloadAsync(request);
            session.Content += "<CALL:5>K2ABC<EOR>";
            session.FailAfterFirst = failAfterFirst;
            var result = await service.DownloadAsync(request);
            Assert.Equal(1, result.Downloaded);
            Assert.Equal(failAfterFirst ? 1 : 0, result.Failed);
            Assert.Contains("K2ABC", await File.ReadAllTextAsync(Path.Combine(root, "export.adif")));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task FourByteJpegIsRejectedAndNotAcceptedAsExistingCard()
    {
        var root = Path.Combine(Path.GetTempPath(), "sq7mru-corrupt-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var item = new DownloadItem("broken.jpg", "");
        var bytes = new byte[] { 255, 216, 255, 217 };
        var store = new ArtifactStore();
        try
        {
            await Assert.ThrowsAsync<ServiceException>(() => store.SaveAsync(root, item, bytes, default));
            await File.WriteAllBytesAsync(Path.Combine(root, item.RelativePath), bytes);
            Assert.False(await store.IsValidAsync(root, item, default));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task CardGenerationIntervalIsSharedAcrossSessionsAndCancellable()
    {
        var clock = new FakeTimeProvider();
        using var scheduler = new ServiceRequestScheduler(clock);
        await scheduler.WaitForCardAsync(TimeSpan.FromSeconds(12), default);
        var next = scheduler.WaitForCardAsync(TimeSpan.FromSeconds(12), default);
        Assert.False(next.IsCompleted);
        clock.Advance(TimeSpan.FromSeconds(11));
        Assert.False(next.IsCompleted);
        clock.Advance(TimeSpan.FromSeconds(1));
        await next.WaitAsync(TimeSpan.FromSeconds(2));
        using var cancellation = new CancellationTokenSource();
        var cancelled = scheduler.WaitForCardAsync(TimeSpan.FromSeconds(12), cancellation.Token);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelled);
    }
}
