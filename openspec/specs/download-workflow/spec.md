# Asynchronous download workflow

## Purpose

Coordinate bounded discovery, download, progress and partial results without blocking on asynchronous I/O.

## Requirements

### Requirement: Bounded streaming work

The application SHALL consume discovery through Parallel.ForEachAsync with bounded concurrency and case-insensitive deduplication of relative paths. The default concurrency SHALL be two, with an eQSL override of one.

#### Scenario: Await all workers

- **WHEN** a worker is still fetching an item
- **THEN** the operation remains incomplete until worker cleanup finishes

### Requirement: Refresh exports and resume certificates

The application SHALL refresh ADIF exports and items explicitly marked RefreshExisting. Other items SHALL be skipped when the store validates the existing certificate or complete indexed bundle. EPC SHALL use this resume behavior for image and PDF certificates and ZIP bundles.

#### Scenario: Updated export

- **WHEN** an existing ADIF export is followed by a newly downloaded export containing another QSO
- **THEN** the new ADIF content replaces the old content

#### Scenario: Resume card retrieval

- **WHEN** a previously downloaded card is valid
- **THEN** the card is skipped without requesting its image again

#### Scenario: Resume an EPC bundle

- **WHEN** the SQLite index lists a complete valid local certificate set for an EPC selection
- **THEN** the selection is skipped without fetching its certificate response
- **AND** missing or invalid members cause the selection to be fetched again

#### Scenario: Bundle counts

- **WHEN** one discovered selection expands into several certificates
- **THEN** progress and results count that selection once, rather than counting extracted files

### Requirement: Partial failures

A non-authentication ServiceException for an item SHALL produce a failed ItemResult. A service failure while discovering later items SHALL produce a [discovery] result and allow previously discovered workers to finish. Authentication failures MUST abort the operation.

#### Scenario: Later discovery fails

- **WHEN** discovery yields a valid export and then throws a service-format error
- **THEN** the export is saved and the result contains the discovery failure

### Requirement: Cancellation and progress

The application SHALL propagate caller cancellation, await worker cleanup and dispose the session. Progress SHALL include processed counts and per-item outcomes; the final result SHALL be ordered by filename.

#### Scenario: Cancel in-flight work

- **WHEN** the caller cancels an active operation
- **THEN** the operation throws OperationCanceledException after worker cleanup and retains committed files

## Implementation references

- [SQ7MRU.Utils.Application/DownloadService.cs](../../../SQ7MRU.Utils.Application/DownloadService.cs)
- [SQ7MRU.Utils.Core/Contracts.cs](../../../SQ7MRU.Utils.Core/Contracts.cs)

## Verification references

- `ApplicationTests.WaitsForEveryDownloadAndReportsPartialFailure`
- `ApplicationTests.CancellationWaitsForWorkerCleanupAndDisposesSession`
- `ReviewRegressionTests.RefreshesExportAndKeepsEarlierValidItemsOnDiscoveryFailure`
- `ServiceIntegrationTests.EqslFallsBackFromSubmodeToModeAndSavesPng`
- `ServiceIntegrationTests.EpcParsesHiddenLoginFieldsAndAwardForm`
- `ImageArchiveTests.SavesEveryImageAndOnlySkipsCompleteGroups`
- `PdfStorageTests.SavesPdfWithCorrectExtensionAndRecognizesItOnResume`
