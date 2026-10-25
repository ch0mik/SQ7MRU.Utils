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

- **WHEN** a valid version tag is created on GitHub and its publication workflows run
- **THEN** NuGet and desktop publishing derive the version from that tag and desktop assets are attached to the release for the same tag
- **AND** publishing that release can also trigger desktop asset publication without re-publishing the NuGet package

#### Scenario: Version source

- **WHEN** a publishing workflow runs for a version tag defined on GitHub
- **THEN** the tag supplies the build and package version without a fixed release version in the repository
- **AND** the desktop manifest is generated from the computed assembly version
- **AND** the HTTP User-Agent reports the generated library assembly version rather than a fixed release number
- **WHEN** an untagged local or manual build runs
- **THEN** it uses the SDK-evaluated version and the package-consumer check discovers the package version from its metadata

### Requirement: Desktop publishing

GitHub Actions SHALL publish self-contained desktop archives and SHA-256 files for win-x64, win-arm64, linux-x64, linux-arm64, osx-x64 and osx-arm64. Each target SHALL build and test on a runner matching its operating system and CPU architecture. v* tag pushes and published release events SHALL create or update the GitHub Release for the same tag; manual dispatch SHALL only create workflow artifacts.

#### Scenario: Manual build

- **WHEN** desktop-release is manually dispatched
- **THEN** platform artifacts are uploaded without creating a GitHub Release

#### Scenario: ARM64 targets

- **WHEN** the desktop release matrix runs
- **THEN** Windows ARM64 uses windows-11-arm and Linux ARM64 uses ubuntu-24.04-arm
- **AND** each of the six RIDs has a distinct archive, checksum and workflow artifact name

#### Scenario: Release assets

- **WHEN** all six matrix jobs succeed for a v-prefixed tag push or published release event
- **THEN** the release job attaches Windows ZIPs, Linux/macOS TAR.GZ archives and their SHA-256 files to the release for that tag
- **AND** an unsuccessful matrix job prevents release asset publication

#### Scenario: Existing GitHub Release

- **WHEN** a v-prefixed release is published or its tag is pushed and the release already exists
- **THEN** all six desktop archives and checksums are uploaded to that release without replacing its title or description
- **AND** reruns replace matching asset filenames; tag and release runs for the same tag are serialized
- **WHEN** a tag push has no existing release
- **THEN** the workflow creates the release for that tag before uploading its assets

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
