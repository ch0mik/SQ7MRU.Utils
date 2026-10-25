using System.Collections.Concurrent;
using System.Net;
using System.Text;
using Microsoft.Extensions.Options;

namespace SQ7MRU.Utils.Infrastructure;

public sealed class ServiceHttpOptions
{
    public int MaxRetries { get; set; } = 4;
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(60);
    public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(2);
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromMinutes(1);
    public TimeSpan EqslCardInterval { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan MinimumRequestInterval { get; set; } = TimeSpan.FromMilliseconds(500);
}

public interface IServiceHttpSessionFactory
{
    ServiceHttpSession Create(QslService service, IProgress<DownloadProgress>? progress = null);
}

// One handler per operation keeps authenticated cookies out of pooled/shared handlers.
public sealed class ServiceHttpSessionFactory(IOptions<ServiceHttpOptions> options,
    ServiceRequestScheduler scheduler, TimeProvider clock) : IServiceHttpSessionFactory
{
    public ServiceHttpSession Create(QslService service, IProgress<DownloadProgress>? progress = null)
    {
        var handler = new HttpClientHandler { UseCookies = true, CookieContainer = new CookieContainer(), AutomaticDecompression = DecompressionMethods.All, AllowAutoRedirect = false };
        var client = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan, MaxResponseContentBufferSize = 32 * 1024 * 1024 };
        var version = typeof(ServiceHttpSessionFactory).Assembly.GetName().Version?.ToString(3) ?? "unknown";
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"SQ7MRU.Utils/{version}");
        return new(client, service, options.Value, scheduler, clock, progress);
    }
}

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

    public Task WaitAsync(QslService service, TimeSpan interval, CancellationToken token) => WaitCoreAsync(service, false, interval, token);
    public Task WaitForCardAsync(TimeSpan interval, CancellationToken token, IProgress<DownloadProgress>? progress = null) =>
        WaitCoreAsync(QslService.Eqsl, true, interval, token, progress);

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
    public void Dispose()
    {
        foreach (var state in states.Values) state.Gate.Dispose();
    }
}

public sealed class ServiceHttpSession(HttpClient client, QslService service, ServiceHttpOptions options,
    ServiceRequestScheduler scheduler, TimeProvider clock, IProgress<DownloadProgress>? progress = null) : IDisposable
{
    public Uri BaseAddress { get; } = service switch
    {
        QslService.Eqsl => new("https://eqsl.cc/qslcard/"),
        QslService.HrdLog => new("https://www.hrdlog.net/"),
        QslService.Epc => new("https://epc-mc.eu/"),
        _ => throw new ArgumentOutOfRangeException(nameof(service))
    };

    public Uri Resolve(string resource)
    {
        if (!Uri.TryCreate(BaseAddress, WebUtility.HtmlDecode(resource), out var uri) ||
            !uri.Host.Equals(BaseAddress.Host, StringComparison.OrdinalIgnoreCase))
            throw new ServiceException(FailureKind.InvalidResponse, "The service returned an unexpected download address.");
        // Old service pages sometimes still return absolute HTTP image links.
        return new UriBuilder(uri) { Scheme = "https", Port = -1 }.Uri;
    }

    public Task<byte[]> GetAsync(string resource, CancellationToken token) =>
        SendAsync(() => new(HttpMethod.Get, Resolve(resource)), true, token);

    public async Task<string> GetTextAsync(string resource, CancellationToken token) =>
        Encoding.UTF8.GetString(await GetAsync(resource, token).ConfigureAwait(false));

    public async Task<string> PostFormAsync(string resource, IEnumerable<KeyValuePair<string, string>> fields,
        CancellationToken token, bool retrySafe = false)
    {
        var values = fields.ToArray();
        var bytes = await SendAsync(() => new(HttpMethod.Post, Resolve(resource))
            { Content = new FormUrlEncodedContent(values) }, retrySafe, token).ConfigureAwait(false);
        return Encoding.UTF8.GetString(bytes);
    }

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
    public void Dispose() => client.Dispose();
}
