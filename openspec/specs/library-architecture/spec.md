# Library architecture and dependency injection

## Purpose

Provide a standalone .NET 10 library with replaceable infrastructure ports and an independent desktop consumer.

## Requirements

### Requirement: Layer boundaries

The solution SHALL keep transport-independent contracts in Core, download orchestration in Application, and HTTP, parsing, storage and composition adapters in SQ7MRU.Utils. Core and Application MUST NOT depend on Avalonia.

#### Scenario: Independent library consumer

- **WHEN** a consumer installs SQ7MRU.Utils without the desktop project
- **THEN** the package includes the Core and Application assemblies and resolves its external dependencies without Avalonia

#### Scenario: Portable package consumer

- **WHEN** a supported Windows, Linux or macOS consumer on x64 or ARM64 installs the library
- **THEN** the same net10.0 package supplies AnyCPU library assemblies and NuGet resolves the platform-specific native SQLite assets through its dependencies
- **AND** the library package does not require a desktop executable or a per-RID package variant

### Requirement: Service registration

AddSq7MruUtils SHALL register download, account-listing and upload services through constructor injection. The scheduler, clock and store SHALL be shared within the service provider. Replaceable ports SHALL use TryAdd registrations.

#### Scenario: Dependency graph

- **WHEN** the registered services are resolved from a validated service provider
- **THEN** IDownloadService, IEqslAccountService and IAdifUploader are available

#### Scenario: Invalid settings

- **WHEN** a consumer configures concurrency outside 1 through 32 or invalid HTTP delays
- **THEN** options validation rejects the settings when the options are accessed

#### Scenario: Shared storage configuration

- **WHEN** a consumer configures ArtifactStorageOptions through DI before resolving services
- **THEN** eQSL discovery and the artifact store use the same validated TechnicalDirectoryName for ADIF exports, SQLite and its lock
- **AND** the library remains independent of desktop environment-variable handling

### Requirement: Public operation contracts

The library SHALL expose Task-based download, eQSL account listing and ADIF upload APIs accepting CancellationToken. ServiceCredentials.ToString MUST NOT reveal credential values.

#### Scenario: Credential display

- **WHEN** a credentials object is converted to a string
- **THEN** the result is a redacted placeholder

### Requirement: Public XML API comments

Handwritten public API declarations in Core, Application and the library SHALL carry XML comments directly in source. Public methods SHALL describe behavior, parameters, results where applicable, cancellation, failures and relevant ownership/replay rules. Missing public comments MUST fail the library build. ADIF field descriptions SHALL state their raw string representation and validation limits.

#### Scenario: Public method added

- **WHEN** a handwritten public declaration lacks an XML documentation comment
- **THEN** compiler diagnostic CS1591 fails that library project build

#### Scenario: Agent consumes API descriptions

- **WHEN** an IDE or agent reads the installed package documentation
- **THEN** summaries and method contracts are available in the XML files generated from the adjacent source comments
- **AND** internal generated helpers are not treated as public API contracts

## Implementation references

- [SQ7MRU.Utils.Core/Contracts.cs](../../../SQ7MRU.Utils.Core/Contracts.cs)
- [SQ7MRU.Utils.Application/DownloadService.cs](../../../SQ7MRU.Utils.Application/DownloadService.cs)
- [SQ7MRU.Utils/ServiceCollectionExtensions.cs](../../../SQ7MRU.Utils/ServiceCollectionExtensions.cs)
- [SQ7MRU.Utils/Infrastructure/ArtifactStorageOptions.cs](../../../SQ7MRU.Utils/Infrastructure/ArtifactStorageOptions.cs)
- [SQ7MRU.Utils/SQ7MRU.Utils.csproj](../../../SQ7MRU.Utils/SQ7MRU.Utils.csproj)

## Verification references

- `ApplicationTests.DependencyGraphResolvesAndRejectsInvalidOptions`
- `ArtifactStorageOptionsTests.RejectsUnsafeNamesBeforeResolvingStorage`
- `ServiceIntegrationTests.EqslSeparatesAccountsAndDownloadsAdifAndImages`
- `eng/verify-package.ps1`
