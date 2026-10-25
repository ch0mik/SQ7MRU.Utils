# SQ7MRU.Utils 2.0.0 / SQ7MRU.Downloader 2.0.0

This release introduces a .NET 10 library and a separate Avalonia desktop downloader, with asynchronous service integrations, resumable downloads and GitHub Actions release workflows.

## Highlights

- Separate `SQ7MRU.Downloader` desktop application with an English UI, eQSL account/QTH selection, directory picker, cancellation and live results displayed newest first.
- Standalone `SQ7MRU.Utils` NuGet package with dependency injection, cancellation tokens, bounded concurrency and per-item download results. The library has no Avalonia dependency.
- Isolated cookie sessions, bounded retries, shared service cooldowns, `Retry-After` handling and paced eQSL card generation.
- Adaptive eQSL generation spacing starts at 1 second and increases by 500 ms after each rate-limit response, while respecting server-requested waits.
- Updated eQSL authentication, linked-account switching and ADIF export/download handling.
- Dynamic EPC category discovery within the certificate accordion, including membership and contest certificates, without a fixed category list.
- EPC JPEG, PNG, GIF and PDF certificates, plus automatic extraction of certificate ZIP responses into the group directory.
- A technical SQLite index for completed EPC bundles. Valid existing certificates and complete bundles are skipped; missing or corrupt files are downloaded again.
- Confined output paths, rejection of symbolic links and junctions, bounded content validation and atomic file replacement.
- Correct native file-creation mode passing on macOS ARM64, preserving private writable certificates and reusable SQLite locks.
- Improved ADIF parsing with case-insensitive tags, length-aware fields, invariant normalization and cancellation checks.
- OpenSpec documentation of implemented architecture, service behavior, security boundaries and delivery workflows.

## Breaking changes

- .NET 10 is required for library consumers and local development.
- Replace the synchronous `Downloader`, `EPC`, `iQSL` and `Uploader` classes with `IDownloadService`, `IEqslAccountService` and `IAdifUploader`, registered through `AddSq7MruUtils`.
- DMC support has been removed.
- Local `build-publish-multi.*` scripts have been removed. Desktop and NuGet publishing use separate GitHub Actions workflows.

## Download behavior

- Existing valid certificates are reused. ADIF exports are refreshed on every run.
- EPC archives are unpacked directly into `<output>/EPC/<login>/<group>/`; downloaded ZIPs are not retained.
- Progress counts logical download selections. One ZIP selection may contain several certificate files.
- Bundle metadata is stored in `<output>/.sq7mru/downloads.sqlite`; older JSON indexes are migrated after certificate validation.
- An already complete EPC bundle is not downloaded again to check for remote additions. Missing local bundle members trigger a new download.
- PDF acceptance uses structural checks; documents are not rendered or sanitized.

## Delivery and validation

- Local verification passed: 104 tests, a Release build without warnings, isolated NuGet consumption, and strict validation of all eight OpenSpec specifications.
- CI is configured to build and test on Windows, Linux and macOS, including isolated NuGet package consumption with SQLite.
- NuGet releases use `v*` tags and Trusted Publishing; desktop releases use `desktop-v*` tags and provide platform archives with SHA-256 checksums.
- Desktop targets are Windows x64, Linux x64, macOS x64 and macOS ARM64. macOS distributions are unsigned; signing, notarization and installers are not included.
- Authenticated service pages can change. Automated integration tests use synthetic responses and do not guarantee every live-account flow.
