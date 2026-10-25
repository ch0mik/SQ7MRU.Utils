namespace SQ7MRU.Utils;

/// <summary>Identifies a supported certificate or QSL download service.</summary>
public enum QslService
{
    /// <summary>eQSL.cc, including linked callsign/QTH accounts and ADIF export.</summary>
    Eqsl,
    /// <summary>HRDLog/iQSL public callsign search; a password is not required.</summary>
    HrdLog,
    /// <summary>EPC MemberCenter certificate downloads, including PDF and ZIP responses.</summary>
    Epc
}

/// <summary>Classifies a service or artifact-storage failure without requiring message parsing.</summary>
public enum FailureKind
{
    /// <summary>Login, authenticated access or certificate retrieval was rejected.</summary>
    Authentication,
    /// <summary>A recognized service rate limit remained unresolved within the retry policy.</summary>
    RateLimited,
    /// <summary>A transport failure or transient HTTP error exhausted its retry policy.</summary>
    Network,
    /// <summary>A request timeout or logical HTTP request time budget was exhausted.</summary>
    Timeout,
    /// <summary>The service returned unsupported, ambiguous or invalid content.</summary>
    InvalidResponse,
    /// <summary>Local storage access, confinement or metadata persistence failed.</summary>
    Storage,
    /// <summary>The requested remote resource or confirmed QSO was not found.</summary>
    NotFound
}

/// <summary>Describes the outcome of one logical download selection, not necessarily one physical file.</summary>
public enum ItemStatus
{
    /// <summary>The selection was fetched and its validated content was saved.</summary>
    Downloaded,
    /// <summary>Existing local content passed validation and its payload was not fetched again.</summary>
    Skipped,
    /// <summary>The selection failed; inspect its failure kind and diagnostic message.</summary>
    Failed
}

/// <summary>Identifies the current phase of a download progress notification.</summary>
public enum ProgressStage
{
    /// <summary>Available download selections are being discovered.</summary>
    Discovering,
    /// <summary>A discovered selection has completed with an item outcome.</summary>
    Downloading,
    /// <summary>Request pacing or a service cooldown is delaying further requests.</summary>
    WaitingForService,
    /// <summary>Discovery and every scheduled worker completed without fatal cancellation or authentication failure.</summary>
    Completed
}

/// <summary>Holds in-memory credentials for one service operation; its string representation is redacted.</summary>
/// <param name="login">Service username or callsign. Download operations require a nonblank value.</param>
/// <param name="password">Password required by eQSL and EPC; optional for HRDLog.</param>
/// <remarks>Properties contain sensitive values. Do not log or persist them. This constructor does not authenticate.</remarks>
public sealed class ServiceCredentials(string login, string? password = null)
{
    /// <summary>Gets the username or callsign supplied by the caller.</summary>
    public string Login { get; } = login;
    /// <summary>Gets the in-memory password, or <see langword="null"/> when none was supplied.</summary>
    public string? Password { get; } = password;
    /// <summary>Returns a fixed redacted credential label without the username or password.</summary>
    /// <returns>A credential placeholder safe to display instead of the credential properties.</returns>
    public override string ToString() => "[credentials]";
}

/// <summary>Identifies an eQSL callsign/QTH account, including a linked account when available.</summary>
/// <param name="CallSign">Account callsign as returned by the service.</param>
/// <param name="Qth">QTH nickname; empty when the service does not provide one.</param>
/// <param name="HamId">Optional opaque eQSL account identifier used during account switching.</param>
public sealed record ServiceAccount(string CallSign, string Qth = "", string? HamId = null);

/// <summary>Specifies one download job and its local output root.</summary>
/// <param name="Service">Adapter to use for discovery and fetching.</param>
/// <param name="Credentials">In-memory service credentials; never include their properties in diagnostics.</param>
/// <param name="OutputDirectory">Output root. Service/account/group paths are created beneath it.</param>
/// <param name="Account">Selected eQSL account; null downloads all available eQSL accounts. Other services ignore it.</param>
public sealed record DownloadRequest(QslService Service, ServiceCredentials Credentials,
    string OutputDirectory, ServiceAccount? Account = null);

/// <summary>Reports discovery, service waits and logical-item outcomes during a download.</summary>
/// <param name="Stage">Current operation phase.</param>
/// <param name="Message">Human-readable status; do not parse it as a machine-readable identifier.</param>
/// <param name="Completed">Processed selection count, including downloaded, skipped and failed selections.</param>
/// <param name="Total">Selections discovered so far; this can increase while workers are already downloading.</param>
/// <param name="RetryAfter">Optional wait duration for pacing or retry cooldown, not an estimated job completion time.</param>
/// <param name="Item">Optional result for the selection that just completed.</param>
/// <param name="Downloaded">Downloaded selection count, not extracted certificate-file count.</param>
/// <param name="Skipped">Selection count satisfied by validated local content.</param>
/// <param name="Failed">Selection and discovery-failure count accumulated so far.</param>
/// <remarks>Callbacks can originate from concurrent workers. Use an appropriate synchronization context for UI updates.</remarks>
public sealed record DownloadProgress(ProgressStage Stage, string Message, int Completed = 0,
    int Total = 0, TimeSpan? RetryAfter = null, ItemResult? Item = null,
    int Downloaded = 0, int Skipped = 0, int Failed = 0);

/// <summary>Describes the outcome of one logical selection or a discovery failure.</summary>
/// <param name="Name">Relative saved path, bundle directory, original logical skip name, or a discovery marker.</param>
/// <param name="Status">Downloaded, skipped or failed outcome.</param>
/// <param name="Failure">Structured failure classification when unsuccessful.</param>
/// <param name="Message">Optional diagnostic; may include local filenames or service context but not credentials.</param>
public sealed record ItemResult(string Name, ItemStatus Status, FailureKind? Failure = null, string? Message = null);

/// <summary>Aggregates completed logical selections; failed items indicate a partial download.</summary>
/// <param name="Items">Completed outcomes, ordered by name by the built-in download service.</param>
/// <remarks>One ZIP selection may produce multiple certificate files. Cancellation and fatal authentication errors throw instead of returning this result.</remarks>
public sealed record DownloadResult(IReadOnlyList<ItemResult> Items)
{
    /// <summary>Gets the number of successfully downloaded selections.</summary>
    public int Downloaded => Items.Count(x => x.Status == ItemStatus.Downloaded);
    /// <summary>Gets the number of selections reused from validated local storage.</summary>
    public int Skipped => Items.Count(x => x.Status == ItemStatus.Skipped);
    /// <summary>Gets the number of failed selections or discovery outcomes.</summary>
    public int Failed => Items.Count(x => x.Status == ItemStatus.Failed);
}

/// <summary>Represents a classified service, response-validation or storage failure.</summary>
/// <param name="kind">Machine-readable failure category.</param>
/// <param name="message">Diagnostic safe for the caller; do not embed credentials or credential-bearing URLs.</param>
/// <param name="innerException">Optional underlying error for diagnostics.</param>
public sealed class ServiceException(FailureKind kind, string message, Exception? innerException = null) : Exception(message, innerException)
{
    /// <summary>Gets the failure category; prefer this value to parsing the message.</summary>
    public FailureKind Kind { get; } = kind;
}

/// <summary>Coordinates service discovery, bounded downloads and validated local persistence.</summary>
public interface IDownloadService
{
    /// <summary>Downloads discovered selections, reusing valid existing certificates and refreshing ADIF exports.</summary>
    /// <param name="request">Service, credentials, output root and optional eQSL account selection.</param>
    /// <param name="progress">Optional thread-safe progress receiver; callback exceptions can interrupt the operation.</param>
    /// <param name="cancellationToken">Cancels discovery, waits, requests and writes. Committed files remain available.</param>
    /// <returns>All completed outcomes sorted by name. Check <see cref="DownloadResult.Failed"/> for partial failures.</returns>
    /// <exception cref="ArgumentNullException">The request or its credentials are null.</exception>
    /// <exception cref="ArgumentException">The callsign or output directory is blank.</exception>
    /// <exception cref="ArgumentOutOfRangeException">No adapter supports the requested service.</exception>
    /// <exception cref="ServiceException">Authentication fails, or the session cannot be opened.</exception>
    /// <exception cref="OperationCanceledException">Caller cancellation is requested; no final result is returned.</exception>
    /// <remarks>Non-authentication item errors become failed results. A discovery error ends that iterator after earlier work finishes. An EPC adapter can isolate a category error and continue other categories. Dispose the owning service provider after operations complete.</remarks>
    Task<DownloadResult> DownloadAsync(DownloadRequest request,
        IProgress<DownloadProgress>? progress = null, CancellationToken cancellationToken = default);
}

/// <summary>Lists authenticated eQSL accounts before selecting a callsign/QTH for download.</summary>
public interface IEqslAccountService
{
    /// <summary>Authenticates eQSL credentials and loads available primary and linked accounts.</summary>
    /// <param name="credentials">Primary eQSL username/callsign and password.</param>
    /// <param name="cancellationToken">Cancels login, account-list requests, service waits and retries.</param>
    /// <returns>Accounts that can be supplied through <see cref="DownloadRequest.Account"/>; their order follows discovery.</returns>
    /// <exception cref="ServiceException">Authentication, transport or account-page validation fails.</exception>
    /// <exception cref="OperationCanceledException">Caller cancellation is requested.</exception>
    /// <remarks>The built-in implementation disposes its cookie-isolated HTTP session before returning; it does not persist login cookies.</remarks>
    Task<IReadOnlyList<ServiceAccount>> GetAccountsAsync(ServiceCredentials credentials,
        CancellationToken cancellationToken = default);
}

/// <summary>Uploads ADIF records to eQSL with explicit confirmation and no automatic upload replay.</summary>
public interface IAdifUploader
{
    /// <summary>Validates ADIF, logs into eQSL, optionally switches accounts and submits a single upload.</summary>
    /// <param name="adif">Complete ADIF text containing at least one terminated QSO record with CALL.</param>
    /// <param name="credentials">Primary eQSL username/callsign and password.</param>
    /// <param name="account">Optional linked callsign/QTH to select before upload; null uses the primary login.</param>
    /// <param name="cancellationToken">Cancels parsing, authentication and upload processing.</param>
    /// <returns>A task completing only after an explicit positive upload response is recognized.</returns>
    /// <exception cref="ArgumentException">ADIF is blank or contains no accepted QSO records.</exception>
    /// <exception cref="FormatException">ADIF tags, field lengths or record termination are invalid.</exception>
    /// <exception cref="ServiceException">Authentication or transport fails, or upload success cannot be confirmed.</exception>
    /// <exception cref="OperationCanceledException">Caller cancellation is requested.</exception>
    /// <remarks>An interrupted or unconfirmed upload may already have reached eQSL. Check the remote account before resubmitting; the uploader deliberately does not replay the POST automatically.</remarks>
    Task UploadAsync(string adif, ServiceCredentials credentials, ServiceAccount? account = null,
        CancellationToken cancellationToken = default);
}

/// <summary>Represents an operation-scoped service adapter session owned and disposed by the caller.</summary>
public interface IDownloadSession : IAsyncDisposable
{
    /// <summary>Authenticates when needed and streams available logical download selections.</summary>
    /// <param name="account">Optional eQSL account filter; service-specific adapters may ignore it.</param>
    /// <param name="cancellationToken">Cancels discovery and its service requests.</param>
    /// <returns>A lazy asynchronous sequence; enumeration performs network I/O and may fail after yielding items.</returns>
    /// <exception cref="ServiceException">Authentication, transport or page interpretation fails.</exception>
    /// <exception cref="OperationCanceledException">Caller cancellation is requested.</exception>
    /// <remarks>Fetch can overlap enumeration. Implementations must support fetch concurrency chosen by the download options; dispose only after enumeration and fetch workers finish.</remarks>
    IAsyncEnumerable<DownloadItem> DiscoverAsync(ServiceAccount? account, CancellationToken cancellationToken);
    /// <summary>Fetches the bytes for an item previously returned by this session.</summary>
    /// <param name="item">Selection with a session-owned resource key; do not use keys from another session.</param>
    /// <param name="cancellationToken">Cancels request waits, retries and response reads.</param>
    /// <returns>Raw payload for the artifact store to validate; it may be an image, PDF, ZIP or ADIF.</returns>
    /// <exception cref="ServiceException">Service access or retrieval fails.</exception>
    /// <exception cref="OperationCanceledException">Caller cancellation is requested.</exception>
    Task<byte[]> FetchAsync(DownloadItem item, CancellationToken cancellationToken);
}

/// <summary>Describes a logical payload and the validation/persistence features its adapter permits.</summary>
/// <param name="RelativePath">Output-root-relative logical filename; actual extensions may change after content detection.</param>
/// <param name="Resource">Opaque session resource key or service-relative address. Do not log it; some legacy requests contain credentials.</param>
/// <param name="IsAdif">Treat content as ADIF and always refresh it rather than skipping an existing export.</param>
/// <param name="AllowImageArchive">Permit bounded certificate ZIP extraction and SQLite bundle indexing.</param>
/// <param name="RefreshExisting">Fetch again even if existing local content is valid.</param>
/// <param name="AllowPdf">Permit structurally validated PDF certificates, including opted-in ZIP entries.</param>
public sealed record DownloadItem(string RelativePath, string Resource, bool IsAdif = false, bool AllowImageArchive = false, bool RefreshExisting = false, bool AllowPdf = false);

/// <summary>Creates owned operation-scoped download sessions for one supported service.</summary>
public interface IDownloadSessionFactory
{
    /// <summary>Gets the service handled by this factory; register one factory per service.</summary>
    QslService Service { get; }
    /// <summary>Creates a session; authentication may be deferred until discovery.</summary>
    /// <param name="credentials">In-memory credentials retained only for the session lifetime.</param>
    /// <param name="progress">Optional receiver for transport pacing and retry notifications.</param>
    /// <param name="cancellationToken">Cancels opening; pass the caller token to subsequent session operations too.</param>
    /// <returns>A session the caller must asynchronously dispose after all workers complete.</returns>
    /// <exception cref="OperationCanceledException">Caller cancellation is requested.</exception>
    Task<IDownloadSession> OpenAsync(ServiceCredentials credentials, IProgress<DownloadProgress>? progress,
        CancellationToken cancellationToken);
}

/// <summary>Validates and persists artifacts beneath a selected output root without escaping that root.</summary>
public interface IArtifactStore
{
    /// <summary>Checks an existing certificate or an entire indexed ZIP selection for valid local content.</summary>
    /// <param name="directory">Selected output root.</param>
    /// <param name="item">Logical selection, including permitted image/archive/PDF formats.</param>
    /// <param name="cancellationToken">Cancels file reads, metadata waits and validation traversal.</param>
    /// <returns>True only when suitable content exists and passes validation; false for missing or invalid content.</returns>
    /// <exception cref="ServiceException">Path confinement or metadata validation fails.</exception>
    /// <exception cref="System.IO.IOException">A filesystem operation fails.</exception>
    /// <exception cref="OperationCanceledException">Caller cancellation is requested.</exception>
    /// <remarks>Alternate detected image/PDF extensions are considered. Indexed bundles require every listed member. Legacy JSON migration can write SQLite metadata even during this check.</remarks>
    Task<bool> IsValidAsync(string directory, DownloadItem item, CancellationToken cancellationToken);
    /// <summary>Validates content and atomically saves a file, or extracts a permitted certificate bundle.</summary>
    /// <param name="directory">Selected output root; required subdirectories are created beneath it.</param>
    /// <param name="item">Logical filename and allowed content formats.</param>
    /// <param name="content">Complete encoded payload; the store does not fetch it from the service.</param>
    /// <param name="cancellationToken">Cancels writes and prevents commit when cancellation is observed before replacement.</param>
    /// <returns>The relative saved filename with its detected extension, or the group directory for an extracted ZIP.</returns>
    /// <exception cref="ServiceException">The payload is invalid, a path is unsafe or bundle metadata cannot be used.</exception>
    /// <exception cref="System.IO.IOException">A filesystem write, replacement or index-lock operation fails.</exception>
    /// <exception cref="UnauthorizedAccessException">The output is not writable.</exception>
    /// <exception cref="OperationCanceledException">Caller cancellation is requested.</exception>
    /// <remarks>ZIP contents are validated before any certificate is written. Each file is atomic, but the whole bundle is not a filesystem transaction; committed members can remain after cancellation. Its SQLite entry is committed last.</remarks>
    Task<string> SaveAsync(string directory, DownloadItem item, byte[] content, CancellationToken cancellationToken);
}
