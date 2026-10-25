using System.Collections.Concurrent;
using System.Net;
using System.Text;
using Microsoft.Extensions.Options;

namespace SQ7MRU.Utils.Infrastructure;

/// <summary>Configures request budgets, bounded retries and shared service pacing.</summary>
/// <remarks>Configure before resolving services. Login/upload POSTs are not automatically retried. OperationTimeout bounds one logical HTTP request, not an entire download job.</remarks>
public sealed class ServiceHttpOptions
{
    /// <summary>Gets or sets retries after the initial replay-safe attempt; default four, DI range zero through twenty.</summary>
    public int MaxRetries { get; set; } = 4;
    /// <summary>Gets or sets each attempt's timeout, including redirect/card-pacing waits; default sixty seconds.</summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(60);
    /// <summary>Gets or sets the total logical-request budget across retries and waits; default five minutes.</summary>
    public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromMinutes(5);
    /// <summary>Gets or sets the exponential retry base; default two seconds, with randomized jitter.</summary>
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(2);
    /// <summary>Gets or sets the computed backoff cap; default one minute. A longer server Retry-After is still honored within the request budget.</summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromMinutes(1);
    /// <summary>Gets or sets initial eQSL card-generation spacing; default and DI minimum one second.</summary>
    /// <remarks>Every recognized eQSL rate limit increases the shared learned spacing by 500 ms. Successful responses do not reduce it; a new scheduler resets it.</remarks>
    public TimeSpan EqslCardInterval { get; set; } = TimeSpan.FromSeconds(1);
    /// <summary>Gets or sets general request spacing per service; default 500 ms and DI minimum zero.</summary>
    public TimeSpan MinimumRequestInterval { get; set; } = TimeSpan.FromMilliseconds(500);
}

/// <summary>Creates independent HTTP sessions while retaining provider-wide pacing and cooldown state.</summary>
public interface IServiceHttpSessionFactory
{
    /// <summary>Creates a session with isolated cookies and an owned HTTP client.</summary>
    /// <param name="service">Service determining the allowed HTTPS host and base path.</param>
    /// <param name="progress">Optional receiver for pacing/retry wait notifications.</param>
    /// <returns>A session the caller must dispose after all operations using it finish.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The service has no supported base address.</exception>
    ServiceHttpSession Create(QslService service, IProgress<DownloadProgress>? progress = null);
}

// One handler per operation keeps authenticated cookies out of pooled/shared handlers.
/// <summary>Creates operation-scoped clients with private cookie jars and guarded redirects.</summary>
/// <param name="options">Validated HTTP configuration.</param>
/// <param name="scheduler">Shared scheduler; the returned sessions do not own or dispose it.</param>
/// <param name="clock">Clock used for delays, server dates and timeout budgets.</param>
public sealed class ServiceHttpSessionFactory(IOptions<ServiceHttpOptions> options,
    ServiceRequestScheduler scheduler, TimeProvider clock) : IServiceHttpSessionFactory
{
    /// <summary>Creates an owned HTTPS session without sending requests or authenticating.</summary>
    /// <param name="service">Service base-address selector.</param>
    /// <param name="progress">Optional wait-notification receiver.</param>
    /// <returns>A disposable session with an isolated CookieContainer and a 32 MiB response-buffer limit.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The service is unsupported.</exception>
    /// <remarks>Automatic redirects are disabled on the handler and validated explicitly by the session. Cookies are never shared between created sessions.</remarks>
    public ServiceHttpSession Create(QslService service, IProgress<DownloadProgress>? progress = null)
    {
        var handler = new HttpClientHandler { UseCookies = true, CookieContainer = new CookieContainer(), AutomaticDecompression = DecompressionMethods.All, AllowAutoRedirect = false };
        var client = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan, MaxResponseContentBufferSize = 32 * 1024 * 1024 };
        var version = typeof(ServiceHttpSessionFactory).Assembly.GetName().Version?.ToString(3) ?? "unknown";
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"SQ7MRU.Utils/{version}");
        return new(client, service, options.Value, scheduler, clock, progress);
    }
}

/// <summary>Serializes request reservations and shares per-service cooldowns across concurrent sessions.</summary>
/// <param name="clock">Clock for reservations and cancellable delays; use the same clock as the transport.</param>
/// <remarks>Thread-safe while alive. Learned eQSL spacing remains in memory for this scheduler's lifetime. Dispose only after all sessions and waiters finish.</remarks>
public sealed class ServiceRequestScheduler(TimeProvider clock) : IDisposable
{
    private sealed class State
    {
        public readonly SemaphoreSlim Gate = new(1, 1);
        public DateTimeOffset Next;
        public DateTimeOffset NextCard;
        public DateTimeOffset? LastCard;
        public TimeSpan CardInterval;
    }
    private readonly ConcurrentDictionary<QslService, State> states = new();

    /// <summary>Waits for a service cooldown and reserves the next general-request slot.</summary>
    /// <param name="service">Independent service scheduling key.</param>
    /// <param name="interval">Minimum general-request spacing to reserve; supply a nonnegative value.</param>
    /// <param name="token">Cancels waiting without sending a request.</param>
    /// <returns>A task completing when the caller may attempt its request; the first unblocked slot is immediate.</returns>
    /// <exception cref="OperationCanceledException">Caller cancellation is requested.</exception>
    public Task WaitAsync(QslService service, TimeSpan interval, CancellationToken token) => WaitCoreAsync(service, false, interval, token);
    /// <summary>Waits for both shared eQSL cooldown and card-generation reservations, then reserves a card slot.</summary>
    /// <param name="interval">Initial spacing floor; a larger learned interval takes precedence.</param>
    /// <param name="token">Cancels a pending card reservation wait.</param>
    /// <param name="progress">Optional receiver for the actual remaining wait before each delay.</param>
    /// <returns>A task completing when the card request may proceed.</returns>
    /// <exception cref="OperationCanceledException">Caller cancellation is requested.</exception>
    /// <remarks>Reservations are shared across sessions and retries. Waking waiters recheck the state, so later rate limits can extend an existing wait.</remarks>
    public Task WaitForCardAsync(TimeSpan interval, CancellationToken token, IProgress<DownloadProgress>? progress = null) =>
        WaitCoreAsync(QslService.Eqsl, true, interval, token, progress);

    /// <summary>Raises shared eQSL generation spacing by 500 ms and extends a pending reservation.</summary>
    /// <param name="initialInterval">Configured initial floor used before the first learned adjustment.</param>
    /// <param name="token">Cancels waiting for the scheduler's state lock.</param>
    /// <returns>A task completing after the adjustment has been applied.</returns>
    /// <exception cref="OperationCanceledException">Caller cancellation is requested.</exception>
    /// <remarks>Call only for recognized rate-limit responses. Network errors and successes must not reset or raise the learned value.</remarks>
    public async Task IncreaseEqslCardIntervalAsync(TimeSpan initialInterval, CancellationToken token)
    {
        var state = states.GetOrAdd(QslService.Eqsl, _ => new());
        await state.Gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            state.CardInterval = (state.CardInterval > initialInterval ? state.CardInterval : initialInterval) + TimeSpan.FromMilliseconds(500);
            if (state.LastCard is { } lastCard) state.NextCard = lastCard + state.CardInterval;
        }
        finally { state.Gate.Release(); }
    }

    private async Task WaitCoreAsync(QslService service, bool card, TimeSpan interval, CancellationToken token,
        IProgress<DownloadProgress>? progress = null)
    {
        var state = states.GetOrAdd(service, _ => new());
        while (true)
        {
            TimeSpan wait;
            await state.Gate.WaitAsync(token).ConfigureAwait(false);
            try
            {
                if (card && interval > state.CardInterval)
                {
                    state.CardInterval = interval;
                    if (state.LastCard is { } lastCard) state.NextCard = lastCard + state.CardInterval;
                }
                var next = card && state.NextCard > state.Next ? state.NextCard : state.Next;
                wait = next - clock.GetUtcNow();
                if (wait <= TimeSpan.Zero)
                {
                    if (card)
                    {
                        state.LastCard = clock.GetUtcNow();
                        state.NextCard = state.LastCard.Value + state.CardInterval;
                    }
                    else state.Next = clock.GetUtcNow() + interval;
                    return;
                }
            }
            finally { state.Gate.Release(); }
            progress?.Report(new(ProgressStage.WaitingForService, "Waiting for eQSL card generation pacing.", RetryAfter: wait));
            await Task.Delay(wait, clock, token).ConfigureAwait(false);
        }
    }

    /// <summary>Extends a service cooldown without shortening a longer cooldown already in effect.</summary>
    /// <param name="service">Service whose requests must wait.</param>
    /// <param name="delay">Nonnegative cooldown measured from the current clock time.</param>
    /// <param name="token">Cancels waiting for the state lock, not the cooldown itself.</param>
    /// <returns>A task completing after updating the reservation; the method does not wait out the delay.</returns>
    /// <exception cref="OperationCanceledException">Caller cancellation is requested.</exception>
    public async Task DeferAsync(QslService service, TimeSpan delay, CancellationToken token)
    {
        var state = states.GetOrAdd(service, _ => new());
        await state.Gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            var next = clock.GetUtcNow() + delay;
            if (next > state.Next) state.Next = next;
        }
        finally { state.Gate.Release(); }
    }
    /// <summary>Disposes scheduler synchronization primitives after all operations and waiters have completed.</summary>
    /// <remarks>This does not cancel active operations. Do not dispose concurrently with session activity.</remarks>
    public void Dispose()
    {
        foreach (var state in states.Values) state.Gate.Dispose();
    }
}

/// <summary>Executes guarded service requests with owned client lifetime, retries, budgets and shared pacing.</summary>
/// <param name="client">Owned client; disable automatic redirects so every redirect is validated by this session.</param>
/// <param name="service">Supported service selecting the permitted host.</param>
/// <param name="options">Timeout, retry and spacing settings; prefer DI-validated options.</param>
/// <param name="scheduler">Shared scheduler, not owned by the session.</param>
/// <param name="clock">Clock consistent with the scheduler.</param>
/// <param name="progress">Optional receiver for retry and pacing waits.</param>
/// <remarks>Use the factory for production cookie isolation and response limits. Dispose after in-flight requests finish. Resolved URLs and resource strings can contain legacy credentials and must not be logged.</remarks>
public sealed class ServiceHttpSession(HttpClient client, QslService service, ServiceHttpOptions options,
    ServiceRequestScheduler scheduler, TimeProvider clock, IProgress<DownloadProgress>? progress = null) : IDisposable
{
    /// <summary>Gets the service's HTTPS base address used for relative requests and host validation.</summary>
    public Uri BaseAddress { get; } = service switch
    {
        QslService.Eqsl => new("https://eqsl.cc/qslcard/"),
        QslService.HrdLog => new("https://www.hrdlog.net/"),
        QslService.Epc => new("https://epc-mc.eu/"),
        _ => throw new ArgumentOutOfRangeException(nameof(service))
    };

    /// <summary>HTML-decodes and resolves an address, rejecting foreign hosts and enforcing HTTPS.</summary>
    /// <param name="resource">Relative, root-relative or same-host absolute service address.</param>
    /// <returns>The HTTPS URI on the configured host, with the standard port.</returns>
    /// <exception cref="ServiceException">The address cannot be resolved or targets an unexpected host.</exception>
    /// <remarks>Legacy HTTP links are upgraded to HTTPS. Query strings are preserved and can contain credentials; never log the returned URI.</remarks>
    public Uri Resolve(string resource)
    {
        if (!Uri.TryCreate(BaseAddress, WebUtility.HtmlDecode(resource), out var uri) ||
            !uri.Host.Equals(BaseAddress.Host, StringComparison.OrdinalIgnoreCase))
            throw new ServiceException(FailureKind.InvalidResponse, "The service returned an unexpected download address.");
        // Old service pages sometimes still return absolute HTTP image links.
        return new UriBuilder(uri) { Scheme = "https", Port = -1 }.Uri;
    }

    /// <summary>Performs a replay-safe GET and returns its buffered response bytes.</summary>
    /// <param name="resource">Service-relative or permitted same-host address.</param>
    /// <param name="token">Cancels pacing, requests, redirects, retries and response buffering.</param>
    /// <returns>The response payload without artifact-content validation.</returns>
    /// <exception cref="ServiceException">Authentication, URL validation, HTTP handling or request budgets fail.</exception>
    /// <exception cref="OperationCanceledException">Caller cancellation is requested.</exception>
    public Task<byte[]> GetAsync(string resource, CancellationToken token) =>
        SendAsync(() => new(HttpMethod.Get, Resolve(resource)), true, token);

    /// <summary>Performs a replay-safe GET and decodes response bytes as UTF-8.</summary>
    /// <param name="resource">Permitted service address.</param>
    /// <param name="token">Cancels service waits, transport and retries.</param>
    /// <returns>UTF-8 decoded response text; HTML login detection remains the adapter's responsibility.</returns>
    /// <exception cref="ServiceException">Service transport, URL or budget handling fails.</exception>
    /// <exception cref="OperationCanceledException">Caller cancellation is requested.</exception>
    public async Task<string> GetTextAsync(string resource, CancellationToken token) =>
        Encoding.UTF8.GetString(await GetAsync(resource, token).ConfigureAwait(false));

    /// <summary>Submits URL-encoded fields and decodes the buffered response as UTF-8.</summary>
    /// <param name="resource">Permitted service endpoint.</param>
    /// <param name="fields">Form fields snapshotted before sending, allowing safe construction of fresh request content.</param>
    /// <param name="token">Cancels waits, transmission and response reads.</param>
    /// <param name="retrySafe">True only for read-only/idempotent operations whose POST may safely be replayed; false for login and upload.</param>
    /// <returns>UTF-8 response text, without interpreting service-specific success markers.</returns>
    /// <exception cref="ServiceException">Authentication, transport, URL validation or retry/time budgets fail.</exception>
    /// <exception cref="OperationCanceledException">Caller cancellation is requested.</exception>
    /// <remarks>A false replay flag still permits guarded same-host redirects; it disables automatic transient-error retries of the logical POST.</remarks>
    public async Task<string> PostFormAsync(string resource, IEnumerable<KeyValuePair<string, string>> fields,
        CancellationToken token, bool retrySafe = false)
    {
        var values = fields.ToArray();
        var bytes = await SendAsync(() => new(HttpMethod.Post, Resolve(resource))
            { Content = new FormUrlEncodedContent(values) }, retrySafe, token).ConfigureAwait(false);
        return Encoding.UTF8.GetString(bytes);
    }

    /// <summary>Sends a fresh request with guarded redirects, timeout budgets, shared pacing and bounded retries.</summary>
    /// <param name="createRequest">Factory creating a new request and owned content on every invocation. Resolve its URI through <see cref="Resolve"/>; do not reuse disposed requests.</param>
    /// <param name="retrySafe">Whether the complete operation is safe to repeat after a transient failure or uncertain response.</param>
    /// <param name="token">Cancels all waits, attempts and response reads for this logical request.</param>
    /// <returns>Buffered bytes from a successful non-rate-limited response; the adapter/store interprets the payload.</returns>
    /// <exception cref="ServiceException">Authentication, response status, redirect validation, retry exhaustion or timeout budgets fail.</exception>
    /// <exception cref="OperationCanceledException">The caller token is cancelled; internal request-budget exhaustion instead reports a timeout service exception.</exception>
    /// <remarks>The factory is a trusted extension point: its initial URI must already be validated. Retry-After can extend backoff but cannot exceed the logical request budget. Cooldowns are shared even when replay is forbidden. Recognized eQSL rate limits increase card spacing by 500 ms; unrelated transient errors do not. Never replay uploads automatically.</remarks>
    public async Task<byte[]> SendAsync(Func<HttpRequestMessage> createRequest, bool retrySafe, CancellationToken token)
    {
        using var budget = new CancellationTokenSource(options.OperationTimeout, clock);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, budget.Token);
        try
        {
            for (var attempt = 0; ; attempt++)
            {
                await scheduler.WaitAsync(service, options.MinimumRequestInterval, linked.Token).ConfigureAwait(false);
                using var timeout = new CancellationTokenSource(options.RequestTimeout, clock);
                using var requestToken = CancellationTokenSource.CreateLinkedTokenSource(linked.Token, timeout.Token);
                TimeSpan delay = TimeSpan.FromMilliseconds(Math.Min(options.MaxDelay.TotalMilliseconds,
                    options.BaseDelay.TotalMilliseconds * Math.Pow(2, attempt) * (0.8 + Random.Shared.NextDouble() * 0.4)));
                FailureKind failure;
                try
                {
                    using var response = await SendFollowingRedirectsAsync(createRequest, requestToken.Token).ConfigureAwait(false);
                    var bytes = await response.Content.ReadAsByteArrayAsync(requestToken.Token).ConfigureAwait(false);
                    var head = Encoding.UTF8.GetString(bytes, 0, Math.Min(bytes.Length, 65536));
                    var limited = response.StatusCode == HttpStatusCode.TooManyRequests ||
                        head.Contains("Too many queries", StringComparison.OrdinalIgnoreCase) ||
                        head.Contains("rate limit exceeded", StringComparison.OrdinalIgnoreCase);
                    if (!limited && response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                        throw new ServiceException(FailureKind.Authentication, $"Service authentication was rejected ({RequestDescription(response)}).");
                    if (limited || response.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.InternalServerError
                        or HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout)
                    {
                        failure = limited ? FailureKind.RateLimited : FailureKind.Network;
                        var retry = response.Headers.RetryAfter;
                        var serverDelay = retry?.Delta ?? (retry?.Date - clock.GetUtcNow());
                        if (serverDelay > delay) delay = serverDelay.Value;
                    }
                    else
                    {
                        if (!response.IsSuccessStatusCode)
                            throw new ServiceException(response.StatusCode == HttpStatusCode.NotFound ? FailureKind.NotFound : FailureKind.InvalidResponse,
                                $"Service returned HTTP {(int)response.StatusCode} ({RequestDescription(response)}).");
                        return bytes;
                    }
                }
                catch (HttpRequestException) { failure = FailureKind.Network; }
                catch (OperationCanceledException) when (!linked.IsCancellationRequested) { failure = FailureKind.Timeout; }

                // Share the cooldown even if this request cannot safely be replayed.
                if (service == QslService.Eqsl && failure == FailureKind.RateLimited)
                    await scheduler.IncreaseEqslCardIntervalAsync(options.EqslCardInterval, linked.Token).ConfigureAwait(false);
                await scheduler.DeferAsync(service, delay, linked.Token).ConfigureAwait(false);
                if (!retrySafe || attempt >= options.MaxRetries)
                    throw new ServiceException(failure, "Service request failed; retry limit reached or replay is unsafe.");
                if (delay >= options.OperationTimeout)
                    throw new ServiceException(failure, "Service requested a wait exceeding the request time budget.");
                progress?.Report(new(ProgressStage.WaitingForService, "Waiting before retrying the service.", RetryAfter: delay));
            }
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            throw new ServiceException(FailureKind.Timeout, "Service request time budget was exhausted.");
        }
    }
    private async Task<HttpResponseMessage> SendFollowingRedirectsAsync(Func<HttpRequestMessage> createRequest, CancellationToken token)
    {
        Uri? destination = null;
        var useGet = false;
        for (var hop = 0; hop < 10; hop++)
        {
            using var request = useGet ? new HttpRequestMessage(HttpMethod.Get, destination) : createRequest();
            if (destination is not null) request.RequestUri = destination;
            if (service == QslService.Eqsl && request.RequestUri is { } uri &&
                (uri.AbsolutePath.EndsWith("/DisplayeQSL.cfm", StringComparison.OrdinalIgnoreCase) || uri.AbsolutePath.EndsWith("/GeteQSL.cfm", StringComparison.OrdinalIgnoreCase)))
            {
                await scheduler.WaitForCardAsync(options.EqslCardInterval, token, progress).ConfigureAwait(false);
            }
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, token).ConfigureAwait(false);
            response.RequestMessage ??= request;
            if ((int)response.StatusCode is not (301 or 302 or 303 or 307 or 308)) return response;
            try
            {
                if (response.Headers.Location is null)
                    throw new ServiceException(FailureKind.InvalidResponse, "Service redirect has no destination.");
                destination = Resolve(new Uri(request.RequestUri!, response.Headers.Location).ToString());
                useGet = useGet || response.StatusCode == HttpStatusCode.SeeOther ||
                    (request.Method == HttpMethod.Post && (int)response.StatusCode is 301 or 302);
            }
            finally { response.Dispose(); }
            await scheduler.WaitAsync(service, options.MinimumRequestInterval, token).ConfigureAwait(false);
        }
        throw new ServiceException(FailureKind.InvalidResponse, "Service returned too many redirects.");
    }
    private static string RequestDescription(HttpResponseMessage response)
    {
        // Never include query strings: eQSL inbox URLs carry passwords.
        var request = response.RequestMessage;
        return $"{request?.Method.Method ?? "HTTP"} {request?.RequestUri?.AbsolutePath ?? "unknown endpoint"}";
    }
    /// <summary>Disposes the owned HTTP client and handler; the shared scheduler remains owned by the provider.</summary>
    /// <remarks>Finish or cancel and await active requests before disposing the session.</remarks>
    public void Dispose() => client.Dispose();
}
