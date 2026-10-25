using System.Collections.Concurrent;
using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using SQ7MRU.Utils.Infrastructure;

namespace SQ7MRU.Utils.Tests;

// Synthetic service fixtures exercise complete authenticated adapter -> use case -> filesystem flows.
public class ServiceIntegrationTests
{
    [Fact]
    public async Task EqslFallsBackFromSubmodeToModeAndSavesPng()
    {
        var lookupModes = new List<string>();
        var fixture = new FixtureFactory((_, request) =>
        {
            var uri = request.RequestUri!;
            if (request.Method == HttpMethod.Post) return Task.FromResult(Html("Welcome"));
            if (uri.AbsolutePath.EndsWith("DownloadInBox.cfm")) return Task.FromResult(Html("<a href='downloadedfiles/test.adi'>ADIF</a>"));
            if (uri.AbsolutePath.Contains("downloadedfiles")) return Task.FromResult(Html(
                "<EOH><CALL:5>K1ABC<QSO_DATE:8>20261020<TIME_ON:4>1234<BAND:3>20m<MODE:3>PSK<SUBMODE:5>PSK31<EOR>"));
            if (uri.AbsolutePath.EndsWith("DisplayeQSL.cfm"))
            {
                if (uri.Query.Contains("Mode=PSK31"))
                {
                    lookupModes.Add("PSK31");
                    return Task.FromResult(Html("There is no entry for a QSO"));
                }
                Assert.Contains("Mode=PSK", uri.Query);
                lookupModes.Add("PSK");
                return Task.FromResult(Html("<img src='/CFFileServlet/_cf_image/card.png'>"));
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aHZkAAAAASUVORK5CYII="))
            });
        });
        var root = Path.Combine(Path.GetTempPath(), "sq7mru-png-" + Guid.NewGuid().ToString("N"));
        await using var provider = Provider(fixture);
        try
        {
            var request = new DownloadRequest(QslService.Eqsl, new("SQ7MRU", "secret"), root, new("SQ7MRU", "Home", "1"));
            var service = provider.GetRequiredService<IDownloadService>();
            var result = await service.DownloadAsync(request);
            Assert.Equal(2, result.Downloaded);
            Assert.Equal(0, result.Failed);
            Assert.Single(Directory.GetFiles(root, "*.png", SearchOption.AllDirectories));
            Assert.Contains(result.Items, item => item.Name.EndsWith(".png"));
            Assert.Equal(new[] { "PSK31", "PSK" }, lookupModes);
            var resumed = await service.DownloadAsync(request);
            Assert.Equal(1, resumed.Skipped);
            Assert.Equal(1, resumed.Downloaded);
            Assert.Equal(2, lookupModes.Count);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task EqslLoginMatchesLegacyFieldsAndVerifiesProtectedAccountPage()
    {
        var paths = new List<string>();
        var fixture = new FixtureFactory(async (_, request) =>
        {
            paths.Add(request.RequestUri!.AbsolutePath);
            if (request.Method == HttpMethod.Post)
            {
                Assert.Equal("Callsign=SQ7MRU&EnteredPassword=secret", await request.Content!.ReadAsStringAsync());
                // A generic login widget in the response is not proof that authentication failed.
                return Html("<aside><form action='LoginFinish.cfm'><input type='password' name='EnteredPassword'></form></aside>");
            }
            Assert.EndsWith("MyAccounts.cfm", request.RequestUri.AbsolutePath);
            return Html("""
                <aside><form action='LoginFinish.cfm'><input type='password' name='EnteredPassword'></form></aside>
                You currently have no other accounts attached.
                <form action='AttachAccount.cfm'><input type='hidden' name='HamID' value='42'></form>
                """);
        });
        await using var provider = Provider(fixture);
        var accounts = await provider.GetRequiredService<IEqslAccountService>().GetAccountsAsync(new("SQ7MRU", "secret"));
        Assert.Equal("42", Assert.Single(accounts).HamId);
        Assert.Equal(new[] { "/qslcard/LoginFinish.cfm", "/qslcard/MyAccounts.cfm" }, paths);
    }

    [Fact]
    public async Task EqslLinkedAccountSwitchRequiresPrimaryAuthenticationInTheSameSession()
    {
        var authenticated = new ConcurrentDictionary<int, bool>();
        var selected = new ConcurrentDictionary<int, bool>();
        var primaryLogins = 0;
        var fixture = new FixtureFactory(async (id, request) =>
        {
            if (request.Method == HttpMethod.Post)
            {
                var body = await request.Content!.ReadAsStringAsync();
                if (!body.Contains("HamID="))
                {
                    Assert.Equal("Callsign=PRIMARY&EnteredPassword=secret", body);
                    authenticated[id] = true;
                    primaryLogins++;
                }
                else
                {
                    Assert.True(authenticated.GetValueOrDefault(id));
                    Assert.Contains("Callsign=LINKED", body);
                    Assert.Contains("SelectCallsign=Log%2BIn", body);
                    selected[id] = true;
                }
                return Html("Welcome");
            }
            Assert.True(selected.GetValueOrDefault(id));
            if (request.RequestUri!.AbsolutePath.EndsWith("DownloadInBox.cfm"))
                return Html("<a href='downloadedfiles/test.adi'>ADIF</a>");
            return Html("<EOH>");
        });
        var root = Path.Combine(Path.GetTempPath(), "sq7mru-linked-" + Guid.NewGuid().ToString("N"));
        await using var provider = Provider(fixture);
        try
        {
            var result = await provider.GetRequiredService<IDownloadService>().DownloadAsync(
                new(QslService.Eqsl, new("PRIMARY", "secret"), root, new("LINKED", "Home", "42")));
            Assert.Equal(1, result.Downloaded);
            Assert.True(primaryLogins >= 1);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Theory]
    [InlineData("<b>Select</b> an account")]
    [InlineData("Wybierz konto")]
    public async Task EqslAccountSelectorWithHiddenPasswordIsNotAnAuthenticationFailure(string prompt)
    {
        var posts = 0;
        var fixture = new FixtureFactory(async (_, request) =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path.EndsWith("Login.cfm")) return Html("""
                <form action='LoginFinish.cfm'><input type='hidden' name='ZeroType' value='Slash'>
                <input type='password' name='EnteredPassword'></form>
                """);
            if (path.EndsWith("LoginFinish.cfm"))
            {
                var body = await request.Content!.ReadAsStringAsync();
                if (++posts == 1)
                {
                    Assert.DoesNotContain("ZeroType=", body);
                    Assert.DoesNotContain("Login=", body);
                    Assert.Contains("EnteredPassword=p%26%2B%3D", body);
                    return Html(prompt + """
                        <form action='LoginFinish.cfm'>
                          <input type='hidden' name='HamID' value='42'>
                          <input type='hidden' name='Callsign' value='SQ7MRU'>
                          <input type='hidden' name='EnteredPassword' value='p&amp;+='>
                          <input type='submit' name='SelectCallsign' value='Log In'>
                        </form>
                        """);
                }
                Assert.Contains("HamID=42", body);
                Assert.Contains("SelectCallsign=Log%2BIn", body);
                return Html("<p>Welcome</p><input type='hidden' name='EnteredPassword' value='p&amp;+='>");
            }
            Assert.EndsWith("MyAccounts.cfm", path);
            return Html("You currently have no other accounts attached.");
        });
        await using var provider = Provider(fixture);
        var accounts = await provider.GetRequiredService<IEqslAccountService>().GetAccountsAsync(new("SQ7MRU", "p&+="));
        Assert.Equal("SQ7MRU", Assert.Single(accounts).CallSign);
        Assert.Equal(2, posts);
    }

    [Fact]
    public async Task EqslReturnedInteractivePasswordFormStillRejectsAuthentication()
    {
        var fixture = new FixtureFactory((_, _) => Task.FromResult(Html("""
            <form action='LoginFinish.cfm'><input name='EnteredPassword' TYPE='PASSWORD'></form>
            """)));
        await using var provider = Provider(fixture);
        var error = await Assert.ThrowsAsync<ServiceException>(() =>
            provider.GetRequiredService<IEqslAccountService>().GetAccountsAsync(new("SQ7MRU", "bad")));
        Assert.Equal(FailureKind.Authentication, error.Kind);
    }

    private sealed class FixtureFactory(Func<int, HttpRequestMessage, Task<HttpResponseMessage>> send) : IServiceHttpSessionFactory
    {
        private int next;
        public ServiceHttpSession Create(QslService service, IProgress<DownloadProgress>? progress = null)
        {
            var id = Interlocked.Increment(ref next);
            return new(new(new StubHandler((request, _) => send(id, request))), service,
                new() { MinimumRequestInterval = TimeSpan.Zero, MaxRetries = 0, EqslCardInterval = TimeSpan.Zero }, new(TimeProvider.System), TimeProvider.System, progress);
        }
    }
    private static HttpResponseMessage Html(string html) => new(HttpStatusCode.OK) { Content = new StringContent(html) };
    private static HttpResponseMessage Jpeg() => new(HttpStatusCode.OK) { Content = new ByteArrayContent(TestImages.Jpeg) };
    private static ServiceProvider Provider(IServiceHttpSessionFactory factory,
        string technicalDirectoryName = ArtifactStorageOptions.DefaultTechnicalDirectoryName) => new ServiceCollection()
        .AddSingleton(factory)
        .Configure<ArtifactStorageOptions>(options => options.TechnicalDirectoryName = technicalDirectoryName)
        .AddSq7MruUtils().BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

    [Theory]
    [InlineData("downloadedfiles/test.adi", "/qslcard/downloadedfiles/test.adi", ".sq7mru")]
    [InlineData("../downloadedfiles/test.adi?key=a&amp;format=adif", "/downloadedfiles/test.adi", ".sq7mru")]
    [InlineData("/exports/downloadedfiles/test.adi", "/exports/downloadedfiles/test.adi", ".sq7mru")]
    [InlineData("https://eqsl.cc/exports/downloadedfiles/test.adi?key=a", "/exports/downloadedfiles/test.adi", ".sq7mru")]
    [InlineData("downloadedfiles/test.adi", "/qslcard/downloadedfiles/test.adi", "technical-data")]
    public async Task EqslSeparatesAccountsAndDownloadsAdifAndImages(string link, string expectedPath, string technicalDirectoryName)
    {
        var selected = new ConcurrentDictionary<int, string>();
        var imageAccounts = new ConcurrentBag<string>();
        var fixture = new FixtureFactory(async (id, request) =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path.EndsWith("Login.cfm")) return Html("<form action='LoginFinish.cfm'><input type='hidden' name='ZeroType' value='Slash'></form>");
            if (path.EndsWith("LoginFinish.cfm"))
            {
                var form = await request.Content!.ReadAsStringAsync();
                if (form.Contains("HamID=1")) selected[id] = "Home";
                if (form.Contains("HamID=2")) selected[id] = "Field";
                return Html("<html>Welcome</html>");
            }
            if (path.EndsWith("MyAccounts.cfm")) return Html("""
                <table><tr><td>SQ7MRU<br>(Home)</td><td><form><input name="HamID" value="1"></form></td></tr>
                <tr><td>SQ7MRU<br>(Field)</td><td><form><input name="HamID" value="2"></form></td></tr></table>
                """);
            if (path.EndsWith("DownloadInBox.cfm"))
            {
                Assert.True(selected.ContainsKey(id));
                Assert.Contains(selected[id], Uri.UnescapeDataString(request.RequestUri.Query));
                return Html($"<a href='{link}'>ADIF</a>");
            }
            if (path.Contains("downloadedfiles"))
            {
                Assert.Equal(expectedPath, path);
                return Html("<EOH><CALL:5>K1ABC<QSO_DATE:8>20261020<TIME_ON:6>123456<BAND:3>20m<MODE:3>FT8<EOR>");
            }
            if (path.EndsWith("DisplayeQSL.cfm"))
            {
                Assert.Contains("12%3A34", request.RequestUri.Query, StringComparison.OrdinalIgnoreCase);
                return Html("<img src='/CFFileServlet/_cf_image/card.jpg'>");
            }
            if (path.EndsWith("card.jpg")) { imageAccounts.Add(selected[id]); return Jpeg(); }
            throw new Exception("Unexpected fixture request");
        });
        var root = Path.Combine(Path.GetTempPath(), "sq7mru-fixture-" + Guid.NewGuid().ToString("N"));
        await using var provider = Provider(fixture, technicalDirectoryName);
        try
        {
            var result = await provider.GetRequiredService<IDownloadService>().DownloadAsync(new(QslService.Eqsl, new("SQ7MRU", "secret"), root));
            Assert.Equal(4, result.Downloaded); Assert.Equal(0, result.Failed);
            Assert.Contains("Home", imageAccounts); Assert.Contains("Field", imageAccounts);
            var exports = Directory.GetFiles(root, "*.adif", SearchOption.AllDirectories);
            Assert.Equal(2, exports.Length);
            Assert.Contains(Path.Combine(root, technicalDirectoryName, "SQ7MRU_Home.adif"), exports);
            Assert.Contains(Path.Combine(root, technicalDirectoryName, "SQ7MRU_Field.adif"), exports);
            Assert.Contains(result.Items, item => item.Name == Path.Combine(technicalDirectoryName, "SQ7MRU_Home.adif"));
            Assert.Equal(2, Directory.GetFiles(root, "*.jpg", SearchOption.AllDirectories).Length);
            var store = provider.GetRequiredService<IArtifactStore>();
            var bundle = new DownloadItem(Path.Combine("EPC", "TEST", "Award.jpg"), "", AllowImageArchive: true);
            await store.SaveAsync(root, bundle, ImageArchiveTests.Zip(("award.jpg", TestImages.Jpeg)), default);
            Assert.True(File.Exists(Path.Combine(root, technicalDirectoryName, "downloads.sqlite")));
            Assert.True(File.Exists(Path.Combine(root, technicalDirectoryName, "downloads.lock")));
            Assert.True(await store.IsValidAsync(root, bundle, default));
            if (technicalDirectoryName != ArtifactStorageOptions.DefaultTechnicalDirectoryName)
                Assert.False(Directory.Exists(Path.Combine(root, ArtifactStorageOptions.DefaultTechnicalDirectoryName)));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public async Task HrdSearchDeduplicatesLinksAndSkipsExistingValidImages()
    {
        var images = 0;
        var fixture = new FixtureFactory((_, request) =>
        {
            if (request.Method == HttpMethod.Post)
            {
                Assert.Contains("callsign=SQ7MRU", request.Headers.GetValues("Cookie"));
                return Task.FromResult(Html("<a href='PrintQsl.aspx?id=123'>one</a><a href='PrintQsl.aspx?id=123'>duplicate</a>"));
            }
            images++; return Task.FromResult(Jpeg());
        });
        var root = Path.Combine(Path.GetTempPath(), "sq7mru-fixture-" + Guid.NewGuid().ToString("N"));
        await using var provider = Provider(fixture);
        try
        {
            var service = provider.GetRequiredService<IDownloadService>();
            var request = new DownloadRequest(QslService.HrdLog, new("SQ7MRU"), root);
            Assert.Equal(1, (await service.DownloadAsync(request)).Downloaded);
            Assert.Equal(1, (await service.DownloadAsync(request)).Skipped);
            Assert.Equal(1, images);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task EpcParsesHiddenLoginFieldsAndAwardForm(bool archive, bool pdf)
    {
        var awardRequests = 0;
        var fixture = new FixtureFactory(async (_, request) =>
        {
            if (request.Method == HttpMethod.Post)
            {
                var body = await request.Content!.ReadAsStringAsync();
                if (body.Contains("task=user.login"))
                {
                    Assert.Contains("csrf-token=1", body);
                    Assert.Contains("username=SQ7MRU", body);
                    Assert.Contains("password=secret", body);
                    Assert.Equal("?lang=en&Itemid=42", request.RequestUri!.Query);
                    return Html("""
                        <form id='login-form'><input type='hidden' name='task' value='user.logout'>
                        <button type='submit'>Log out</button></form>
                        <a href='index.php?option=com_content&amp;view=article&amp;id=61'><span class='item-title'>Europe</span></a>
                        """);
                }
                Assert.Contains("award=eu100", body); Assert.Contains("fileformat=jpg", body);
                var awardRequest = Interlocked.Increment(ref awardRequests);
                if (pdf) return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(TestDocuments.Pdf) };
                if (archive) return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(awardRequest == 1
                        ? ImageArchiveTests.Zip(("TEST-100.jpg", TestImages.Jpeg), ("TEST-200.jpg", TestImages.Jpeg))
                        : ImageArchiveTests.Zip(("TEST-100.jpg", TestImages.Jpeg), ("TEST-200.jpg", TestImages.Jpeg), ("TEST-300.jpg", TestImages.Jpeg)))
                };
                return Jpeg();
            }
            if (request.RequestUri!.AbsolutePath == "/") return Html("""
                <form id='login-form' action='/index.php?lang=en&amp;Itemid=42'><input type='hidden' name='csrf-token' value='1'>
                <input type='hidden' name='return' value='home'></form>
                """);
            return Html("""
                <form action='award.php'><input type='hidden' name='token' value='x'>
                <select name='award'><option value=''>select award</option><option value='eu100'>EU 100</option></select>
                <select name='fileformat'><option value='jpg'>jpg</option></select>
                <input type='submit' name='submit' value='Submit'></form>
                """);
        });
        var root = Path.Combine(Path.GetTempPath(), "sq7mru-fixture-" + Guid.NewGuid().ToString("N"));
        await using var provider = Provider(fixture);
        try
        {
            var result = await provider.GetRequiredService<IDownloadService>().DownloadAsync(new(QslService.Epc, new(" sq7mru ", "secret"), root));
            Assert.Equal(1, result.Downloaded); Assert.Equal(0, result.Failed);
            Assert.Equal(pdf ? 0 : archive ? 2 : 1, Directory.GetFiles(root, "*.jpg", SearchOption.AllDirectories).Length);
            if (pdf) Assert.Single(Directory.GetFiles(root, "*.pdf", SearchOption.AllDirectories));
            var repeated = await provider.GetRequiredService<IDownloadService>().DownloadAsync(new(QslService.Epc, new(" sq7mru ", "secret"), root));
            Assert.Equal(1, repeated.Skipped);
            Assert.Equal(1, awardRequests);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Theory]
    [InlineData(404)]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(200)]
    public async Task EpcContinuesPastUnavailableAndInformationalCategories(int status)
    {
        var fixture = new FixtureFactory(async (_, request) =>
        {
            if (request.Method == HttpMethod.Post)
            {
                var body = await request.Content!.ReadAsStringAsync();
                if (!body.Contains("task=user.login")) return Jpeg();
                return Html("""
                    <nav><a href='index.php?option=com_content&amp;view=article&amp;id=1'><span class='item-title'>Unavailable</span></a>
                    <a href='index.php?option=com_content&amp;view=article&amp;id=2'><span class='item-title'>Information</span></a>
                    <a href='index.php?option=com_content&amp;view=article&amp;id=9999'><span class='item-title'>New awards</span></a></nav>
                    """);
            }
            if (request.RequestUri!.AbsolutePath == "/") return Html("<form id='login-form'></form>");
            if (request.RequestUri.Query.EndsWith("id=1")) return status == 200
                ? Html("<form action='index.php?task=user.login'><input type='password' name='password'></form>")
                : new((HttpStatusCode)status);
            if (request.RequestUri.Query.EndsWith("id=2")) return Html("<article>Information only</article>");
            return Html("<form id='login-form'><input type='password' name='password'></form><form action='award-new.php'><input type='hidden' name='award' value='first'><button name='download'>Download</button></form>");
        });
        var root = Path.Combine(Path.GetTempPath(), "sq7mru-epc-menu-" + Guid.NewGuid().ToString("N"));
        await using var provider = Provider(fixture);
        try
        {
            var result = await provider.GetRequiredService<IDownloadService>().DownloadAsync(new(QslService.Epc, new("TEST", "secret"), root));
            Assert.Equal(1, result.Downloaded);
            Assert.Equal(status == 200 ? 0 : 1, result.Failed);
            if (status != 200)
                Assert.Contains(result.Items, entry => entry.Failure == (status == 404 ? FailureKind.NotFound : FailureKind.InvalidResponse) && entry.Name.Contains("Unavailable"));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Theory]
    [InlineData("<form id='login-form'><input type='PASSWORD' name='password'></form>")]
    [InlineData("Username and password do not match")]
    public async Task EpcRejectsFailedLogin(string response)
    {
        var fixture = new FixtureFactory((_, request) => Task.FromResult(Html(
            request.Method == HttpMethod.Post ? response :
                "<form id='login-form'><input type='hidden' name='token' value='1'></form>")));
        await using var provider = Provider(fixture);
        var service = provider.GetRequiredService<IDownloadService>();
        var error = await Assert.ThrowsAsync<ServiceException>(() => service.DownloadAsync(
            new(QslService.Epc, new("SQ7MRU", "secret"), Path.GetTempPath())));
        Assert.Equal(FailureKind.Authentication, error.Kind);
    }

    [Theory]
    [InlineData("Error: Upload was unsuccessful.", false)]
    [InlineData("0 records added", false)]
    [InlineData("success", false)]
    [InlineData("RESULT=OKAY", false)]
    [InlineData("RESULT=OK; Error: rejected", false)]
    [InlineData("<script>const response = 'RESULT=OK';</script>Unknown outcome", false)]
    [InlineData("RESULT=OK", true)]
    [InlineData("1 record added", true)]
    [InlineData("12 records added", true)]
    public async Task UploadRequiresAnUnambiguousPositiveResult(string response, bool success)
    {
        var fixture = new FixtureFactory((_, request) => Task.FromResult(Html(
            request.RequestUri!.AbsolutePath.EndsWith("LoginFinish.cfm") ? "Welcome" : response)));
        await using var provider = Provider(fixture);
        var operation = provider.GetRequiredService<IAdifUploader>().UploadAsync("<CALL:6>SQ7MRU<EOR>", new("test", "secret"));
        if (success) await operation;
        else Assert.Equal(FailureKind.InvalidResponse, (await Assert.ThrowsAsync<ServiceException>(() => operation)).Kind);
    }

    [Fact]
    public async Task UploadUsesMultipartAndRequiresConfirmedOutcome()
    {
        var uploads = 0;
        var fixture = new FixtureFactory(async (_, request) =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("Login.cfm")) return Html("<form action='LoginFinish.cfm'></form>");
            if (request.RequestUri!.AbsolutePath.EndsWith("LoginFinish.cfm")) return Html("Welcome");
            Assert.StartsWith("multipart/form-data", request.Content!.Headers.ContentType!.ToString());
            Assert.Contains("UPLOAD.ADIF", await request.Content.ReadAsStringAsync());
            uploads++; return Html("RESULT=OK");
        });
        await using var provider = Provider(fixture);
        await provider.GetRequiredService<IAdifUploader>().UploadAsync("<CALL:6>SQ7MRU<EOR>", new("SQ7MRU", "secret"));
        Assert.Equal(1, uploads);
    }

    [Fact]
    public async Task AcceptsSuccessfulThirdAccountSelectionResponse()
    {
        var selections = 0;
        var fixture = new FixtureFactory((_, request) => Task.FromResult(Html(
            request.RequestUri!.AbsolutePath.EndsWith("MyAccounts.cfm") ? "You currently have no other accounts attached."
            : selections++ < 3 ? "<form action='LoginFinish.cfm'><input type='hidden' name='HamID' value='1'><input name='SelectCallsign' value='Log In'></form>" : "Welcome")));
        await using var provider = Provider(fixture);
        Assert.Single(await provider.GetRequiredService<IEqslAccountService>().GetAccountsAsync(new("test", "secret")));
        Assert.Equal(4, selections);
    }

    [Theory]
    [InlineData("<form action='LoginFinish.cfm'><input type='password' name='EnteredPassword'><input type='hidden' name='HamID' value='123'></form>", FailureKind.Authentication)]
    [InlineData("<form action='SomethingElse.cfm'><input type='hidden' name='HamID' value='123'></form>", FailureKind.InvalidResponse)]
    [InlineData("Incorrect password<form action='AttachAccount.cfm'><input name='HamID' value='1'></form>", FailureKind.Authentication)]
    public async Task HiddenHamIdAloneDoesNotProveAnAuthenticatedAccountList(string html, FailureKind failure)
    {
        var fixture = new FixtureFactory((_, request) => Task.FromResult(Html(request.RequestUri!.AbsolutePath.EndsWith("LoginFinish.cfm") ? "Welcome" : html)));
        await using var provider = Provider(fixture);
        var error = await Assert.ThrowsAsync<ServiceException>(() => provider.GetRequiredService<IEqslAccountService>().GetAccountsAsync(new("test", "secret")));
        Assert.Equal(failure, error.Kind);
    }

    [Fact]
    public async Task LoginFailureIsFatalAndDoesNotCreateFiles()
    {
        var fixture = new FixtureFactory((_, _) => Task.FromResult(Html("<html>Incorrect password</html>")));
        await using var provider = Provider(fixture);
        var error = await Assert.ThrowsAsync<ServiceException>(() => provider.GetRequiredService<IEqslAccountService>().GetAccountsAsync(new("SQ7MRU", "bad")));
        Assert.Equal(FailureKind.Authentication, error.Kind);
    }
}
