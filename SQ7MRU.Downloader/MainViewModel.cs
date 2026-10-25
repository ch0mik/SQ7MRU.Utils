using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SQ7MRU.Utils;
namespace SQ7MRU.Downloader;
public partial class MainViewModel(IDownloadService downloader, IEqslAccountService accounts) : ObservableObject
{
    public IReadOnlyList<QslService> Services { get; } = Enum.GetValues<QslService>();
    public ObservableCollection<ServiceAccount> Accounts { get; } = [];
    public ObservableCollection<ItemResult> Results { get; } = [];
    [ObservableProperty] private QslService selectedService;
    [ObservableProperty] private string login = "";
    [ObservableProperty] private string password = "";
    [ObservableProperty] private string outputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SQ7MRU");
    [ObservableProperty] private ServiceAccount? selectedAccount;
    [ObservableProperty] private string status = "Select a service and an output directory.";
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private double progressValue;
    [ObservableProperty] private double progressMaximum = 1;
    private CancellationTokenSource? cancellation;
    private int operationVersion;
    private bool CanRun => !IsBusy;
    private bool CanCancel => IsBusy;
    partial void OnIsBusyChanged(bool value)
    {
        DownloadCommand.NotifyCanExecuteChanged(); LoadAccountsCommand.NotifyCanExecuteChanged(); CancelCommand.NotifyCanExecuteChanged();
    }
    partial void OnLoginChanged(string value) => ClearAccounts();
    partial void OnPasswordChanged(string value) => ClearAccounts();
    partial void OnSelectedServiceChanged(QslService value) => ClearAccounts();
    private void ClearAccounts() { Accounts.Clear(); SelectedAccount = null; }
    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task LoadAccountsAsync()
    {
        if (SelectedService != QslService.Eqsl) { Status = "Account/QTH selection is available for eQSL only."; return; }
        await RunAsync(async token =>
        {
            ClearAccounts();
            foreach (var account in await accounts.GetAccountsAsync(new(Login, Password), token)) Accounts.Add(account);
            SelectedAccount = Accounts.FirstOrDefault();
            Status = $"Accounts found: {Accounts.Count}. Select an account/QTH.";
        });
    }
    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task DownloadAsync()
    {
        await RunAsync(async token =>
        {
            if (SelectedService == QslService.Eqsl && Accounts.Count == 0)
            {
                foreach (var account in await accounts.GetAccountsAsync(new(Login, Password), token)) Accounts.Add(account);
                SelectedAccount = Accounts.FirstOrDefault();
                Status = Accounts.Count == 0 ? "The service returned no accounts."
                    : "Select an account/QTH, then click Start download.";
                return;
            }
            if (SelectedService == QslService.Eqsl && SelectedAccount is null)
            {
                Status = "Select an account/QTH before starting the download.";
                return;
            }
            Results.Clear();
            var version = operationVersion;
            var downloaded = 0;
            var skipped = 0;
            var failed = 0;
            var progress = new Progress<DownloadProgress>(p =>
            {
                if (!IsBusy || version != operationVersion) return;
                if (p.Total > 0) ProgressMaximum = p.Total;
                ProgressValue = Math.Max(ProgressValue, p.Completed);
                if (p.Item is not null) Results.Insert(0, p.Item);
                downloaded = Math.Max(downloaded, p.Downloaded);
                skipped = Math.Max(skipped, p.Skipped);
                failed = Math.Max(failed, p.Failed);
                var counts = $"Downloaded: {downloaded}, skipped: {skipped}, failed: {failed}.";
                Status = p.RetryAfter is { } delay ? $"{counts} Waiting for service — {delay.TotalSeconds:0.###} s."
                    : p.Item?.Status == ItemStatus.Failed
                        ? $"{counts} {p.Item.Name}: {p.Item.Failure} — {p.Item.Message}"
                        : $"{counts} Processed: {p.Completed}/{p.Total} — {p.Message}";
            });
            var result = await downloader.DownloadAsync(new(SelectedService, new(Login, Password), OutputDirectory, SelectedAccount), progress, token);
            // Keep the live log order; the final result is sorted by filename, not completion time.
            foreach (var item in result.Items)
                if (!Results.Contains(item)) Results.Insert(0, item);
            Status = $"Downloaded: {result.Downloaded}, skipped: {result.Skipped}, failed: {result.Failed}.";
        });
    }
    private async Task RunAsync(Func<CancellationToken, Task> action)
    {
        if (string.IsNullOrWhiteSpace(Login) || (SelectedService != QslService.HrdLog && string.IsNullOrWhiteSpace(Password)))
        { Status = "Enter your login and the required password."; return; }
        operationVersion++;
        ProgressValue = 0; ProgressMaximum = 1;
        IsBusy = true; Status = "Connecting to the service…";
        using var source = new CancellationTokenSource(); cancellation = source;
        try { await action(source.Token); }
        catch (OperationCanceledException) { Status = "Operation cancelled. Successfully saved files remain in the output directory."; }
        catch (ServiceException ex) { Status = $"{ex.Kind}: {ex.Message}"; }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException or FormatException)
        { Status = "Unable to complete the operation. Check the output directory, credentials and service response format."; }
        finally { cancellation = null; IsBusy = false; }
    }
    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel() => cancellation?.Cancel();
}
