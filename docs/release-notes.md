# Release notes

## C# repository tooling

- Replace auxiliary scripts with a non-packable C#/.NET build-tools project for SHA256SUMS and reviewed ADIF metadata generation.
- Move checksum regression tests into xUnit and add metadata extraction/inventory tests.
- Remove the Python scripts and workflow dependency; all build-tool tests run with the solution test suite.

## ADIF support and backwards compatibility

- Add all 181 QSO fields from the reviewed ADIF 3.1.8 release and correct public XML descriptions.
- Add a field-preserving document API and explicit standard/provider compatibility profiles.
- Correct ADI header framing, Gregorian UTC timestamps, field/type/export restrictions and reviewed band mappings.
- Validate supported values, USERDEF ranges, APP types, mode/submode and QSL relationships, with bounded parsing and cancellation.
- Preserve older ADIF import and eQSL behavior. Regression tests cover ADIF 1.0, 2.2.7, 3.0.6 and 3.1.8; eQSL uploads keep original payload bytes and version declarations.
- Require explicit compatibility export for older/provider declarations and deprecated values. Standard export rejects unsupported data instead of silently dropping it.
- Check official releases before future ADIF work and release preparation. ADX remains optional and unsupported; external registries and all historical/geographic constraints are outside this profile.
- Local verification passed 206 tests, installed-NuGet consumption, OpenSpec validation and single-file Windows x64 publication.

## Desktop distribution packages

- Distribute Windows builds as ZIP archives, Linux builds as TAR.GZ archives preserving executable permissions, and macOS builds as DMG images.
- macOS DMGs contain `SQ7MRU.Downloader.app`, an application icon and an Applications shortcut for drag-and-drop installation.
- Each application executable includes the .NET runtime and native package libraries; no separately installed .NET runtime is required.
- Retain all eight targets: Windows x86/x64/ARM64, Linux x64/ARM32/ARM64 and macOS x64/ARM64.
- Verify archive contents and published executable bytes. Validate and mount DMGs read-only to check the application bundle before hashing.
- Create distribution containers only in GitHub Actions.

## Package integrity and publication

- Calculate SHA-256 from each completed ZIP, TAR.GZ or DMG and publish a combined `SHA256SUMS` manifest.
- Verify all eight package hashes, required platform formats and a shared version before attaching release assets.
- Block release upload when a package is missing, modified, duplicated or belongs to a different version.
- Retain all packages and the manifest in the `release-desktop-packages` workflow artifact, including for manual runs.
- Keep desktop publication tied to published GitHub Releases; tag pushes continue to trigger NuGet publication only.
- Preserve release titles and descriptions. Reruns replace matching package assets and workflow artifacts.

## Technical download files

- Configure the shared technical directory through `ArtifactStorageOptions.TechnicalDirectoryName`; `.sq7mru` is only the default.
- Allow desktop users to override it with `SQ7MRU_TECHNICAL_DIRECTORY` before startup.
- Reject unsafe directory names and keep SQLite, its lock and ADIF exports together without migrating previous data automatically.
- Save eQSL ADIF exports directly in `<output>/<technical-directory>/`, beside `downloads.sqlite`.
- Keep separate `<callsign>_<QTH>.adif` filenames for each account, omitting the QTH suffix when empty.
- Continue refreshing exports on each run while keeping newly downloaded ADIF files out of QSL image folders.
- Leave existing exports in the previous location untouched.

## Documentation and validation

- Update repository and package READMEs, OpenSpec, repository hygiene guidance and generated-artifact ignore rules.
- Local Windows validation passed 206 .NET tests (including checksum-promotion and metadata-generator tests).
- Workflow YAML/event-boundary checks, PowerShell syntax checks and strict validation of all eight OpenSpec capabilities passed.
- Actual platform container creation and mounted-DMG checks run on the corresponding GitHub Actions runners; local checks do not establish successful remote execution.

## Platform notes

- Native package libraries may extract automatically at startup; Linux still requires system GUI and C libraries.
- Windows x86 and Linux ARM32 are cross-published; source tests run on their 64-bit hosts.
- macOS Developer ID signing and notarization are not configured. A DMG alone does not establish Gatekeeper trust.
- Release versions are supplied by GitHub tags, without a concrete product release number in source or documentation.
