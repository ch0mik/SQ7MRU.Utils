# Validated and confined artifact storage

## Purpose

Persist private QSL downloads through validated content and directory-safe atomic replacement.

## Requirements

### Requirement: Directory confinement

The store SHALL normalize the output root, accept a trailing separator and reject lexical path traversal. It SHALL reject untrusted symbolic links and junctions in directory traversal. Windows SHALL hold directory handles against rename; Unix SHALL use no-follow directory-relative operations.

#### Scenario: Linked output subdirectory

- **WHEN** an output subdirectory is a junction or symbolic link to another directory
- **THEN** reading and writing through it fail without modifying the target

#### Scenario: Directory replacement race

- **WHEN** another operation attempts to redirect a held directory during a write
- **THEN** the write cannot be redirected into the replacement target

#### Scenario: Trailing separator

- **WHEN** a valid output directory ends in a path separator
- **THEN** the store accepts the directory

#### Scenario: macOS ARM64 file creation

- **WHEN** native openat creates a temporary certificate or index lock on macOS ARM64
- **THEN** the mode argument is passed in its Darwin variadic stack slot, producing owner-only read/write permissions subject to the process umask
- **AND** the committed certificate can be reopened for writing and the persistent index lock can be acquired again after release

#### Scenario: Linux ARM64 directory flags

- **WHEN** directory confinement runs on Linux ARM64
- **THEN** native open calls use the ARM64 O_DIRECTORY and O_NOFOLLOW bits, while preserving O_CLOEXEC
- **AND** the flags follow the running process architecture, independently of the machine that built the portable library
- **AND** x64 flag values are not reused as ARM64 O_DIRECT or O_LARGEFILE flags
- **AND** the same architecture-specific no-follow flag is used for directory traversal, certificate reads, temporary writes and SQLite locks

### Requirement: Image validation limits

The store SHALL identify JPEG, PNG and GIF and decode the image before accepting it. It MUST reject encoded inputs larger than 32 MiB, dimensions above 8192 on either axis, and decoded areas above four million pixels. Saved extensions SHALL match the detected format.

#### Scenario: Invalid JPEG signature

- **WHEN** a file contains only the JPEG start and end markers
- **THEN** the store rejects it and does not skip it as a valid existing card

#### Scenario: PNG response

- **WHEN** a valid PNG is fetched for an item initially named with a JPG extension
- **THEN** the store saves a PNG and recognizes it when resuming

### Requirement: Atomic file replacement

The store SHALL create a unique temporary file, write it asynchronously, check cancellation and atomically replace the destination. Temporary files SHALL be removed when the operation completes or fails.

#### Scenario: Cancelled write

- **WHEN** cancellation occurs before commit
- **THEN** a partially written destination is not committed

### Requirement: ADIF storage validation

ADIF acceptance SHALL require an EOH or EOR marker and reject HTML content. Record-level parsing SHALL remain the responsibility of the ADIF reader.

#### Scenario: HTML response

- **WHEN** a service returns an HTML page instead of ADIF or an image
- **THEN** the store refuses to persist it as the expected artifact

### Requirement: EPC certificate archives

The store SHALL accept ZIPs only for opted-in items, with 1–128 entries, a 32 MiB archive/per-entry limit and a 64 MiB expanded limit. Entries MUST be flat regular files with safe unique names and validated JPEG, PNG or GIF payloads, or structurally validated PDFs when permitted. Unsafe paths, symbolic links and duplicate output names MUST be rejected before writing.

#### Scenario: Multiple certificates in one response

- **WHEN** an EPC award response contains a valid ZIP with multiple image or opted-in PDF certificates
- **THEN** every certificate is stored directly in the EPC group directory, alongside individual certificates, retaining its filename and using the detected format extension
- **AND** the download result counts the selected award as one item and reports its directory

#### Scenario: Resume a certificate group

- **WHEN** all certificates listed for the bundle in the SQLite index exist and pass validation for their detected format
- **THEN** the store reports the complete group as valid for callers that permit skipping
- **AND** EPC download operations skip valid local certificates and complete indexed bundles without requesting their payload again
- **AND** a missing certificate or missing bundle entry requires downloading the bundle again

#### Scenario: Technical metadata is separate from certificates

- **WHEN** a certificate bundle is saved or a complete legacy bundle is checked
- **THEN** its certificate filenames are recorded transactionally in `<output>/.sq7mru/downloads.sqlite`, leaving the group directory for certificates
- **AND** JSON indexes from either previous layout are removed only after the complete validated image set has a committed database entry

#### Scenario: Concurrent index updates

- **WHEN** multiple sessions or processes update different bundle entries
- **THEN** an operating-system file lock serializes the bounded read, SQLite transaction and atomic replacement without losing prior entries
- **AND** waiting for the lock supports cancellation and a 30-second timeout

#### Scenario: Invalid database

- **WHEN** the database is malformed or exceeds 16 MiB
- **THEN** a storage error is reported without overwriting the original database
- **AND** no credentials or cookies are recorded in the database

#### Scenario: Interrupted archive persistence

- **WHEN** cancellation or a storage failure interrupts writing the certificates
- **THEN** the database entry is not committed before the certificates; each file uses atomic replacement
- **AND** already committed certificate files may remain available for the next run

### Requirement: Opt-in PDF certificates

The store SHALL accept PDFs only for opted-in items and save them with a PDF extension. It MUST enforce a 32 MiB limit and check the version header, EOF, startxref offset and trailer or XRef stream framing. Existing files and opted-in archive entries SHALL pass the same checks. These checks do not perform full parsing, rendering or active-content sanitization.

#### Scenario: Membership PDF

- **WHEN** EPC returns a structurally complete membership PDF for an item initially named with a JPG extension
- **THEN** the store saves a PDF and recognizes it on the next run
- **AND** a truncated PDF or HTML response is rejected

## Implementation references

- [SQ7MRU.Utils/Infrastructure/ArtifactStore.cs](../../../SQ7MRU.Utils/Infrastructure/ArtifactStore.cs)
- [SQ7MRU.Utils/Infrastructure/ImageArchiveStore.cs](../../../SQ7MRU.Utils/Infrastructure/ImageArchiveStore.cs)
- [SQ7MRU.Utils/Infrastructure/ArchiveIndex.cs](../../../SQ7MRU.Utils/Infrastructure/ArchiveIndex.cs)
- [SQ7MRU.Utils/Infrastructure/SecureArtifactDirectory.cs](../../../SQ7MRU.Utils/Infrastructure/SecureArtifactDirectory.cs)
- [SQ7MRU.Utils/Infrastructure/CardImageValidator.cs](../../../SQ7MRU.Utils/Infrastructure/CardImageValidator.cs)
- [SQ7MRU.Utils/Infrastructure/PdfCertificateValidator.cs](../../../SQ7MRU.Utils/Infrastructure/PdfCertificateValidator.cs)

## Verification references

- `ImageArchiveTests`
- `PdfStorageTests`

- `AdifAndStorageTests.AcceptsOutputDirectoryWithTrailingSeparator`
- `AdifAndStorageTests.RejectsDirectoryLinksBeforeReadingOrWriting`
- `AdifAndStorageTests.StoresNonJpegCardsWithCorrectExtensionAndFindsThemOnResume`
- `ReviewRegressionTests.HeldDirectoryCannotBeRedirectedDuringAWrite`
- `ReviewRegressionTests.UnixCreatedFilesAndReopenedIndexLocksHavePrivateWritablePermissions`
- `ReviewRegressionTests.LinuxDirectoryFlagsFollowTheTargetKernelAbi`
- `ReviewRegressionTests.FourByteJpegIsRejectedAndNotAcceptedAsExistingCard`
