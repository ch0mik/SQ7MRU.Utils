# ADIF parsing and utilities

## Purpose

Parse and export QSO data while respecting declared field lengths and record boundaries.

## Requirements

### Requirement: Explicit format reference and compatibility boundaries

Public XML comments and the package README SHALL link to the [ADIF 1.0 specification](https://www.adif.org/100/adif_100.htm), distinguish format rules from library policies and disclose parser/model/export limitations without claiming full schema conformance.

#### Scenario: Consumer checks compatibility

- **WHEN** a consumer reads the ADIF API documentation
- **THEN** the documentation identifies the source specification and limitations without claiming full conformance to every ADIF revision
- **AND** CALL filtering, trimming, case normalization and dropping unknown fields are identified as library policies
- **AND** the string-only API, lack of full schema validation and later-revision model fields are disclosed
- **AND** the exporter's ADIF_VER 3.0.6 tagged header is distinguished from the strict ADIF 1.0 header convention; protocol identifiers are data format versions, not product release tags

### Requirement: Length-aware parsing

AdifReader SHALL read declared field lengths, match tag names without case sensitivity, support records without a header and avoid splitting a field value at embedded ADIF markers.

#### Scenario: Embedded marker

- **WHEN** a COMMENT value contains an EOR-like substring within its declared length
- **THEN** the marker remains part of the field value and does not end the record

### Requirement: Malformed records and disposal

The reader SHALL reject invalid field lengths, unterminated tags and CALL-bearing records without EOR. It SHALL observe cancellation during parsing and reject use after disposal.

#### Scenario: Truncated value

- **WHEN** a declared length exceeds remaining input
- **THEN** the reader throws FormatException

### Requirement: Normalization and export

The utilities SHALL normalize callsigns to invariant uppercase and bands to invariant lowercase while allowing absent band values. Export SHALL write a header, nonempty string properties as length-prefixed fields and EOR for each row.

#### Scenario: Headerless record

- **WHEN** a valid CALL record ends with EOR without an EOH header
- **THEN** the reader returns the normalized callsign

## Implementation references

- [SQ7MRU.Utils/ADIF/AdifReader.cs](../../../SQ7MRU.Utils/ADIF/AdifReader.cs)
- [SQ7MRU.Utils/ADIF/AdifHelper.cs](../../../SQ7MRU.Utils/ADIF/AdifHelper.cs)
- [SQ7MRU.Utils/ADIF/AdifRow.cs](../../../SQ7MRU.Utils/ADIF/AdifRow.cs)

## Verification references

- `AdifAndStorageTests.ReadsCaseInsensitiveTagsWithoutSplittingFieldValues`
- `AdifAndStorageTests.RejectsTruncatedRecords`
