# Library architecture and dependency injection

## Purpose

Provide a standalone .NET 10 library with replaceable infrastructure ports and an independent desktop consumer.

## Requirements

### Requirement: Layer boundaries

The solution SHALL keep transport-independent contracts in Core, download orchestration in Application, and HTTP, parsing, storage and composition adapters in SQ7MRU.Utils. Core and Application MUST NOT depend on Avalonia.

#### Scenario: Independent library consumer

- **WHEN** a consumer installs SQ7MRU.Utils without the desktop project
- **THEN** the package includes the Core and Application assemblies and resolves its external dependencies without Avalonia

### Requirement: Service registration

AddSq7MruUtils SHALL register download, account-listing and upload services through constructor injection. The scheduler, clock and store SHALL be shared within the service provider. Replaceable ports SHALL use TryAdd registrations.

#### Scenario: Dependency graph

- **WHEN** the registered services are resolved from a validated service provider
- **THEN** IDownloadService, IEqslAccountService and IAdifUploader are available

#### Scenario: Invalid settings

- **WHEN** a consumer configures concurrency outside 1 through 32 or invalid HTTP delays
- **THEN** options validation rejects the settings when the options are accessed

### Requirement: Public operation contracts

The library SHALL expose Task-based download, eQSL account listing and ADIF upload APIs accepting CancellationToken. ServiceCredentials.ToString MUST NOT reveal credential values.

#### Scenario: Credential display

- **WHEN** a credentials object is converted to a string
- **THEN** the result is a redacted placeholder

## Implementation references

- [SQ7MRU.Utils.Core/Contracts.cs](../../../SQ7MRU.Utils.Core/Contracts.cs)
- [SQ7MRU.Utils.Application/DownloadService.cs](../../../SQ7MRU.Utils.Application/DownloadService.cs)
- [SQ7MRU.Utils/ServiceCollectionExtensions.cs](../../../SQ7MRU.Utils/ServiceCollectionExtensions.cs)
- [SQ7MRU.Utils/SQ7MRU.Utils.csproj](../../../SQ7MRU.Utils/SQ7MRU.Utils.csproj)

## Verification references

- `ApplicationTests.DependencyGraphResolvesAndRejectsInvalidOptions`
- `eng/verify-package.ps1`
