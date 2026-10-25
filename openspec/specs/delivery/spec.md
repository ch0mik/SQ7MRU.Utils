# Build, package and release delivery

## Purpose

Deliver a standalone NuGet library and separate desktop binaries through reproducible GitHub Actions workflows.

## Requirements

### Requirement: Multi-platform validation

CI SHALL restore, build and test on Windows, Linux and macOS and install the generated NuGet package into an isolated consumer. Tests SHALL use controlled HTTP responses rather than real service credentials.

#### Scenario: NuGet consumption

- **WHEN** the generated SQ7MRU.Utils package is restored into the smoke-test project
- **THEN** the consumer resolves public services and runs cancellation and ADIF checks
- **AND** ZIP certificate storage and the native SQLite dependency execute through the public package API

### Requirement: NuGet publishing

A valid v-prefixed SemVer tag SHALL build and verify the package before publishing via nuget.org Trusted Publishing. The publish job SHALL use the nuget environment and an OIDC-issued temporary key; NUGET_USER SHALL be configured externally.

#### Scenario: Package release

- **WHEN** a valid library version tag is pushed and registry trust is configured
- **THEN** only the verified SQ7MRU.Utils package is pushed to nuget.org

### Requirement: Desktop publishing

GitHub Actions SHALL publish self-contained desktop archives and SHA-256 files for win-x64, linux-x64, osx-x64 and osx-arm64. desktop-v tags SHALL create a GitHub Release; manual dispatch SHALL only create workflow artifacts.

#### Scenario: Manual build

- **WHEN** desktop-release is manually dispatched
- **THEN** platform artifacts are uploaded without creating a GitHub Release

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
