# Desktop download experience

## Purpose

Expose the library in a separate English-language Avalonia MVVM application with account selection and live results.

## Requirements

### Requirement: Independent English desktop

SQ7MRU.Downloader SHALL use Avalonia and MVVM and present English labels and status messages. The application SHALL collect service credentials in memory and allow selection of a local output directory.

#### Scenario: Startup

- **WHEN** the desktop application starts
- **THEN** the form presents service, login, password, output-directory and eQSL account controls in English

### Requirement: Technical directory configuration

SQ7MRU.Downloader SHALL resolve technical storage configuration at startup through the library's options. SQ7MRU_TECHNICAL_DIRECTORY MAY override the default directory name without changing graphical-card paths.

#### Scenario: Technical directory override

- **WHEN** the process starts with SQ7MRU_TECHNICAL_DIRECTORY set to a valid name
- **THEN** the composition root configures ArtifactStorageOptions before resolving the view model and services
- **AND** changing this environment variable requires restarting the application to use the new value

### Requirement: Account selection before eQSL download

Starting eQSL without loaded accounts SHALL first load the account list and wait for the user to start again. Starting without a selected account SHALL NOT begin downloading.

#### Scenario: First download action

- **WHEN** the user starts eQSL with credentials but no loaded accounts
- **THEN** the list is populated and no download operation is invoked

### Requirement: Live newest-first results

Each live item result SHALL be inserted at the top of the result list and automatically scrolled into view. Completing the operation SHALL preserve live order and add any missing final results without duplicates.

#### Scenario: New result arrives

- **WHEN** the operation reports an item result
- **THEN** it appears before older entries and is scrolled into view

#### Scenario: EPC bundle result

- **WHEN** one EPC selection downloads several certificates from a ZIP
- **THEN** the result represents the selection and reports its group directory
- **AND** Downloaded and Processed counters count selections, not the number of physical certificate files

### Requirement: Cancellation and close behavior

The UI SHALL offer cancellation while busy and prevent starting another operation. Closing while busy SHALL request cancellation and keep the window open until the user closes it after completion.

#### Scenario: Close during download

- **WHEN** the user closes a busy window
- **THEN** the operation receives cancellation and the window remains open during cleanup

## Implementation references

- [SQ7MRU.Downloader/MainViewModel.cs](../../../SQ7MRU.Downloader/MainViewModel.cs)
- [SQ7MRU.Downloader/MainWindow.axaml](../../../SQ7MRU.Downloader/MainWindow.axaml)
- [SQ7MRU.Downloader/MainWindow.axaml.cs](../../../SQ7MRU.Downloader/MainWindow.axaml.cs)
- [SQ7MRU.Downloader/App.axaml.cs](../../../SQ7MRU.Downloader/App.axaml.cs)

## Verification references

- `DesktopTests.EqslFirstLoadsAccountsAndWaitsForUserBeforeDownloading`
- `DesktopTests.WindowRendersAndCommandsLoadAccountsAndDownload`
