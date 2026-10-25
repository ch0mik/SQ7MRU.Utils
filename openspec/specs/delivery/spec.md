# Build, package and release delivery

## Purpose

Deliver a standalone NuGet library and separate desktop binaries through reproducible GitHub Actions workflows.

## Requirements

### Requirement: Multi-platform validation

CI SHALL restore, build and test on Windows, Linux and macOS, each on native x64 and ARM64 runners, and install the generated NuGet package into an isolated consumer. Tests SHALL use controlled HTTP responses rather than real service credentials.

#### Scenario: NuGet consumption

- **WHEN** the generated SQ7MRU.Utils package is restored into the smoke-test project
- **THEN** the consumer resolves public services and runs cancellation and ADIF checks
- **AND** ZIP certificate storage and the native SQLite dependency execute through the public package API
- **AND** all three shipped library assemblies are verified as CPU-neutral IL without a 32-bit requirement

#### Scenario: Packaged API documentation

- **WHEN** a generated NuGet package is checked before consumption
- **THEN** it contains a library-focused README and XML API documentation beside all three DLLs under lib/net10.0
- **AND** the XML files identify their corresponding assemblies and document public API summaries and parameter descriptions
- **AND** missing, empty or malformed required documentation fails package verification

### Requirement: NuGet publishing

A valid v-prefixed SemVer tag SHALL produce one platform-neutral package, verify that same artifact on all six native OS/CPU combinations, then publish via nuget.org Trusted Publishing. The publish job SHALL use the nuget environment and an OIDC-issued temporary key; NUGET_USER SHALL be configured externally.

#### Scenario: Package release

- **WHEN** a valid library version tag is pushed and registry trust is configured
- **THEN** only the verified SQ7MRU.Utils package is pushed to nuget.org

#### Scenario: NuGet platform failure

- **WHEN** package consumption or source tests fail on any of the six native targets
- **THEN** the NuGet publish job does not run
- **AND** each target has a distinct test-results artifact

#### Scenario: Single portable package

- **WHEN** the release verification matrix runs
- **THEN** each job downloads the same candidate package built by the pack job and executes it through an isolated consumer
- **AND** publishing downloads that candidate artifact rather than rebuilding it or publishing per-RID variants

#### Scenario: Candidate promotion

- **WHEN** the pack job has uploaded its candidate artifact
- **THEN** verification jobs may consume it, but its existence alone does not authorize publication
- **AND** the publish job depends on both the pack job and the entire successful verification matrix

#### Scenario: Shared release version

- **WHEN** a valid version tag is created on GitHub and its release is published
- **THEN** the tag push starts NuGet publishing and the published release event starts desktop publishing for the same version
- **AND** the tag push does not start a duplicate desktop build; release publication does not re-publish NuGet

#### Scenario: Version source

- **WHEN** a publishing workflow runs for a version tag defined on GitHub
- **THEN** the tag supplies the build and package version without a fixed release version in the repository
- **AND** the desktop manifest is generated from the computed assembly version
- **AND** the HTTP User-Agent reports the generated library assembly version rather than a fixed release number
- **WHEN** an untagged local or manual build runs
- **THEN** it uses the SDK-evaluated version and the package-consumer check discovers the package version from its metadata

### Requirement: Desktop publishing

GitHub Actions SHALL publish desktop assets for win-x86, win-x64, win-arm64, linux-x64, linux-arm64, linux-arm, osx-x64 and osx-arm64. The six 64-bit targets SHALL use native runners; 32-bit targets SHALL cross-publish from x64 Windows or ARM64 Linux hosts. Published v-prefixed releases SHALL trigger asset uploads; tag pushes MUST NOT trigger desktop publishing. Manual dispatch SHALL only upload workflow artifacts.

#### Scenario: Manual build

- **WHEN** desktop-release is manually dispatched
- **THEN** platform artifacts are uploaded without creating a GitHub Release
- **AND** packages and the combined checksum manifest are available as workflow artifacts; the GitHub release-upload step is skipped

#### Scenario: One automatic desktop run

- **WHEN** a version tag is pushed and its GitHub Release is then published
- **THEN** only the published release event starts the desktop matrix
- **AND** the tag-push event starts NuGet publishing without queuing another desktop matrix

#### Scenario: ARM64 targets

- **WHEN** the desktop release matrix runs
- **THEN** Windows ARM64 uses windows-11-arm and Linux ARM64 uses ubuntu-24.04-arm
- **AND** each desktop RID has a distinct distribution package, checksum and workflow artifact name

#### Scenario: 32-bit desktop publication

- **WHEN** the desktop matrix publishes win-x86 or linux-arm
- **THEN** it cross-publishes from windows-latest or ubuntu-24.04-arm respectively
- **AND** source tests run on the host architecture, without claiming native 32-bit runtime validation
- **AND** linux-x86 is not published because the selected .NET SDK does not provide its application host

#### Scenario: Release assets

- **WHEN** all eight desktop matrix jobs succeed for a published v-prefixed release event
- **THEN** the release job attaches three Windows ZIPs, three Linux TAR.GZ archives, two macOS DMGs and a combined SHA256SUMS file to the release for that tag
- **AND** an unsuccessful matrix job prevents release asset publication

#### Scenario: Existing GitHub Release

- **WHEN** a v-prefixed release is published
- **THEN** all eight desktop packages and SHA256SUMS are uploaded to that release without replacing its title or description
- **AND** reruns replace matching asset filenames and desktop runs for the same tag are serialized
- **AND** the workflow does not create a release; an unavailable release causes asset publication to fail

### Requirement: Self-contained single-file desktop

Desktop publication SHALL produce one executable per RID with the .NET runtime and native package libraries bundled, without requiring a separately installed .NET runtime. Native libraries MAY self-extract at startup. External debug-symbol files MUST NOT be shipped beside the executable. System GUI and C-library dependencies SHALL remain operating-system prerequisites.

#### Scenario: Publish output verification

- **WHEN** a desktop target is published
- **THEN** the workflow checks that the output directory contains exactly the expected executable before packaging it
- **AND** single-file publication disables trimming, embeds managed symbols and excludes external PDB files
- **AND** any extra publish-output file fails that job and prevents release asset upload
- **AND** library API XML documentation remains packaged in NuGet but is excluded from desktop output

#### Scenario: Linux system prerequisites

- **WHEN** a desktop release is used on Linux
- **THEN** documentation distinguishes bundled .NET from required system libraries and the graphical session
- **AND** it provides an explicitly scoped Ubuntu 24.04 package list with official sources and notes that other distributions need different package versions
- **AND** the current backend is identified as X11, with XWayland required in a Wayland session

#### Scenario: Local publish with a target runtime

- **WHEN** the desktop project is published with an explicit RID using `dotnet publish -c Release -r <rid>`
- **THEN** project defaults enable self-contained single-file publication and bundle native libraries for extraction, without additional CLI flags
- **AND** ordinary local `dotnet run` without a RID remains available

### Requirement: Platform distribution containers

Desktop workflows SHALL wrap Windows executables in ZIP and Linux executables in TAR.GZ preserving executable permissions. macOS SHALL distribute DMG containing a Finder-launchable app bundle, icon and Applications shortcut. Every container SHALL preserve the published executable bytes and include its runtime. Packaging SHALL run only in GitHub Actions as part of delivery.

#### Scenario: Archive verification

- **WHEN** a Windows or Linux container is built
- **THEN** its content is exactly the expected executable and the unpacked bytes match the publication
- **AND** the Linux archive retains executable permissions

#### Scenario: macOS distribution

- **WHEN** a macOS DMG is created
- **THEN** hdiutil verifies it and the workflow mounts it read-only to check the executable, Info.plist, icon and Applications shortcut
- **AND** the bundle uses the build version and contains the matching self-contained executable
- **AND** packaging does not claim Developer ID signing, notarization or Gatekeeper approval

### Requirement: Final package checksums

Each platform job SHALL hash its finished distribution container with SHA-256. After all builds succeed, an aggregation job SHALL validate exactly one package for every RID, a common version, the required platform formats and each package checksum, then write SHA256SUMS. Validation failure MUST prevent release upload. Hashes SHALL be generated after any operation that changes package bytes.

#### Scenario: Corrupt or incomplete distribution

- **WHEN** any package changes after hashing, a target is missing or packages use different versions
- **THEN** aggregation fails without uploading release assets

#### Scenario: Final manifest

- **WHEN** all eight distribution packages pass verification
- **THEN** SHA256SUMS contains sorted SHA-256 values and package filenames, excluding itself
- **AND** manual dispatch retains the same packages and manifest as workflow artifacts without publishing a release

### Requirement: Supported GitHub action runtime

GitHub-provided JavaScript actions SHALL declare a supported runner runtime instead of relying on forced migration from Node.js 20. The current checkout/setup/artifact action releases SHALL target Node.js 24. Updating these actions SHALL preserve the defined trigger, permission and artifact contracts.

#### Scenario: Action runtime upgrade

- **WHEN** checkout, .NET setup or artifact actions are upgraded
- **THEN** their official manifests declare Node.js 24 and their configured inputs remain supported
- **AND** CI, tag-based NuGet publishing and release-based desktop delivery keep their existing behavior

### Requirement: C# repository tooling

Repository checksum and ADIF metadata tooling SHALL use the non-packable C#/.NET SQ7MRU.Build project. Regression tests SHALL run in the existing xUnit suite. Workflows MUST NOT require Python scripts or runtime dependencies.

#### Scenario: .NET-only tooling

- **WHEN** the desktop workflow aggregates packages or a maintainer regenerates reviewed ADIF metadata
- **THEN** it invokes SQ7MRU.Build through the .NET SDK
- **AND** tool failures block the corresponding operation without adding dependencies to the published library

### Requirement: Public repository hygiene

The repository MUST NOT contain live credentials, cookies, downloaded QSO logs, certificates, runtime SQLite indexes or lock files, legacy JSON indexes, local diagnostic pages, build output or private local configuration. Synthetic source fixtures MAY be versioned explicitly in the test tree.

#### Scenario: Local working data

- **WHEN** a developer creates a real ADIF export, cookie jar, environment override or release archive
- **THEN** Git ignore rules exclude it from normal staging

## Implementation references

- [.github/workflows/ci.yml](../../../.github/workflows/ci.yml)
- [.github/workflows/nuget-release.yml](../../../.github/workflows/nuget-release.yml)
- [.github/workflows/desktop-release.yml](../../../.github/workflows/desktop-release.yml)
- [eng/verify-package.ps1](../../../eng/verify-package.ps1)
- [eng/package-desktop.ps1](../../../eng/package-desktop.ps1)
- [eng/render-macos-icon.swift](../../../eng/render-macos-icon.swift)
- [eng/SQ7MRU.Build/DesktopChecksums.cs](../../../eng/SQ7MRU.Build/DesktopChecksums.cs)
- [.gitignore](../../../.gitignore)
- [global.json](../../../global.json)
- [Directory.Build.props](../../../Directory.Build.props)
- [docs/linux-runtime-requirements.md](../../../docs/linux-runtime-requirements.md)

## Verification references

- `eng/verify-package.ps1`
- `DesktopChecksumsTests`
- `eng/package-desktop.ps1` archive checks and mounted-DMG checks on the corresponding runners
- `SQ7MRU.Utils.Tests`
