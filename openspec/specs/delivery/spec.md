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
- **AND** the release-upload job is intentionally skipped

#### Scenario: One automatic desktop run

- **WHEN** a version tag is pushed and its GitHub Release is then published
- **THEN** only the published release event starts the desktop matrix
- **AND** the tag-push event starts NuGet publishing without queuing another desktop matrix

#### Scenario: ARM64 targets

- **WHEN** the desktop release matrix runs
- **THEN** Windows ARM64 uses windows-11-arm and Linux ARM64 uses ubuntu-24.04-arm
- **AND** each desktop RID has a distinct executable, checksum and workflow artifact name

#### Scenario: 32-bit desktop publication

- **WHEN** the desktop matrix publishes win-x86 or linux-arm
- **THEN** it cross-publishes from windows-latest or ubuntu-24.04-arm respectively
- **AND** source tests run on the host architecture, without claiming native 32-bit runtime validation
- **AND** linux-x86 is not published because the selected .NET SDK does not provide its application host

#### Scenario: Release assets

- **WHEN** all eight desktop matrix jobs succeed for a published v-prefixed release event
- **THEN** the release job attaches Windows EXEs, native Linux/macOS executables and their SHA-256 files directly to the release for that tag without ZIP/TAR wrapping
- **AND** an unsuccessful matrix job prevents release asset publication

#### Scenario: Existing GitHub Release

- **WHEN** a v-prefixed release is published
- **THEN** all eight desktop executables and checksums are uploaded to that release without replacing its title or description
- **AND** reruns replace matching asset filenames and desktop runs for the same tag are serialized
- **AND** the workflow does not create a release; an unavailable release causes asset publication to fail

### Requirement: Self-contained single-file desktop

Desktop publication SHALL produce one executable per RID with the .NET runtime and native package libraries bundled, without requiring a separately installed .NET runtime. Native libraries MAY self-extract at startup. External debug-symbol files MUST NOT be shipped beside the executable. System GUI and C-library dependencies SHALL remain operating-system prerequisites.

#### Scenario: Publish output verification

- **WHEN** a desktop target is published
- **THEN** the workflow checks that the output directory contains exactly the expected executable before staging it as a direct release asset
- **AND** single-file publication disables trimming, embeds managed symbols and excludes external PDB files
- **AND** any extra publish-output file fails that job and prevents release asset upload
- **AND** library API XML documentation remains packaged in NuGet but is excluded from desktop output

#### Scenario: Local publish with a target runtime

- **WHEN** the desktop project is published with an explicit RID using `dotnet publish -c Release -r <rid>`
- **THEN** project defaults enable self-contained single-file publication and bundle native libraries for extraction, without additional CLI flags
- **AND** ordinary local `dotnet run` without a RID remains available

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
- [.gitignore](../../../.gitignore)
- [global.json](../../../global.json)
- [Directory.Build.props](../../../Directory.Build.props)

## Verification references

- `eng/verify-package.ps1`
- `SQ7MRU.Utils.Tests`
