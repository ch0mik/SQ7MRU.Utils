using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using SQ7MRU.Downloader;

[assembly: AvaloniaTestApplication(typeof(SQ7MRU.Utils.Tests.TestAppBuilder))]
namespace SQ7MRU.Utils.Tests;
public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
public class DesktopTests
{
    private sealed class FakeServices : IDownloadService, IEqslAccountService
    {
        public int Downloads;
        public Task<DownloadResult> DownloadAsync(DownloadRequest request, IProgress<DownloadProgress>? progress = null, CancellationToken cancellationToken = default)
        {
            Downloads++;
            return Task.FromResult(new DownloadResult([new("test.jpg", ItemStatus.Downloaded)]));
        }
        public Task<IReadOnlyList<ServiceAccount>> GetAccountsAsync(ServiceCredentials credentials, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ServiceAccount>>([new("SQ7MRU", "Home", "1")]);
    }
    [AvaloniaFact]
    public async Task EqslFirstLoadsAccountsAndWaitsForUserBeforeDownloading()
    {
        var fake = new FakeServices();
        var vm = new MainViewModel(fake, fake) { Login = "SQ7MRU", Password = "secret" };
        await vm.DownloadCommand.ExecuteAsync(null);
        Assert.Single(vm.Accounts);
        Assert.Equal(0, fake.Downloads);
        Assert.Contains("Select an account/QTH", vm.Status);
        await vm.DownloadCommand.ExecuteAsync(null);
        Assert.Equal(1, fake.Downloads);
    }
    [AvaloniaFact]
    public async Task WindowRendersAndCommandsLoadAccountsAndDownload()
    {
        var fake = new FakeServices(); var vm = new MainViewModel(fake, fake) { Login = "SQ7MRU", Password = "secret" };
        var window = new MainWindow { DataContext = vm }; window.Show();
        try
        {
            await vm.LoadAccountsCommand.ExecuteAsync(null);
            Assert.Equal("Home", vm.SelectedAccount?.Qth);
            await vm.DownloadCommand.ExecuteAsync(null);
            Assert.Single(vm.Results); Assert.False(vm.IsBusy);
            Assert.Contains("Downloaded: 1", vm.Status);
        }
        finally { window.Close(); }
    }
}
