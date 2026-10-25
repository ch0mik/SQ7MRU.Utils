using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace SQ7MRU.Utils.Infrastructure;

internal static class EqslProtocol
{
    public static async Task LoginAsync(ServiceHttpSession http, ServiceCredentials credentials,
        ServiceAccount? account, CancellationToken token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(credentials.Login);
        ArgumentException.ThrowIfNullOrWhiteSpace(credentials.Password);
        // Match the working 1.x wire protocol. Always authenticate the primary login before
        // switching a linked account: its password may differ, while the session permits switching.
        var fields = new Dictionary<string, string>
        {
            ["Callsign"] = credentials.Login,
            ["EnteredPassword"] = credentials.Password
        };
        var html = await PostLoginAsync(http, fields, "primary login", token).ConfigureAwait(false);
        var selectedRequestedAccount = false;
        for (var step = 0; ; step++)
        {
            var doc = ServiceHtml.Parse(html);
            var choices = ServiceHtml.Nodes(doc.DocumentNode, "//form")
                .Where(f => ServiceHtml.Nodes(f, ".//input").Any(i => i.GetAttributeValue("name", "").Equals("HamID", StringComparison.OrdinalIgnoreCase)))
                .Where(f => f.GetAttributeValue("action", "").Contains("LoginFinish.cfm", StringComparison.OrdinalIgnoreCase) ||
                    ServiceHtml.Nodes(f, ".//input").Any(i => i.GetAttributeValue("name", "").Equals("SelectCallsign", StringComparison.OrdinalIgnoreCase)))
                .ToArray();
            if (choices.Length == 0) break;
            if (step >= 3)
                throw new ServiceException(FailureKind.Authentication, "eQSL account selection did not complete (LoginFinish.cfm).");
            var choice = account?.HamId is null ? choices[0] : choices.FirstOrDefault(f =>
                ServiceHtml.Nodes(f, ".//input").Any(i => i.GetAttributeValue("name", "").Equals("HamID", StringComparison.OrdinalIgnoreCase)
                    && i.GetAttributeValue("value", "") == account.HamId));
            // A linked account may only become switchable after entering the primary account first.
            choice ??= choices[0];
            var ids = ServiceHtml.Nodes(choice, ".//input")
                .Where(i => i.GetAttributeValue("name", "").Equals("HamID", StringComparison.OrdinalIgnoreCase)).ToArray();
            var hamId = ids.FirstOrDefault(i => i.GetAttributeValue("value", "") == account?.HamId) ?? ids[0];
            var selectedHamId = ServiceHtml.Required(hamId, "value");
            selectedRequestedAccount = account?.HamId == selectedHamId;
            fields = SelectionFields(selectedRequestedAccount ? account!.CallSign : credentials.Login, credentials.Password, selectedHamId);
            html = await PostLoginAsync(http, fields, "account selection", token).ConfigureAwait(false);

        }
        if (account?.HamId is not null && !selectedRequestedAccount)
            await PostLoginAsync(http, SelectionFields(account.CallSign, credentials.Password, account.HamId),
                "linked account switch", token).ConfigureAwait(false);
        // LoginFinish can include a login widget even after success. AccountsAsync or the protected
        // download response provides positive evidence of authentication; do not infer it from a widget.
    }

    private static Dictionary<string, string> SelectionFields(string callsign, string password, string hamId) => new()
    {
        ["HamID"] = hamId,
        ["EnteredPassword"] = password,
        ["Callsign"] = callsign,
        // This literal value is intentional: FormUrlEncodedContent encodes '+' as %2B, as in 1.x.
        ["SelectCallsign"] = "Log+In"
    };

    private static async Task<string> PostLoginAsync(ServiceHttpSession http, Dictionary<string, string> fields,
        string stage, CancellationToken token)
    {
        try
        {
            var html = await http.PostFormAsync("LoginFinish.cfm", fields, token).ConfigureAwait(false);
            ServiceHtml.CheckAuthentication(html, checkLoginForms: false);
            return html;
        }
        catch (ServiceException ex)
        {
            throw new ServiceException(ex.Kind, $"eQSL {stage} (LoginFinish.cfm): {ex.Message}", ex);
        }
    }

    public static async Task<IReadOnlyList<ServiceAccount>> AccountsAsync(ServiceHttpSession http,
        ServiceCredentials credentials, CancellationToken token)
    {
        var html = await GetTextAsync(http, "MyAccounts.cfm", "account listing", token).ConfigureAwait(false);
        ServiceHtml.CheckAuthentication(html, checkLoginForms: false);
        var doc = ServiceHtml.Parse(html);
        var accounts = new List<ServiceAccount>();
        foreach (var input in ServiceHtml.Nodes(doc.DocumentNode, "//input[translate(@name,'HAMID','hamid')='hamid']"))
        {
            var form = input.Ancestors("form").FirstOrDefault() ?? input.ParentNode;
            var row = input.Ancestors("tr").FirstOrDefault() ?? form.ParentNode;
            var call = form.SelectSingleNode(".//input[translate(@name,'CALLSIGN','callsign')='callsign']")?.GetAttributeValue("value", "");
            var cell = ServiceHtml.Nodes(row, ".//td").FirstOrDefault(x => x.InnerHtml.Contains("<br", StringComparison.OrdinalIgnoreCase));
            var text = cell is null ? ServiceHtml.Text(row) : ServiceHtml.Text(cell);
            var match = Regex.Match(text, @"(?<call>[A-Za-z0-9/]+)\s*\((?<qth>[^)]*)\)");
            call ??= match.Success ? match.Groups["call"].Value :
                Regex.Match(text, @"\b[A-Za-z0-9]+[0-9][A-Za-z0-9/]+\b").Value;
            var attach = form.GetAttributeValue("action", "").Contains("AttachAccount.cfm", StringComparison.OrdinalIgnoreCase);
            if (!attach && !match.Success) continue;
            if (form.GetAttributeValue("action", "").Contains("LoginFinish.cfm", StringComparison.OrdinalIgnoreCase) &&
                ServiceHtml.Nodes(form, ".//input").Any(i => i.GetAttributeValue("name", "").Equals("SelectCallsign", StringComparison.OrdinalIgnoreCase))) continue;
            if (string.IsNullOrWhiteSpace(call)) call = credentials.Login;
            accounts.Add(new(call, match.Success ? match.Groups["qth"].Value : "", ServiceHtml.Required(input, "value")));
        }
        if (accounts.Count == 0)
        {
            if (!html.Contains("You currently have no other accounts attached.", StringComparison.OrdinalIgnoreCase))
            {
                try { ServiceHtml.CheckAuthentication(html); }
                catch (ServiceException ex) { throw new ServiceException(ex.Kind, $"eQSL account verification (MyAccounts.cfm): {ex.Message}", ex); }
                throw new ServiceException(FailureKind.InvalidResponse, "Cannot identify eQSL accounts in MyAccounts.cfm.");
            }
            accounts.Add(new(credentials.Login));
        }
        return accounts.Distinct().ToArray();
    }

    public static string AdifDownloadAddress(string html, ServiceHttpSession http)
    {
        var document = ServiceHtml.Parse(html);
        // Preserve the complete href, including ../, a root-relative path and query parameters.
        // A substring starting at "downloadedfiles/" may point at a different server directory.
        var links = ServiceHtml.Nodes(document.DocumentNode, "//a[@href]")
            .Select(link => link.GetAttributeValue("href", ""));
        foreach (var link in links)
        {
            var decoded = WebUtility.HtmlDecode(link);
            if (Uri.TryCreate(http.BaseAddress, decoded, out var uri) && IsAdifPath(uri.AbsolutePath))
                return http.Resolve(decoded).AbsoluteUri;
        }
        // Some legacy pages use a quoted JavaScript location instead of an anchor.
        foreach (Match match in Regex.Matches(html, "[\"'](?<url>[^\"'<>\r\n]*downloadedfiles/[^\"'<>\r\n]*)[\"']", RegexOptions.IgnoreCase))
        {
            var decoded = WebUtility.HtmlDecode(match.Groups["url"].Value);
            if (Uri.TryCreate(http.BaseAddress, decoded, out var uri) && IsAdifPath(uri.AbsolutePath))
                return http.Resolve(decoded).AbsoluteUri;
        }
        throw new ServiceException(FailureKind.InvalidResponse, "eQSL inbox export did not return a recognizable ADIF download link.");
    }

    private static bool IsAdifPath(string path) => path.Contains("/downloadedfiles/", StringComparison.OrdinalIgnoreCase) &&
        (path.EndsWith(".adi", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".adif", StringComparison.OrdinalIgnoreCase));

    public static async Task<string> GetTextAsync(ServiceHttpSession http, string resource, string stage, CancellationToken token)
    {
        try { return await http.GetTextAsync(resource, token).ConfigureAwait(false); }
        catch (ServiceException ex) { throw new ServiceException(ex.Kind, $"eQSL {stage}: {ex.Message}", ex); }
    }

    public static async Task<byte[]> GetBytesAsync(ServiceHttpSession http, string resource, string stage, CancellationToken token)
    {
        try { return await http.GetAsync(resource, token).ConfigureAwait(false); }
        catch (ServiceException ex) { throw new ServiceException(ex.Kind, $"eQSL {stage}: {ex.Message}", ex); }
    }
}

