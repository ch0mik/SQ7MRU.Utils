# SQ7MRU.Utils

Asynchronous .NET 10 library for ADIF, eQSL.cc, HRDLog/iQSL and EPC. The desktop application is a separate project: **SQ7MRU.Downloader** (Avalonia 12, MVVM).

## Library

Install `SQ7MRU.Utils` from nuget.org. The single platform-neutral `net10.0` package includes AnyCPU Core, Application and infrastructure assemblies; there are no separate per-RID or internal packages to install and no Avalonia dependency. Native SQLite assets are resolved through NuGet dependencies for the consuming platform.

The package includes a [library-focused README](SQ7MRU.Utils/README.md) and generated XML documentation beside all three DLLs. Public API declarations carry `///` comments covering behavior, parameters, results, cancellation, errors and ownership/replay rules where relevant. These descriptions are available to IDEs and agents directly from source or the installed package. Missing public comments fail library builds; package verification checks README/XML presence and documented summaries.

```csharp
using Microsoft.Extensions.DependencyInjection;
using SQ7MRU.Utils;

var services = new ServiceCollection();
services.AddSq7MruUtils(
    downloads => downloads.MaxConcurrency = 2,
    http => http.MaxRetries = 4);
await using var provider = services.BuildServiceProvider();
using var cancellation = new CancellationTokenSource();
var downloader = provider.GetRequiredService<IDownloadService>();
var result = await downloader.DownloadAsync(
    new DownloadRequest(QslService.Eqsl,
        new ServiceCredentials("YOUR_CALLSIGN", "YOUR_PASSWORD"), "./downloads"),
    new Progress<DownloadProgress>(p => Console.WriteLine(p.Message)),
    cancellation.Token);
Console.WriteLine($"Downloaded {result.Downloaded}; skipped {result.Skipped}; failed {result.Failed}");
```

`IEqslAccountService.GetAccountsAsync` lists accounts/QTHs. Pass a selected `ServiceAccount` in `DownloadRequest` to download that account; omit it to download all accounts. Each account has an isolated authenticated session. Discovery and download run as a bounded asynchronous pipeline. eQSL uses the legacy primary-login POST and literal `SelectCallsign=Log+In` account-switch value; linked accounts are switched only after establishing a primary session. Authentication is verified from protected account data, not from generic login widgets in the login response. eQSL account language must be English. `QslService.HrdLog` only requires a login/callsign. `IAdifUploader.UploadAsync` uploads ADIF and never automatically replays an upload with an uncertain outcome.

Network requests and file payload reads/writes use asynchronous APIs. ADIF parsing and native filesystem confinement checks are synchronous; SQLite executes on a worker thread. Cancellation propagates as `OperationCanceledException` and stops requests, retries and pending work. Completed files remain available; valid existing images, opted-in PDFs and complete indexed EPC bundles are skipped on subsequent runs. The ADIF export is always refreshed atomically. Cancellation does not return a final summary. A failed individual file is reported in `DownloadResult.Items`; authentication failures abort the operation with `ServiceException`. A service failure during discovery is returned as a `[discovery]` failure after earlier discovered items finish; later undiscovered items are not processed. Treat any nonzero `Failed` count as partial failure.

HTTP sessions isolate cookies per operation. A scheduler shared within the DI container enforces request spacing and server cooldowns per service. Retries cover 429, selected transient 5xx/408 responses, network failures, request timeouts and recognized HTML rate-limit messages. `Retry-After` accepts delta or date; waits exceeding the request budget fail rather than retry early. Read-only search and award-generation POSTs explicitly opt into replay; login and upload do not. `OperationTimeout` bounds each logical HTTP request including all its retries; it does not bound the complete download job. Supply a deadline through the caller's cancellation token to bound the job. Upload success requires an explicit success marker or a positive record count; a failure marker takes precedence.

The library does not log credentials, request URLs or response bodies. Passwords live in memory and are not persisted by the desktop application. eQSL's legacy inbox endpoint requires a password query parameter over HTTPS; consumers adding HTTP instrumentation must redact query strings. Service pages are parsed defensively and unexpected formats are reported, not saved as images. Cards are decoded before acceptance (JPEG, PNG and GIF; maximum 4 million pixels and 32 MiB encoded size) and written through a temporary file before atomic replacement. Output paths reject symbolic links and junctions; Windows holds directory handles against rename while Unix uses no-follow, directory-relative file operations. macOS system aliases `/var` and `/tmp` are normalized to `/private`.

## Specifications and repository hygiene

The current implementation is documented in [OpenSpec](openspec/README.md), with eight capability specifications and links to source and tests. Validate the baseline with:

```sh
openspec validate --all --strict --no-interactive
```

See [public repository hygiene](docs/repository-hygiene.md) for local data exclusions and pre-commit checks.

## Architecture

- `SQ7MRU.Utils.Core`: contracts, requests, results and ports; no HTTP or UI dependencies.
- `SQ7MRU.Utils.Application`: download use case, bounded concurrency, cancellation and partial-result aggregation.
- `SQ7MRU.Utils`: HTTP/service and filesystem adapters, ADIF utilities, DI composition and NuGet packaging.
- `SQ7MRU.Downloader`: Avalonia MVVM application, asynchronous commands, progress, cancellation and account selection.
- `SampleApp`: asynchronous CLI consumer. Password comes from `SQ7MRU_PASSWORD`, never command-line arguments.
- `SQ7MRU.Utils.Tests`: deterministic HTTP/time tests, application and parser/storage tests, headless Avalonia tests.

DI registrations use `TryAdd` so consumers can replace transport/storage ports. Keep one service provider for the application's lifetime to share service cooldowns. eQSL defaults to one concurrent operation; other services use `MaxConcurrency`. Override concurrency per service with `downloads.MaxConcurrencyByService[QslService.Epc] = 1`. eQSL card generation spacing starts at 1 second by default and increases by 500 ms after each recognized eQSL rate-limit response. Learned spacing is shared across sessions and retries for the service provider lifetime; successful responses do not reduce it. `ServiceHttpOptions.EqslCardInterval` sets the initial interval and must be at least 1 second. Server `Retry-After` and the shared retry cooldown still apply; restarting with a new service provider resets the learned spacing. No service locator is used in application logic.

## Build and run

Install .NET 10 SDK (selected by `global.json`).

```sh
dotnet restore SQ7MRU.Utils.sln
dotnet build SQ7MRU.Utils.sln -c Release --no-restore
dotnet test SQ7MRU.Utils.sln -c Release --no-build
dotnet run --project SQ7MRU.Downloader
dotnet run --project SampleApp -- HrdLog SQ7MRU ./downloads
```

The desktop application supports eQSL, HRDLog and EPC downloads, account selection, a directory picker, cancellation, rate-limit status and per-file results. For eQSL, starting without loaded accounts first loads the account list and waits for the user to choose an account/QTH and start again. The library API still supports downloading all accounts when no account is specified. The upload API is available to library consumers; the initial desktop UI focuses on downloads.

EPC discovers categories dynamically within the certificate accordion, including membership and contest certificates, while excluding account-management menus. Certificates are saved as validated JPEG, PNG, GIF or PDF. ZIP responses are unpacked directly into `<output>/EPC/<login>/<group>/`, without saving the archive or adding another award subdirectory.

Valid existing certificates and complete bundles indexed in `<output>/.sq7mru/downloads.sqlite` are skipped. Missing or corrupt certificates are downloaded again. Progress counts selections: one ZIP selection can produce several certificate files. Completed bundles are not polled for new remote contents.

See the [EPC download guide](docs/epc-downloads.md) for menu discovery, output layout, SQLite/JSON migration, content limits and resume behavior. Copy-ready English release text is available in [release notes](docs/release-notes.md).

## Migration from 1.x

Version 2 is a breaking release. Replace `new Downloader(...).Download()`, `new iQSL(...).Download()` and `new EPC(...).Download()` with `IDownloadService.DownloadAsync`. Replace `Uploader.UploadAdif` with `IAdifUploader.UploadAsync`. Register services through `AddSq7MruUtils`; use `ServiceException.Kind` and per-file results instead of parsing log messages. Synchronous network APIs and inheritance from `HttpClient` have been removed. DMC support has been removed. ADIF parsing is case-insensitive and length-aware, supports records without a header and rejects truncated records.

Downloaded eQSL ADIF logs are stored in `<output>/.sq7mru/<callsign>_<QTH>.adif`, alongside the technical SQLite index, rather than among QSL images. An empty QTH omits the suffix. Each export is refreshed; existing exports in the previous location are left untouched.

## Publishing

The desktop project defaults to self-contained single-file output when a RID is specified, following [Microsoft's single-file deployment guidance](https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview?tabs=cli). For example, `dotnet publish SQ7MRU.Downloader/SQ7MRU.Downloader.csproj -c Release -r win-x64 -o artifacts/desktop-win-x64` produces one executable containing the .NET runtime and native package libraries. API documentation XML files remain in NuGet and are excluded from desktop publish output.

Three workflows live in `.github/workflows`:

- `ci.yml`: build and test on Windows, Linux and macOS, each on native x64 and ARM64 runners; pack and install the generated NuGet package into an isolated consumer; retain test results and packages as workflow artifacts.
- `nuget-release.yml`: a SemVer `v*` tag builds one candidate package, verifies that same artifact on all six native OS/CPU targets, then publishes **only SQ7MRU.Utils** to nuget.org if every verification job succeeds. Package version is taken from the tag. Prerelease tags are supported.
- `desktop-release.yml`: publication of a GitHub Release for a `v*` tag builds/tests and produces self-contained single-file executables for `win-x86`, `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `linux-arm`, `osx-x64`, `osx-arm64`. Windows ZIPs, Linux TAR.GZ archives and macOS DMGs are verified and attached to that existing release with a combined `SHA256SUMS`. Tag pushes do not start this workflow. `workflow_dispatch` produces the same packages and manifest as workflow artifacts without creating a release.

Desktop publishes eight targets. The six 64-bit targets are shared with native CI and NuGet release verification; the two 32-bit desktop targets are cross-published and run source tests on their 64-bit host.

Automatic publishing uses separate events, so creating a tag and publishing its release starts one desktop run:

| Event | NuGet publishing | Desktop behavior |
| --- | --- | --- |
| Version-tag push | Build, verify and publish the package | No run |
| GitHub Release published for that tag | No run | Build and attach release assets |
| Manual desktop workflow run | No run | Upload workflow artifacts only; release job is skipped |

Native target mapping:

| RID | GitHub runner | Release asset |
| --- | --- | --- |
| `win-x86` | `windows-latest` (cross-publish from x64) | ZIP containing EXE |
| `win-x64` | `windows-latest` | ZIP containing EXE |
| `win-arm64` | `windows-11-arm` | ZIP containing EXE |
| `linux-x64` | `ubuntu-latest` | TAR.GZ containing executable |
| `linux-arm64` | `ubuntu-24.04-arm` | TAR.GZ containing executable |
| `linux-arm` | `ubuntu-24.04-arm` (cross-publish from ARM64) | TAR.GZ containing executable |
| `osx-x64` | `macos-15-intel` | DMG containing app bundle |
| `osx-arm64` | `macos-latest` | DMG containing app bundle |

NuGet release publication uses three stages: `pack` builds one AnyCPU candidate; `verify` downloads and executes that same package on each of the six native 64-bit targets above; `publish` uploads the original candidate only after all verification jobs succeed. The isolated consumer checks all three shipped assemblies for CPU-neutral IL and exercises the native SQLite dependency. A failure on any platform blocks NuGet publication.

The native storage adapter chooses Linux filesystem flags from the running process architecture, so consuming the same portable package on ARM64 does not reuse x64-only flag values. Local ABI regression checks complement the native tests executed by the release matrix.

Packages use `SQ7MRU.Downloader-<version>-<rid>.zip`, `.tar.gz` or `.dmg` filenames and separate `desktop-<rid>` workflow artifacts. The aggregation job waits for every matrix build, verifies all eight final container hashes and their common version, then writes `SHA256SUMS`. The `release-desktop-packages` artifact contains all packages and that manifest, including on manual runs. Only published release events upload to GitHub Releases. The eight-target desktop matrix is configuration. Native execution of its six 64-bit targets must be confirmed by GitHub Actions; 32-bit targets are cross-published rather than runtime-tested natively.

Create a version tag on GitHub to start NuGet publishing, then publish the GitHub Release for that tag to start desktop publishing. This avoids duplicate desktop builds when both events occur. Release titles and notes are preserved; the desktop job attaches assets to the existing release without creating one. Reruns replace matching desktop asset filenames, and desktop runs for the same tag are serialized. The workflow version must be committed and available on the relevant tag; rerunning an old workflow run does not pick up newer YAML changes.

Release versions are maintained on GitHub, without concrete tag numbers in source or documentation. The workflows pass the tag-derived version to MSBuild; the desktop manifest and HTTP User-Agent use the generated assembly version. Untagged builds use the SDK-evaluated version, and package-consumer verification discovers the generated package version from its metadata when no version is supplied.

Before the first NuGet release, configure Trusted Publishing:

1. Create a nuget.org Trusted Publishing policy for owner `ch0mik`, repository `SQ7MRU.Utils`, workflow filename `nuget-release.yml`, environment `nuget` and package scope `SQ7MRU.Utils`.
2. Set repository variable `NUGET_USER` to the nuget.org profile name that owns/can publish the package.
3. Configure the GitHub environment `nuget` to allow the intended release tags. The workflow uses `NuGet/login@v1` and GitHub OIDC; no long-lived NuGet API key is required.
4. Commit and push the reviewed source and workflow changes, then create the intended version tag on GitHub. Local builds never publish to nuget.org.

See [NuGet Trusted Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing).

Run the desktop application locally with `dotnet run --project ./SQ7MRU.Downloader/`. Each RID produces one executable with the .NET runtime included, then GitHub Actions wraps it in the platform's distribution container. Native package libraries self-extract at startup; no separately installed .NET runtime is required. Managed debug symbols are embedded and external PDB/API XML files are excluded. System GUI dependencies still apply (see [Avalonia supported platforms](https://docs.avaloniaui.net/docs/supported-platforms)). Archive and DMG creation runs only in GitHub Actions.

Windows ZIP and Linux TAR.GZ contain only the executable; TAR.GZ retains executable permissions. macOS DMGs contain `SQ7MRU.Downloader.app` with an icon and versioned `Info.plist`, plus an Applications shortcut for drag-and-drop installation. Each DMG is verified and mounted read-only to check its contents before hashing. Developer ID signing and notarization are not configured; a DMG alone does not establish Gatekeeper trust.

External sites can change forms and response formats. Tests use synthetic HTML/HTTP fixtures, not real accounts; validate authenticated flows against the services before a production release.

Download `SHA256SUMS` and the desired package into the same directory. Hashes refer to the finished ZIP/TAR.GZ/DMG bytes. On Linux use `sha256sum --ignore-missing -c SHA256SUMS`. On macOS use `shasum -a 256 <package.dmg>` and compare the value with the matching manifest line; on Windows use `Get-FileHash .\<package.zip> -Algorithm SHA256`. Verify the downloaded container before extracting or opening it.
