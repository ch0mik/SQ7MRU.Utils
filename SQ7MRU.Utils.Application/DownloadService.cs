using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SQ7MRU.Utils;

/// <summary>Controls worker concurrency for the built-in download use case.</summary>
/// <remarks>Configure through AddSq7MruUtils. DI validation requires every configured concurrency to be between 1 and 32.</remarks>
public sealed class DownloadOptions
{
    /// <summary>Gets or sets the fallback maximum number of concurrent item workers. Defaults to two.</summary>
    public int MaxConcurrency { get; set; } = 2;
    /// <summary>Gets or sets service-specific worker limits; an eQSL override of one is provided by default.</summary>
    /// <remarks>A registered override takes precedence over <see cref="MaxConcurrency"/>. Do not mutate options while a job runs.</remarks>
    public Dictionary<QslService, int> MaxConcurrencyByService { get; set; } = new() { [QslService.Eqsl] = 1 };
}
/// <summary>Implements streaming discovery, bounded worker execution and partial-result aggregation.</summary>
/// <param name="factories">Factories with one registration for each supported service.</param>
/// <param name="store">Storage adapter used to validate and atomically persist payloads.</param>
/// <param name="options">Validated concurrency settings.</param>
/// <param name="logger">Receives classified failure events without credentials or payloads.</param>
/// <remarks>Prefer resolving <see cref="IDownloadService"/> from the service provider. Each call owns and asynchronously disposes its session after all workers finish.</remarks>
public sealed class DownloadService(IEnumerable<IDownloadSessionFactory> factories, IArtifactStore store,
    IOptions<DownloadOptions> options, ILogger<DownloadService> logger) : IDownloadService
{
    /// <summary>Discovers and processes download selections, skipping valid local content unless refresh is required.</summary>
    /// <param name="request">Service, credentials, output root and optional eQSL account.</param>
    /// <param name="progress">Optional receiver; callbacks can originate from concurrent workers and should not throw.</param>
    /// <param name="cancellationToken">Cancels discovery, worker requests, service waits and persistence.</param>
    /// <returns>Completed logical-selection outcomes sorted by name; inspect the failed count for partial failure.</returns>
    /// <exception cref="ArgumentNullException">Request or credentials are null.</exception>
    /// <exception cref="ArgumentException">Login or output directory is blank.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The service has no registered factory.</exception>
    /// <exception cref="InvalidOperationException">More than one factory is registered for the service.</exception>
    /// <exception cref="ServiceException">Authentication fails or session opening fails.</exception>
    /// <exception cref="OperationCanceledException">Caller cancellation stops the operation after worker cleanup.</exception>
    /// <remarks>ADIF exports always refresh. A non-authentication per-item service error becomes a failed item. A discovery error adds a discovery failure after prior work finishes. One ZIP selection counts once even if it writes several certificates. Cancellation and fatal errors do not return a final result.</remarks>
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
