using System.Net;
using System.Net.Sockets;
using System.Text;
using SQ7MRU.Utils.Infrastructure;

namespace SQ7MRU.Utils.Tests;

public class CookieSessionTests
{
    // Route production URLs to a loopback HTTP server, retaining HttpClientHandler's real cookie processing.
    private sealed class LoopbackHandler(Uri endpoint, HttpMessageHandler inner) : DelegatingHandler(inner)
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var original = request.RequestUri!;
            request.RequestUri = new(endpoint, original.PathAndQuery);
            try { return await base.SendAsync(request, cancellationToken); }
            finally { request.RequestUri = original; }
        }
    }

    [Theory]
    [InlineData(QslService.Eqsl)]
    [InlineData(QslService.Epc)]
    public async Task CookieContainerKeepsSeedAndRedirectCookiesForSubsequentRequests(QslService service)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var endpoint = new Uri($"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}/");
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var observed = new List<string>();
        var server = Task.Run(async () =>
        {
            for (var step = 0; step < 4; step++)
            {
                using var socket = await listener.AcceptTcpClientAsync(cancellation.Token);
                await using var stream = socket.GetStream();
                using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
                var requestLine = await reader.ReadLineAsync(cancellation.Token);
                var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                while (await reader.ReadLineAsync(cancellation.Token) is { Length: > 0 } line)
                {
                    var separator = line.IndexOf(':');
                    headers[line[..separator]] = line[(separator + 1)..].Trim();
                }
                if (headers.TryGetValue("Content-Length", out var length) && int.Parse(length) > 0)
                {
                    var body = new char[int.Parse(length)];
                    await reader.ReadBlockAsync(body.AsMemory(), cancellation.Token);
                }
                observed.Add(headers.GetValueOrDefault("Cookie", ""));
                var extra = step switch
                {
                    0 => "Set-Cookie: CFID=seed; Path=/qslcard/; HttpOnly\r\n",
                    1 => "Set-Cookie: CFTOKEN=authenticated; Path=/qslcard/; HttpOnly\r\nLocation: /qslcard/Welcome.cfm\r\n",
                    _ => ""
                };
                var response = Encoding.ASCII.GetBytes($"HTTP/1.1 {(step == 1 ? "302 Found" : "200 OK")}\r\n{extra}Content-Length: 2\r\nConnection: close\r\n\r\nOK");
                await stream.WriteAsync(response, cancellation.Token);
                Assert.NotNull(requestLine);
            }
        }, cancellation.Token);
        var cookies = new CookieContainer();
        using var scheduler = new ServiceRequestScheduler(TimeProvider.System);
        using var session = new ServiceHttpSession(new HttpClient(new LoopbackHandler(endpoint,
            new HttpClientHandler { UseCookies = true, CookieContainer = cookies, AllowAutoRedirect = false, UseProxy = false })),
            service, new() { MinimumRequestInterval = TimeSpan.Zero }, scheduler, TimeProvider.System);
        try
        {
            await session.GetTextAsync("/qslcard/Login.cfm", cancellation.Token);
            await session.PostFormAsync("/qslcard/LoginFinish.cfm", [], cancellation.Token);
            await session.GetTextAsync("/qslcard/MyAccounts.cfm", cancellation.Token);
            await server;
            Assert.Empty(observed[0]);
            Assert.Contains("CFID=seed", observed[1]);
            foreach (var header in observed.Skip(2))
            {
                Assert.Contains("CFID=seed", header);
                Assert.Contains("CFTOKEN=authenticated", header);
            }
        }
        finally
        {
            await cancellation.CancelAsync();
            listener.Stop();
            try { await server; } catch (OperationCanceledException) { } catch (SocketException) { }
        }
    }
}

