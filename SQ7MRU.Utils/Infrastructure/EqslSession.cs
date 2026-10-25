using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace SQ7MRU.Utils.Infrastructure;

internal sealed class EqslAccountService(IServiceHttpSessionFactory factory) : IEqslAccountService
{
    public async Task<IReadOnlyList<ServiceAccount>> GetAccountsAsync(ServiceCredentials credentials, CancellationToken cancellationToken = default)
    {
        using var http = factory.Create(QslService.Eqsl);
        await EqslProtocol.LoginAsync(http, credentials, null, cancellationToken).ConfigureAwait(false);
        return await EqslProtocol.AccountsAsync(http, credentials, cancellationToken).ConfigureAwait(false);
    }
}

internal sealed class EqslSessionFactory(IServiceHttpSessionFactory factory, IOptions<ArtifactStorageOptions> options) : IDownloadSessionFactory
{
    public QslService Service => QslService.Eqsl;
    public Task<IDownloadSession> OpenAsync(ServiceCredentials credentials, IProgress<DownloadProgress>? progress, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IDownloadSession>(new EqslSession(factory, credentials, progress,
            ArtifactStorageOptions.ValidateDirectoryName(options.Value.TechnicalDirectoryName)));
    }
}

internal sealed class EqslSession(IServiceHttpSessionFactory factory, ServiceCredentials credentials,
    IProgress<DownloadProgress>? progress, string technicalDirectoryName) : IDownloadSession
{
    private readonly List<ServiceHttpSession> sessions = [];
    private readonly ConcurrentDictionary<string, (ServiceHttpSession Http, string Url, byte[]? Content, string? FallbackUrl)> resources = new();

    public async IAsyncEnumerable<DownloadItem> DiscoverAsync(ServiceAccount? account,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var initial = factory.Create(QslService.Eqsl, progress);
        sessions.Add(initial);
        await EqslProtocol.LoginAsync(initial, credentials, account, cancellationToken).ConfigureAwait(false);
        var accounts = account is null ? await EqslProtocol.AccountsAsync(initial, credentials, cancellationToken).ConfigureAwait(false) : [account];
        foreach (var selected in accounts)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var http = factory.Create(QslService.Eqsl, progress);
            sessions.Add(http);
            await EqslProtocol.LoginAsync(http, credentials, selected, cancellationToken).ConfigureAwait(false);
            var page = await EqslProtocol.GetTextAsync(http, "DownloadInBox.cfm?UserName=" + ServiceHtml.Query(selected.CallSign) +
                "&Password=" + ServiceHtml.Query(credentials.Password!) + "&QTHNickname=" + ServiceHtml.Query(selected.Qth), "inbox export", cancellationToken).ConfigureAwait(false);
            ServiceHtml.CheckAuthentication(page);
            var adifUrl = EqslProtocol.AdifDownloadAddress(page, http);
            var adif = await EqslProtocol.GetBytesAsync(http, adifUrl, "ADIF download", cancellationToken).ConfigureAwait(false);
            var folder = ArtifactStore.SafeName(selected.CallSign + (selected.Qth.Length > 0 ? "_" + selected.Qth : ""));
            var key = Guid.NewGuid().ToString("N");
            resources.TryAdd(key, (http, "", adif, null));
            yield return new(Path.Combine(technicalDirectoryName, folder + ".adif"), key, true);
            using var reader = new AdifReader(Encoding.UTF8.GetString(adif));
            List<AdifRow> rows;
            try { rows = reader.GetAdifRows(cancellationToken); }
            catch (FormatException) { throw new ServiceException(FailureKind.InvalidResponse, "eQSL returned malformed ADIF."); }
            foreach (var qso in rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var rawTime = (qso.TIME_ON ?? "").PadRight(4, '0');
                if (!DateTime.TryParseExact(qso.QSO_DATE + rawTime[..4], "yyyyMMddHHmm", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var date) || string.IsNullOrWhiteSpace(qso.BAND) || string.IsNullOrWhiteSpace(qso.CALL))
                    throw new ServiceException(FailureKind.InvalidResponse, "ADIF record lacks a valid QSO date, callsign or band.");
                var mode = qso.SUBMODE ?? qso.MODE ?? "";
                var url = "DisplayeQSL.cfm?Callsign=" + ServiceHtml.Query(qso.CALL) +
                    "&VisitorCallsign=" + ServiceHtml.Query(selected.CallSign) +
                    "&QSODate=" + ServiceHtml.Query(date.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + ":00.0") +
                    "&Band=" + ServiceHtml.Query(qso.BAND) + "&Mode=" + ServiceHtml.Query(mode);
                var filename = ArtifactStore.SafeName($"{qso.CALL}_{date:yyyyMMdd_HHmm}_{qso.BAND}_{mode}.jpg");
                key = Guid.NewGuid().ToString("N");
                var fallback = !string.IsNullOrWhiteSpace(qso.MODE) && !mode.Equals(qso.MODE, StringComparison.OrdinalIgnoreCase)
                    ? url[..url.LastIndexOf("&Mode=", StringComparison.Ordinal)] + "&Mode=" + ServiceHtml.Query(qso.MODE) : null;
                resources.TryAdd(key, (http, url, null, fallback));
                yield return new(Path.Combine(folder, filename), key);
            }
        }
    }

    public async Task<byte[]> FetchAsync(DownloadItem item, CancellationToken cancellationToken)
    {
        var resource = resources[item.Resource];
        if (resource.Content is not null) return resource.Content;
        var page = await EqslProtocol.GetTextAsync(resource.Http, resource.Url, "QSL lookup", cancellationToken).ConfigureAwait(false);
        ServiceHtml.CheckAuthentication(page);
        if (page.Contains("There is no entry for a QSO", StringComparison.OrdinalIgnoreCase) && resource.FallbackUrl is not null)
        {
            page = await EqslProtocol.GetTextAsync(resource.Http, resource.FallbackUrl, "QSL lookup by MODE", cancellationToken).ConfigureAwait(false);
            ServiceHtml.CheckAuthentication(page);
        }
        if (page.Contains("There is no entry for a QSO", StringComparison.OrdinalIgnoreCase))
            throw new ServiceException(FailureKind.NotFound, "No confirmed eQSL for this QSO.");
        var image = Regex.Match(page, @"/CFFileServlet/_cf_image/[^\s""'<>]+", RegexOptions.IgnoreCase);
        if (!image.Success) throw new ServiceException(FailureKind.InvalidResponse, "eQSL image link is missing.");
        return await EqslProtocol.GetBytesAsync(resource.Http, image.Value, "QSL image download", cancellationToken).ConfigureAwait(false);
    }
    public ValueTask DisposeAsync()
    {
        foreach (var session in sessions) session.Dispose();
        return ValueTask.CompletedTask;
    }
}

internal sealed class AdifUploader(IServiceHttpSessionFactory factory) : IAdifUploader
{
    public async Task UploadAsync(string adif, ServiceCredentials credentials, ServiceAccount? account = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(adif);
        using var reader = new AdifReader(adif);
        if (reader.GetAdifRows(cancellationToken).Count == 0) throw new ArgumentException("ADIF must contain QSO records.", nameof(adif));
        using var http = factory.Create(QslService.Eqsl);
        await EqslProtocol.LoginAsync(http, credentials, account, cancellationToken).ConfigureAwait(false);
        var bytes = await http.SendAsync(() =>
        {
            var content = new MultipartFormDataContent();
            content.Add(new StringContent("TRUE"), "AsyncMode");
            content.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(adif)), "Filename", "UPLOAD.ADIF");
            return new(HttpMethod.Post, http.Resolve("UploadFile.cfm")) { Content = content };
        }, false, cancellationToken).ConfigureAwait(false);
        var response = Encoding.UTF8.GetString(bytes);
        ServiceHtml.CheckAuthentication(response);
        if (!EqslUploadResponse.IsSuccess(response))
            throw new ServiceException(FailureKind.InvalidResponse, "Upload outcome could not be confirmed. Check eQSL before resubmitting.");
    }
}

