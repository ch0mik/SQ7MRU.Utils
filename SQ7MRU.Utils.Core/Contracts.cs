namespace SQ7MRU.Utils;

public enum QslService { Eqsl, HrdLog, Epc }
public enum FailureKind { Authentication, RateLimited, Network, Timeout, InvalidResponse, Storage, NotFound }
public enum ItemStatus { Downloaded, Skipped, Failed }
public enum ProgressStage { Discovering, Downloading, WaitingForService, Completed }

// Avoid the credential-revealing ToString generated for records.
public sealed class ServiceCredentials(string login, string? password = null)
{
    public string Login { get; } = login;
    public string? Password { get; } = password;
    public override string ToString() => "[credentials]";
}
public sealed record ServiceAccount(string CallSign, string Qth = "", string? HamId = null);
public sealed record DownloadRequest(QslService Service, ServiceCredentials Credentials,
    string OutputDirectory, ServiceAccount? Account = null);
public sealed record DownloadProgress(ProgressStage Stage, string Message, int Completed = 0,
    int Total = 0, TimeSpan? RetryAfter = null, ItemResult? Item = null,
    int Downloaded = 0, int Skipped = 0, int Failed = 0);
public sealed record ItemResult(string Name, ItemStatus Status, FailureKind? Failure = null, string? Message = null);
public sealed record DownloadResult(IReadOnlyList<ItemResult> Items)
{
    public int Downloaded => Items.Count(x => x.Status == ItemStatus.Downloaded);
    public int Skipped => Items.Count(x => x.Status == ItemStatus.Skipped);
    public int Failed => Items.Count(x => x.Status == ItemStatus.Failed);
}
public sealed class ServiceException(FailureKind kind, string message, Exception? innerException = null) : Exception(message, innerException)
{
    public FailureKind Kind { get; } = kind;
}
public interface IDownloadService
{
    Task<DownloadResult> DownloadAsync(DownloadRequest request,
        IProgress<DownloadProgress>? progress = null, CancellationToken cancellationToken = default);
}
public interface IEqslAccountService
{
    Task<IReadOnlyList<ServiceAccount>> GetAccountsAsync(ServiceCredentials credentials,
        CancellationToken cancellationToken = default);
}
public interface IAdifUploader
{
    Task UploadAsync(string adif, ServiceCredentials credentials, ServiceAccount? account = null,
        CancellationToken cancellationToken = default);
}
public interface IDownloadSession : IAsyncDisposable
{
    IAsyncEnumerable<DownloadItem> DiscoverAsync(ServiceAccount? account, CancellationToken cancellationToken);
    Task<byte[]> FetchAsync(DownloadItem item, CancellationToken cancellationToken);
}
public sealed record DownloadItem(string RelativePath, string Resource, bool IsAdif = false, bool AllowImageArchive = false, bool RefreshExisting = false, bool AllowPdf = false);
public interface IDownloadSessionFactory
{
    QslService Service { get; }
    Task<IDownloadSession> OpenAsync(ServiceCredentials credentials, IProgress<DownloadProgress>? progress,
        CancellationToken cancellationToken);
}
public interface IArtifactStore
{
    Task<bool> IsValidAsync(string directory, DownloadItem item, CancellationToken cancellationToken);
    Task<string> SaveAsync(string directory, DownloadItem item, byte[] content, CancellationToken cancellationToken);
}
