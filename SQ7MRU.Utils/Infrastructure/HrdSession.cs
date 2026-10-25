using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace SQ7MRU.Utils.Infrastructure;

internal sealed class HrdSessionFactory(IServiceHttpSessionFactory factory) : IDownloadSessionFactory
{
    public QslService Service => QslService.HrdLog;
    public Task<IDownloadSession> OpenAsync(ServiceCredentials credentials, IProgress<DownloadProgress>? progress, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IDownloadSession>(new HrdSession(factory.Create(Service, progress), credentials));
    }
}

internal sealed class HrdSession(ServiceHttpSession http, ServiceCredentials credentials) : IDownloadSession
{
    public async IAsyncEnumerable<DownloadItem> DiscoverAsync(ServiceAccount? account,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // HRDLog uses a callsign cookie as well as the search form.
        var page = await http.SendAsync(() =>
        {
            var request = new HttpRequestMessage(HttpMethod.Post, http.Resolve("searchqso.aspx?log=" + ServiceHtml.Query(credentials.Login)));
            request.Headers.TryAddWithoutValidation("Cookie", "callsign=" + ServiceHtml.Query(credentials.Login.ToUpperInvariant()));
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["ctl00$ContentPlaceHolder1$TbCallsign"] = credentials.Login,
                ["ctl00$ContentPlaceHolder1$CbAllQsl"] = "on"
            });
            return request;
        }, true, cancellationToken).ConfigureAwait(false); // Read-only search, safe to replay.
        var html = System.Text.Encoding.UTF8.GetString(page);
        var matches = Regex.Matches(html, @"PrintQsl\.aspx\?id=(?<id>[a-zA-Z0-9_-]+)", RegexOptions.IgnoreCase);
        if (matches.Count == 0 && !html.Contains("No QSO", StringComparison.OrdinalIgnoreCase) &&
            !html.Contains("No records", StringComparison.OrdinalIgnoreCase))
            throw new ServiceException(FailureKind.InvalidResponse, "HRDLog returned no recognizable QSL links or empty-result message.");
        foreach (Match match in matches)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var id = match.Groups["id"].Value;
            yield return new(Path.Combine("iQSL_HRDLOG", ArtifactStore.SafeName(credentials.Login), id + ".jpg"), "qsl.aspx?id=" + id);
        }
    }
    public Task<byte[]> FetchAsync(DownloadItem item, CancellationToken cancellationToken) => http.GetAsync(item.Resource, cancellationToken);
    public ValueTask DisposeAsync() { http.Dispose(); return ValueTask.CompletedTask; }
}
