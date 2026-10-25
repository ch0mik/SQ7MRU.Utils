using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace SQ7MRU.Utils.Tests;

public class ApplicationTests
{
    private sealed class Session : IDownloadSession, IDownloadSessionFactory
    {
        public QslService Service => QslService.Eqsl;
        public readonly TaskCompletionSource Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Active, Peak, Finished;
        public bool Disposed;
        public Task<IDownloadSession> OpenAsync(ServiceCredentials credentials, IProgress<DownloadProgress>? progress, CancellationToken cancellationToken) => Task.FromResult<IDownloadSession>(this);
        public async IAsyncEnumerable<DownloadItem> DiscoverAsync(ServiceAccount? account, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            for (var i = 0; i < 6; i++) { cancellationToken.ThrowIfCancellationRequested(); yield return new(i + ".jpg", i.ToString()); }
        }
        public async Task<byte[]> FetchAsync(DownloadItem item, CancellationToken cancellationToken)
        {
            var active = Interlocked.Increment(ref Active);
            Peak = Math.Max(Peak, active);
            Started.TrySetResult();
            try { await Release.Task.WaitAsync(cancellationToken); Interlocked.Increment(ref Finished); return [0xff, 0xd8, 0xff, 0xd9]; }
            finally { Interlocked.Decrement(ref Active); }
        }
        public ValueTask DisposeAsync() { Disposed = true; return ValueTask.CompletedTask; }
    }
    private sealed class Store : IArtifactStore
    {
        public Task<bool> IsValidAsync(string directory, DownloadItem item, CancellationToken cancellationToken) => Task.FromResult(item.Resource == "0");
        public Task<string> SaveAsync(string directory, DownloadItem item, byte[] content, CancellationToken cancellationToken)
        {
            if (item.Resource == "3") throw new IOException();
            return Task.FromResult(item.RelativePath);
        }
    }
    private static DownloadService Create(Session session) => new([session], new Store(), Options.Create(new DownloadOptions { MaxConcurrency = 2 }), NullLogger<DownloadService>.Instance);
    private static DownloadRequest Request => new(QslService.Eqsl, new("test", "secret"), "output");

    [Fact]
    public async Task WaitsForEveryDownloadAndReportsPartialFailure()
    {
        var session = new Session();
        var updates = new System.Collections.Concurrent.ConcurrentBag<DownloadProgress>();
        var operation = Create(session).DownloadAsync(Request, new CallbackProgress<DownloadProgress>(updates.Add));
        await session.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.False(operation.IsCompleted);
        session.Release.SetResult();
        var result = await operation;
        Assert.Equal(4, result.Downloaded); Assert.Equal(1, result.Skipped); Assert.Equal(1, result.Failed);
        Assert.Equal(5, session.Finished); Assert.InRange(session.Peak, 1, 2); Assert.True(session.Disposed);
        var itemUpdates = updates.Where(x => x.Item is not null).ToArray();
        Assert.Equal(6, itemUpdates.Length);
        Assert.Single(itemUpdates, x => x.Item!.Status == ItemStatus.Failed && x.Item.Failure == FailureKind.Storage);
        Assert.Equal(4, itemUpdates.Max(x => x.Downloaded));
        Assert.Equal(1, itemUpdates.Max(x => x.Skipped));
        Assert.Equal(1, itemUpdates.Max(x => x.Failed));
    }
    [Fact]
    public async Task CancellationWaitsForWorkerCleanupAndDisposesSession()
    {
        var session = new Session();
        using var cts = new CancellationTokenSource();
        var operation = Create(session).DownloadAsync(Request, cancellationToken: cts.Token);
        await session.Started.Task.WaitAsync(TimeSpan.FromSeconds(2)); cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
        Assert.Equal(0, session.Active); Assert.True(session.Disposed);
    }
    [Fact]
    public void DependencyGraphResolvesAndRejectsInvalidOptions()
    {
        using var services = new ServiceCollection().AddSq7MruUtils(o => o.MaxConcurrency = 0).BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
        Assert.NotNull(services.GetRequiredService<IDownloadService>());
        Assert.NotNull(services.GetRequiredService<IEqslAccountService>());
        Assert.NotNull(services.GetRequiredService<IAdifUploader>());
        Assert.Throws<OptionsValidationException>(() => services.GetRequiredService<IOptions<DownloadOptions>>().Value);
        Assert.DoesNotContain("secret", new ServiceCredentials("test", "secret").ToString());
    }
}
