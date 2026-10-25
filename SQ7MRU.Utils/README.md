# SQ7MRU.Utils

A .NET 10 library for eQSL, HRDLog/iQSL, EPC certificates and ADIF processing. One AnyCPU NuGet package includes the infrastructure, Core and Application assemblies. It has no Avalonia dependency; the desktop downloader is a separate application.

## Install and register

```sh
dotnet add package SQ7MRU.Utils
```

```csharp
using Microsoft.Extensions.DependencyInjection;
using SQ7MRU.Utils;

var services = new ServiceCollection();
services.AddSq7MruUtils(
    downloads => downloads.MaxConcurrency = 2,
    http => http.MaxRetries = 4);

await using var provider = services.BuildServiceProvider();
using var cancellation = new CancellationTokenSource();

var credentials = new ServiceCredentials("YOUR_CALLSIGN", "YOUR_PASSWORD");
var downloader = provider.GetRequiredService<IDownloadService>();
var progress = new Progress<DownloadProgress>(p => Console.WriteLine(p.Message));
var result = await downloader.DownloadAsync(
    new DownloadRequest(QslService.Epc, credentials, "./downloads"),
    progress,
    cancellation.Token);

Console.WriteLine($"Downloaded: {result.Downloaded}; skipped: {result.Skipped}; failed: {result.Failed}");
```

Keep one provider alive for the application lifetime to share cooldowns and learned eQSL pacing. Finish or cancel and await active operations before disposing it. Credentials are supplied by the caller and retained in memory; never log their properties or persist real credentials in source.

## Public operations

| API | Behavior |
| --- | --- |
| `IDownloadService.DownloadAsync` | Streams discovery through bounded workers, validates local content, fetches missing selections and returns sorted per-item results. |
| `IEqslAccountService.GetAccountsAsync` | Authenticates the primary eQSL login and returns available callsign/QTH accounts. |
| `IAdifUploader.UploadAsync` | Validates ADIF, authenticates, optionally selects an eQSL account and uploads once; explicit success confirmation is required. |
| `AdifReader.GetAdifRows` | Synchronously parses length-delimited ADIF fields with cancellation checks; terminated records require CALL to be returned. |
| `AdifHelper.ExportAsADIF` | Validates and exports rows under the selected standard/provider profile; does not upload. |
| `AdifHelper.ConvertToString` | Serializes one row without a file header. |
| `AdifHelper.FixRecord` | Mutates only callsign/band casing and returns the same record instance. |
| `AdifHelper.FreqToBand` | Maps MHz to reviewed ADIF band intervals including endpoints; unmatched values return null. |

For eQSL, pass an account from `GetAccountsAsync` through `DownloadRequest.Account`; null means all available accounts. HRDLog requires only a callsign. EPC normalizes the login callsign to uppercase and preserves the supplied password.

## ADIF reference and compatibility

The [official ADIF specification](https://www.adif.org/) is authoritative. Metadata was reviewed against [ADIF 3.1.8](https://www.adif.org/318/ADIF_318.htm) on 2026-10-25: 181 QSO properties and 33 band intervals. Protocol revisions are independent of NuGet/product versions.

- `GetAdifRows()` preserves the provider-compatible API: older/tagged headers, legacy modes/statuses, Unicode provider data, CALL filtering, trimming and CALL/BAND casing. It does not apply the latest semantic rules to eQSL. Duplicate fields project their last value; the document API retains all occurrences in compatibility mode.
- `ReadDocument(AdifProfile.Standard)` preserves exact field values, empty fields, header metadata and extensions. It accepts older declared revisions and import-only enum values, while enforcing supported ADI grammar/types. It rejects duplicate fields as an explicit strict-profile policy. It does not require CALL.
- `AdifWriter.Export(document)` enforces reviewed current export eligibility. It rejects international fields, deprecated representations, unknown unregistered fields and invalid values instead of silently losing data. Existing older ADIF_VER declarations are not upgraded: use explicit compatibility export to preserve them.
- `AdifWriter.Export(document, AdifProfile.EqslLegacy)` preserves older/provider metadata and values without claiming current conformance. eQSL upload uses the original caller bytes after compatibility parsing; no version or semantic upgrade is applied.
- `AdifHelper.ExportAsADIF(rows)` now writes a valid header, the reviewed protocol revision, invariant Gregorian UTC timestamp and nonempty records. The explicit `EqslLegacy` overload retains the earlier helper's protocol declaration. Null properties are omitted; empty strings remain fields. Empty documents/records fail explicitly.

```csharp
using var reader = new AdifReader(text);
var document = reader.ReadDocument(AdifProfile.EqslLegacy);
var preserved = AdifWriter.Export(document, AdifProfile.EqslLegacy);
```

The strict profile checks ASCII/CRLF rules, supported number precision, dates/times, numeric ranges, catalog enumerations, known mode/submode relations, USERDEF constraints, APP type consistency, grid extensions, QSL dates/statuses and reviewed primary subdivisions. It is not an exhaustive validator of external DARC/county/award registries, all historical DXCC dates or geographic locator adjacency. Such raw values remain modeled; callers needing those external checks must supply them. Numeric validation uses decimal precision and rejects larger values with a diagnostic. Unknown imported fields are retained, but standard export requires legal APP/USERDEF declarations. No arbitrary text between fields is retained, and XML/ADX is not parsed.

Limits: 32 Ã— 1024 Ã— 1024 input/output string characters, 100,000 fields per document and 4,096 fields per header/record. Parsing is synchronous CPU work with cancellation checks. Malformed tags and incomplete records fail even without CALL. The field model itself stores raw strings and does not validate property assignment.

`FreqToBand` uses the reviewed table including endpoints, 8m, 5m, submm and corrected microwave ranges. Band labels do not imply local permission to transmit.

Before ADIF implementation, review and release preparation, maintainers check for new official releases and record available versus implemented support. New standards must preserve tested older-provider compatibility. ADX is optional and remains outside this ADI milestone. See [the review](https://github.com/ch0mik/SQ7MRU.Utils/blob/master/docs/adif-conformance-review.md).

## Results, cancellation and errors

- Counts represent logical selections, not physical files. One EPC ZIP can produce several certificates.
- `DownloadProgress.Total` grows during discovery. Item callbacks can originate from concurrent workers; marshal UI changes through an appropriate synchronization context and avoid throwing from callbacks.
- A completed `DownloadResult` may contain failed items. Inspect `Failed`, `ItemResult.Failure` and `ItemResult.Message` rather than assuming completion means complete success.
- Initial authentication or certificate-fetch authentication failures throw `ServiceException`; use its `Kind` instead of parsing messages. Other per-item service/storage failures are recorded in results.
- A discovery failure ends that iterator after previously scheduled work finishes. EPC can isolate a category failure and continue other categories.
- Caller cancellation throws `OperationCanceledException` after worker cleanup. No final result is returned; already committed files remain available.
- A transport's internal timeout is reported as `ServiceException` with `FailureKind.Timeout`, not caller cancellation.
- Upload requests are not replayed automatically. An interrupted or unconfirmed upload may already have reached eQSL; inspect the remote account before submitting it again.

## Downloads and storage

`<technical-directory>` is configured through `ArtifactStorageOptions.TechnicalDirectoryName`; `.sq7mru` is the default, not a fixed required name. Configure it once before resolving services:

```csharp
using SQ7MRU.Utils.Infrastructure;

services.Configure<ArtifactStorageOptions>(options =>
    options.TechnicalDirectoryName = "technical-data");
```

The name must be one portable directory component, at most 100 characters. Paths, traversal, trailing spaces/dots and Windows device names are rejected. ADIF exports, SQLite and its lock share the configured directory. Changing it does not migrate previous files or indexes; bundles may be downloaded again until indexed there. The desktop exposes this setting through `SQ7MRU_TECHNICAL_DIRECTORY` at startup.

eQSL ADIF exports are technical files saved as `<output>/<technical-directory>/<callsign>_<QTH>.adif`, beside `downloads.sqlite` (the QTH suffix is omitted when empty). Graphical cards remain in their account folders. Exports are refreshed each run. Older ADIF files in card folders are not automatically moved or deleted.

Valid existing certificates are skipped; ADIF exports and explicitly refresh-marked items are fetched again. Actual image/PDF extensions are detected from content. EPC archives are extracted directly into their group directory and indexed in `<output>/<technical-directory>/downloads.sqlite`. Every indexed member must remain valid for a bundle to be skipped. Remote additions inside an already complete bundle are not polled automatically.

Paths are confined to the output root and untrusted symbolic links/junctions are rejected. Files use atomic replacement. A ZIP is validated before certificate writes and its SQLite index is committed last; the whole bundle is not one filesystem transaction.

Images are decoded with a 32 MiB encoded limit, 8192 pixels per axis and a four-million-pixel area limit. Opted-in PDFs receive bounded structural checks, not rendering or active-content sanitization. Archives permit 1Ã¢â‚¬â€œ128 flat regular certificate entries, at most 32 MiB compressed/per entry and 64 MiB expanded in total.

## Transport and extension points

`DownloadOptions` controls concurrency; defaults are two workers and one for eQSL. `ServiceHttpOptions` controls retry counts, per-attempt timeouts, total logical-request budgets and request spacing. eQSL generation spacing starts at one second and increases by 500 ms after recognized rate limits. Server cooldowns and Retry-After remain respected.

`OperationTimeout` applies to one HTTP request across retries, not an entire download job. Use a caller cancellation deadline for a job-wide limit. Replay-safe GET/search/generation requests can be retried; login and upload must not be marked safe for transient-error replay.

Replace `IDownloadSessionFactory`, `IDownloadSession`, `IArtifactStore` or `IServiceHttpSessionFactory` through DI for controlled integrations and tests. Created sessions and clients are owned by their caller; the shared scheduler is owned by the provider. A custom `ServiceHttpSession.SendAsync` request factory is trusted: create fresh requests and validate initial addresses through `Resolve`. Never log legacy resource URLs or query strings, which can contain credentials.

## XML API reference for IDEs and agents

The package ships compiler-generated XML comments alongside all three DLLs under `lib/net10.0/`:

- `SQ7MRU.Utils.xml`
- `SQ7MRU.Utils.Core.xml`
- `SQ7MRU.Utils.Application.xml`

They document public methods, constructors, contracts, options and the ADIF row model through summaries, parameters, returns, exceptions and behavior notes. Documentation originates from `///` comments immediately above the declarations. Missing public XML comments fail the library build. Read the declared cancellation, ownership, replay and partial-failure contracts before generating integration code.

The package-consumer check verifies the README and XML files as well as AnyCPU assemblies and native SQLite execution. Source and the as-built OpenSpec specifications are available in the [project repository](https://github.com/ch0mik/SQ7MRU.Utils).
