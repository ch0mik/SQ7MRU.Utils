using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SQ7MRU.Utils;

public sealed class DownloadOptions
{
    public int MaxConcurrency { get; set; } = 2;
    public Dictionary<QslService, int> MaxConcurrencyByService { get; set; } = new() { [QslService.Eqsl] = 1 };
}
public sealed class DownloadService(IEnumerable<IDownloadSessionFactory> factories, IArtifactStore store,
    IOptions<DownloadOptions> options, ILogger<DownloadService> logger) : IDownloadService
{
    public async Task<DownloadResult> DownloadAsync(DownloadRequest request,
        IProgress<DownloadProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Credentials);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Credentials.Login);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OutputDirectory);
        cancellationToken.ThrowIfCancellationRequested();
        var concurrency = options.Value.MaxConcurrencyByService.GetValueOrDefault(request.Service, options.Value.MaxConcurrency);
        var factory = factories.SingleOrDefault(x => x.Service == request.Service)
            ?? throw new ArgumentOutOfRangeException(nameof(request), "Unsupported service.");
        progress?.Report(new(ProgressStage.Discovering, "Discovering available files."));
        await using var session = await factory.OpenAsync(request.Credentials, progress, cancellationToken).ConfigureAwait(false);
        var results = new ConcurrentBag<ItemResult>();
        var completed = 0;
        var downloaded = 0;
        var skipped = 0;
        var failed = 0;
        var total = 0;
        await Parallel.ForEachAsync(DiscoverItems(cancellationToken),
            new ParallelOptions { MaxDegreeOfParallelism = concurrency, CancellationToken = cancellationToken },
            async (item, token) =>
            {
                ItemResult outcome;
                try
                {
                    if (!item.IsAdif && !item.RefreshExisting && await store.IsValidAsync(request.OutputDirectory, item, token).ConfigureAwait(false))
                        outcome = new(item.RelativePath, ItemStatus.Skipped);
                    else
                    {
                        var content = await session.FetchAsync(item, token).ConfigureAwait(false);
                        var savedPath = await store.SaveAsync(request.OutputDirectory, item, content, token).ConfigureAwait(false);
                        outcome = new(savedPath, ItemStatus.Downloaded);
                    }
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
                catch (ServiceException ex) when (ex.Kind != FailureKind.Authentication)
                {
                    outcome = new(item.RelativePath, ItemStatus.Failed, ex.Kind, ex.Message);
                    logger.LogWarning("Download failed: {Kind}", ex.Kind);
                }
                catch (IOException)
                {
                    outcome = new(item.RelativePath, ItemStatus.Failed, FailureKind.Storage, "Cannot access the output file.");
                }
                catch (UnauthorizedAccessException)
                {
                    outcome = new(item.RelativePath, ItemStatus.Failed, FailureKind.Storage, "Output directory is not writable.");
                }
                results.Add(outcome);
                switch (outcome.Status)
                {
                    case ItemStatus.Downloaded: Interlocked.Increment(ref downloaded); break;
                    case ItemStatus.Skipped: Interlocked.Increment(ref skipped); break;
                    case ItemStatus.Failed: Interlocked.Increment(ref failed); break;
                }
                progress?.Report(new(ProgressStage.Downloading, item.RelativePath, Interlocked.Increment(ref completed), Volatile.Read(ref total),
                    Item: outcome, Downloaded: Volatile.Read(ref downloaded), Skipped: Volatile.Read(ref skipped), Failed: Volatile.Read(ref failed)));
            }).ConfigureAwait(false);
        progress?.Report(new(ProgressStage.Completed, "Download completed.", completed, Volatile.Read(ref total)));
        return new(results.OrderBy(x => x.Name, StringComparer.Ordinal).ToArray());

        async IAsyncEnumerable<DownloadItem> DiscoverItems([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken token)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using var enumerator = session.DiscoverAsync(request.Account, token).GetAsyncEnumerator(token);
            while (true)
            {
                bool available;
                try { available = await enumerator.MoveNextAsync().ConfigureAwait(false); }
                catch (ServiceException ex) when (ex.Kind != FailureKind.Authentication)
                {
                    var outcome = new ItemResult("[discovery]", ItemStatus.Failed, ex.Kind, ex.Message);
                    results.Add(outcome);
                    Interlocked.Increment(ref failed);
                    Interlocked.Increment(ref total);
                    progress?.Report(new(ProgressStage.Downloading, outcome.Message!, Interlocked.Increment(ref completed),
                        Volatile.Read(ref total), Item: outcome, Downloaded: Volatile.Read(ref downloaded),
                        Skipped: Volatile.Read(ref skipped), Failed: Volatile.Read(ref failed)));
                    break;
                }
                if (!available) break;
                if (!seen.Add(enumerator.Current.RelativePath)) continue;
                Interlocked.Increment(ref total);
                yield return enumerator.Current;
            }
        }
    }
}
