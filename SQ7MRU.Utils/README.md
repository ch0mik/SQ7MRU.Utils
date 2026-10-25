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
| `AdifHelper.ExportAsADIF` | Exports string-backed rows with a header and record terminators; does not validate or upload them. |
| `AdifHelper.ConvertToString` | Serializes one row without a file header. |
| `AdifHelper.FixRecord` | Mutates only callsign/band casing and returns the same record instance. |
| `AdifHelper.FreqToBand` | Maps MHz to the implemented legacy ADIF band intervals; strict interval endpoints and unmatched values return null. |

For eQSL, pass an account from `GetAccountsAsync` through `DownloadRequest.Account`; null means all available accounts. HRDLog requires only a callsign. EPC normalizes the login callsign to uppercase and preserves the supplied password.

## ADIF reference and compatibility

The [ADIF 1.0 specification](https://www.adif.org/100/adif_100.htm) describes the base physical format: case-insensitive field names, declared lengths, optional type indicators, record markers, zero-length fields and arbitrary field order. The reader accepts these text-format constructs; type indicators are not interpreted.

Library policies differ from the general format: records without a nonblank CALL are omitted, values are trimmed, CALL/BAND casing is normalized, and unknown fields are skipped rather than retained. The string-based API does not handle arbitrary binary payloads or validate the full field schema. The row model contains fields from later revisions too. Export currently declares ADIF_VER 3.0.6 and uses a tagged header; it does not implement the strict ADIF 1.0 header convention. These utilities do not claim full conformance to every ADIF revision.

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

Valid existing certificates are skipped; ADIF exports and explicitly refresh-marked items are fetched again. Actual image/PDF extensions are detected from content. EPC archives are extracted directly into their group directory and indexed in `<output>/.sq7mru/downloads.sqlite`. Every indexed member must remain valid for a bundle to be skipped. Remote additions inside an already complete bundle are not polled automatically.

Paths are confined to the output root and untrusted symbolic links/junctions are rejected. Files use atomic replacement. A ZIP is validated before certificate writes and its SQLite index is committed last; the whole bundle is not one filesystem transaction.

Images are decoded with a 32 MiB encoded limit, 8192 pixels per axis and a four-million-pixel area limit. Opted-in PDFs receive bounded structural checks, not rendering or active-content sanitization. Archives permit 1–128 flat regular certificate entries, at most 32 MiB compressed/per entry and 64 MiB expanded in total.

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
