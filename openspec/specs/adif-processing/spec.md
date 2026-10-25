# ADIF parsing and utilities

## Purpose

Parse and export QSO data while respecting declared field lengths and record boundaries.

## Requirements

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
