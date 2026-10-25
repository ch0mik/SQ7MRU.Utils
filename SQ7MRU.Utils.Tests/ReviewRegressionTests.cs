using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using SQ7MRU.Utils.Infrastructure;

namespace SQ7MRU.Utils.Tests;

public class ReviewRegressionTests
{
    [Theory]
    [InlineData(Architecture.X64, 0xb0000, 0x20000)]
    [InlineData(Architecture.Arm64, 0x8c000, 0x8000)]
    public void LinuxDirectoryFlagsFollowTheTargetKernelAbi(Architecture architecture, int directory, int noFollow)
    {
        // Values are the architecture-specific Linux UAPI contract, checked even
        // on Windows so ARM64 flag regressions do not require an ARM64 test host.
        var flags = SecureArtifactDirectory.LinuxOpenFlags(architecture);
        Assert.Equal(directory, flags.Directory);
        Assert.Equal(noFollow, flags.NoFollow);
        if (architecture == Architecture.Arm64) Assert.Equal(0, flags.Directory & (1 << 16)); // O_DIRECT
    }

    [Fact]
    public void UnixCreatedFilesAndReopenedIndexLocksHavePrivateWritablePermissions()
    {
        if (OperatingSystem.IsWindows()) return;
        var root = Path.Combine(Path.GetTempPath(), "sq7mru-unix-mode-" + Guid.NewGuid().ToString("N"));
        var ownerReadWrite = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        try
        {
            using (var directory = new SecureArtifactDirectory(root, create: true))
            {
                using (var stream = directory.CreateTemporary("certificate.tmp")) stream.Write("test"u8);
                directory.Commit("certificate.tmp", "certificate.pdf");
                Assert.Equal(ownerReadWrite, File.GetUnixFileMode(Path.Combine(root, "certificate.pdf")));
                // This reproduced Permission denied on macOS ARM64 after the initial write.
                File.WriteAllText(Path.Combine(root, "certificate.pdf"), "updated");
                using (var lease = directory.TryAcquireLock("downloads.lock")) Assert.NotNull(lease);
                Assert.Equal(ownerReadWrite, File.GetUnixFileMode(Path.Combine(root, "downloads.lock")));
                using (var reopened = directory.TryAcquireLock("downloads.lock")) Assert.NotNull(reopened);
            }
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); }
    }

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

    [Fact]
    public async Task RepeatedRateLimitsAddHalfASecondAndExtendPendingReservations()
    {
        var clock = new FakeTimeProvider();
        using var scheduler = new ServiceRequestScheduler(clock);
        var initial = TimeSpan.FromSeconds(1);
        await scheduler.WaitForCardAsync(initial, default);
        var pending = scheduler.WaitForCardAsync(initial, default);
        await scheduler.IncreaseEqslCardIntervalAsync(initial, default);
        await scheduler.IncreaseEqslCardIntervalAsync(initial, default);
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.False(pending.IsCompleted);
        // Let the original one-second timer wake and re-check the extended reservation.
        await Task.Yield();
        clock.Advance(TimeSpan.FromMilliseconds(999));
        Assert.False(pending.IsCompleted);
        clock.Advance(TimeSpan.FromMilliseconds(1));
        await pending.WaitAsync(TimeSpan.FromSeconds(2));
        var next = scheduler.WaitForCardAsync(initial, default);
        clock.Advance(TimeSpan.FromSeconds(2));
        await next.WaitAsync(TimeSpan.FromSeconds(2));
    }
}
