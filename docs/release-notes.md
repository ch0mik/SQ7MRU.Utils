# Release notes

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

- Save eQSL ADIF exports directly in `<output>/.sq7mru/`, beside `downloads.sqlite`.
- Keep separate `<callsign>_<QTH>.adif` filenames for each account, omitting the QTH suffix when empty.
- Continue refreshing exports on each run while keeping newly downloaded ADIF files out of QSL image folders.
- Leave existing exports in the previous location untouched.

## Documentation and validation

- Update repository and package READMEs, OpenSpec, repository hygiene guidance and generated-artifact ignore rules.
- Local Windows validation passed 107 .NET tests and six checksum-promotion tests.
- Workflow YAML/event-boundary checks, PowerShell syntax checks and strict validation of all eight OpenSpec capabilities passed.
- Actual platform container creation and mounted-DMG checks run on the corresponding GitHub Actions runners; local checks do not establish successful remote execution.

## Platform notes

- Native package libraries may extract automatically at startup; Linux still requires system GUI and C libraries.
- Windows x86 and Linux ARM32 are cross-published; source tests run on their 64-bit hosts.
- macOS Developer ID signing and notarization are not configured. A DMG alone does not establish Gatekeeper trust.
- Release versions are supplied by GitHub tags, without a concrete product release number in source or documentation.
