using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Time.Testing;
using SQ7MRU.Utils.Infrastructure;

namespace SQ7MRU.Utils.Tests;

public sealed class CallbackProgress<T>(Action<T> callback) : IProgress<T>
{
    public void Report(T value) => callback(value);
}
public sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request, cancellationToken);
}
public class TransportTests
{
    [Fact]
    public async Task NotFoundIdentifiesFinalEndpointWithoutLeakingQueryParameters()
    {
        var calls = 0;
        using var http = new ServiceHttpSession(new(new StubHandler((_, _) =>
        {
            if (++calls == 1)
            {
                var redirect = new HttpResponseMessage(HttpStatusCode.Found);
                redirect.Headers.Location = new("/qslcard/missing.cfm?Password=secret&Callsign=private", UriKind.Relative);
                return Task.FromResult(redirect);
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        })), QslService.Eqsl, new() { MinimumRequestInterval = TimeSpan.Zero }, new(TimeProvider.System), TimeProvider.System);
        var error = await Assert.ThrowsAsync<ServiceException>(() => http.GetAsync("DownloadInBox.cfm?Password=secret", default));
        Assert.Equal(FailureKind.NotFound, error.Kind);
        Assert.Contains("GET /qslcard/missing.cfm", error.Message);
        Assert.DoesNotContain("secret", error.Message);
        Assert.DoesNotContain("private", error.Message);
        Assert.DoesNotContain("?", error.Message);
    }

    [Fact]
    public async Task RejectsCrossOriginRedirectBeforeReplayingCredentials()
    {
        var calls = 0;
        using var http = new ServiceHttpSession(new(new StubHandler((_, _) =>
        {
            calls++;
            var response = new HttpResponseMessage(HttpStatusCode.TemporaryRedirect);
            response.Headers.Location = new("https://unexpected.example/login");
            return Task.FromResult(response);
        })), QslService.Eqsl, new(), new(TimeProvider.System), TimeProvider.System);
        var error = await Assert.ThrowsAsync<ServiceException>(() => http.PostFormAsync("LoginFinish.cfm",
            new Dictionary<string, string> { ["password"] = "secret" }, default));
        Assert.Equal(FailureKind.InvalidResponse, error.Kind);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task FollowsSameOriginLoginRedirectAsGet()
    {
        var calls = 0;
        using var http = new ServiceHttpSession(new(new StubHandler((request, _) =>
        {
            if (++calls == 1)
            {
                var response = new HttpResponseMessage(HttpStatusCode.Found);
                response.Headers.Location = new("/qslcard/Welcome.cfm", UriKind.Relative);
                return Task.FromResult(response);
            }
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Null(request.Content);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("Welcome") });
        })), QslService.Eqsl, new() { MinimumRequestInterval = TimeSpan.Zero }, new(TimeProvider.System), TimeProvider.System);
        Assert.Equal("Welcome", await http.PostFormAsync("LoginFinish.cfm", [], default));
        Assert.Equal(2, calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HonorsRetryAfterDeltaAndDate(bool date)
    {
        var clock = new FakeTimeProvider();
        var start = clock.GetUtcNow();
        var calls = 0;
        var handler = new StubHandler((_, _) =>
        {
            var response = new HttpResponseMessage(++calls == 1 ? HttpStatusCode.TooManyRequests : HttpStatusCode.OK) { Content = new StringContent("ok") };
            if (calls == 1) response.Headers.RetryAfter = date ? new RetryConditionHeaderValue(start.AddSeconds(30)) : new RetryConditionHeaderValue(TimeSpan.FromSeconds(30));
            return Task.FromResult(response);
        });
        using var http = new ServiceHttpSession(new(handler), QslService.Eqsl, new() { MinimumRequestInterval = TimeSpan.Zero },
            new(clock), clock, new CallbackProgress<DownloadProgress>(p => clock.Advance(p.RetryAfter!.Value)));
        await http.GetAsync("test", default);
        Assert.Equal(2, calls);
        Assert.True(clock.GetUtcNow() >= start.AddSeconds(30));
    }

    [Fact]
    public async Task HtmlRateLimitHasBoundedRetries()
    {
        var clock = new FakeTimeProvider();
        var calls = 0;
        using var http = new ServiceHttpSession(new(new StubHandler((_, _) =>
        {
            calls++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ERROR - Too many queries overloading the system. Slow down!") });
        })), QslService.Eqsl, new() { MaxRetries = 2, MinimumRequestInterval = TimeSpan.Zero }, new(clock), clock,
            new CallbackProgress<DownloadProgress>(p => clock.Advance(p.RetryAfter!.Value)));
        var error = await Assert.ThrowsAsync<ServiceException>(() => http.GetAsync("test", default));
        Assert.Equal(FailureKind.RateLimited, error.Kind);
        Assert.Equal(3, calls);
    }

    [Fact]
    public async Task CancellationInterruptsCooldown()
    {
        var clock = new FakeTimeProvider();
        using var cts = new CancellationTokenSource();
        using var http = new ServiceHttpSession(new(new StubHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.TooManyRequests)))),
            QslService.Eqsl, new(), new(clock), clock, new CallbackProgress<DownloadProgress>(_ => cts.Cancel()));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => http.GetAsync("test", cts.Token));
    }

    [Theory]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task UnsafePostsAreNeverReplayed(HttpStatusCode status)
    {
        var calls = 0;
        using var http = new ServiceHttpSession(new(new StubHandler((_, _) =>
        {
            calls++; return Task.FromResult(new HttpResponseMessage(status));
        })), QslService.Eqsl, new(), new(TimeProvider.System), TimeProvider.System);
        await Assert.ThrowsAsync<ServiceException>(() => http.PostFormAsync("UploadFile.cfm", [], default));
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task CooldownIsSharedPerServiceAndDoesNotBlockOtherServices()
    {
        var clock = new FakeTimeProvider();
        var scheduler = new ServiceRequestScheduler(clock);
        await scheduler.DeferAsync(QslService.Eqsl, TimeSpan.FromSeconds(10), default);
        var waiting = scheduler.WaitAsync(QslService.Eqsl, TimeSpan.Zero, default);
        Assert.False(waiting.IsCompleted);
        await scheduler.WaitAsync(QslService.Epc, TimeSpan.Zero, default);
        clock.Advance(TimeSpan.FromSeconds(10));
        await waiting.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task RequestTimeoutIsClassified()
    {
        var clock = new FakeTimeProvider();
        using var http = new ServiceHttpSession(new(new StubHandler(async (_, token) =>
        {
            var wait = Task.Delay(Timeout.InfiniteTimeSpan, token);
            clock.Advance(TimeSpan.FromSeconds(5));
            await wait;
            return new(HttpStatusCode.OK);
        })), QslService.Eqsl, new() { RequestTimeout = TimeSpan.FromSeconds(5), MaxRetries = 0 }, new(clock), clock);
        var error = await Assert.ThrowsAsync<ServiceException>(() => http.GetAsync("test", default));
        Assert.Equal(FailureKind.Timeout, error.Kind);
    }
}
