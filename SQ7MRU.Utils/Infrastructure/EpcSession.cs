using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;

namespace SQ7MRU.Utils.Infrastructure;

internal sealed class EpcSessionFactory(IServiceHttpSessionFactory factory) : IDownloadSessionFactory
{
    public QslService Service => QslService.Epc;
    public Task<IDownloadSession> OpenAsync(ServiceCredentials credentials, IProgress<DownloadProgress>? progress, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IDownloadSession>(new EpcSession(factory.Create(Service, progress), credentials));
    }
}

internal sealed class EpcSession(ServiceHttpSession http, ServiceCredentials credentials) : IDownloadSession
{
    private readonly ConcurrentDictionary<string, (string Action, Dictionary<string, string> Fields)> resources = new();
    private readonly ConcurrentDictionary<string, ServiceException> errors = new();
    public async IAsyncEnumerable<DownloadItem> DiscoverAsync(ServiceAccount? account,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(credentials.Password);
        var doc = ServiceHtml.Parse(await http.GetTextAsync("/", cancellationToken).ConfigureAwait(false));
        var form = doc.GetElementbyId("login-form") ?? throw new ServiceException(FailureKind.InvalidResponse, "EPC login form is missing.");
        var fields = ServiceHtml.Nodes(form, ".//input[@type='hidden']").ToDictionary(x => ServiceHtml.Required(x, "name"), x => x.GetAttributeValue("value", ""));
        fields["username"] = credentials.Login.Trim().ToUpperInvariant();
        fields["password"] = credentials.Password;
        fields["Submit"] = "";
        fields["option"] = "com_users";
        fields["task"] = "user.login";
        var actionUrl = form.GetAttributeValue("action", "index.php?lang=en");
        var page = await http.PostFormAsync(actionUrl, fields, cancellationToken).ConfigureAwait(false);
        ServiceHtml.CheckAuthentication(page, checkLoginForms: false);
        doc = ServiceHtml.Parse(page);
        // Joomla reuses login-form for its authenticated logout form.
        // Only an interactive password field means that login is still required.
        if (ServiceHtml.Nodes(doc.DocumentNode, "//input").Any(input =>
            input.GetAttributeValue("type", "").Equals("password", StringComparison.OrdinalIgnoreCase)))
            throw new ServiceException(FailureKind.Authentication, "EPC login did not establish a session.");
        var groups = EpcProtocol.Groups(doc, http);
        if (groups.Count == 0) throw new ServiceException(FailureKind.InvalidResponse, "EPC award navigation is missing.");
        var restrictedArticles = 0;
        var discoveredAwards = 0;
        foreach (var (title, groupUrl) in groups)
        {
            cancellationToken.ThrowIfCancellationRequested();
            List<(string Name, string Action, Dictionary<string, string> Fields)> awards = [];
            ServiceException? failure = null;
            try
            {
                var html = await http.GetTextAsync(groupUrl, cancellationToken).ConfigureAwait(false);
                var group = ServiceHtml.Parse(html);
                awards = EpcProtocol.Awards(group, title, http.Resolve(groupUrl)).ToList();
                // Menu discovery includes articles with separate access restrictions and
                // pages that retain an embedded login module beside usable award forms.
                // Neither proves that the initial login failed for the entire operation.
                if (awards.Count == 0 && ServiceHtml.Nodes(group.DocumentNode, "//input").Any(input =>
                    input.GetAttributeValue("type", "").Equals("password", StringComparison.OrdinalIgnoreCase)))
                {
                    // Account-management articles also contain password fields. They
                    // are not certificate categories and must never become failed items.
                    restrictedArticles++;
                    continue;
                }
                // Navigation also contains informational articles and empty award groups.
                // They are safe to inspect but are not download items without certificate forms.
            }
            catch (ServiceException ex)
            {
                failure = ex.Kind == FailureKind.Authentication
                    ? new ServiceException(FailureKind.InvalidResponse, $"EPC access to article '{title}' was rejected; other categories will still be checked.")
                    : ex;
            }
            if (failure is not null)
            {
                var key = Guid.NewGuid().ToString("N");
                errors[key] = failure;
                yield return new(Path.Combine("EPC", ArtifactStore.SafeName(credentials.Login), ArtifactStore.SafeName(title), "[discovery]"), key, RefreshExisting: true);
                continue;
            }
            foreach (var award in awards)
            {
                discoveredAwards++;
                var key = Guid.NewGuid().ToString("N");
                resources.TryAdd(key, (award.Action, award.Fields));
                yield return new(Path.Combine("EPC", ArtifactStore.SafeName(credentials.Login), ArtifactStore.SafeName(title), ArtifactStore.SafeName(award.Name) + ".jpg"), key,
                    AllowImageArchive: true, AllowPdf: true);
            }
        }
        if (discoveredAwards == 0 && restrictedArticles > 0)
            throw new ServiceException(FailureKind.Authentication,
                "EPC returned no certificate forms and some articles require authentication; the session may have expired.");
    }
    public async Task<byte[]> FetchAsync(DownloadItem item, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (errors.TryGetValue(item.Resource, out var error)) throw error;
        var resource = resources[item.Resource];
        // This form generates an award file without changing account data.
        var content = await http.SendAsync(() => new(HttpMethod.Post, http.Resolve(resource.Action))
            { Content = new FormUrlEncodedContent(resource.Fields) }, true, cancellationToken).ConfigureAwait(false);
        var endpoint = http.Resolve(resource.Action).AbsolutePath;
        var prefix = Encoding.UTF8.GetString(content, 0, Math.Min(content.Length, 512)).TrimStart('\uFEFF', ' ', '\r', '\n', '\t');
        if (prefix.StartsWith("<!doctype", StringComparison.OrdinalIgnoreCase) ||
            prefix.StartsWith("<html", StringComparison.OrdinalIgnoreCase) ||
            prefix.StartsWith("<form", StringComparison.OrdinalIgnoreCase))
            throw new ServiceException(FailureKind.InvalidResponse,
                $"EPC returned an HTML page instead of a certificate (POST {endpoint}); the certificate form needs verification.");
        return content;
    }
    public ValueTask DisposeAsync() { http.Dispose(); return ValueTask.CompletedTask; }
}
