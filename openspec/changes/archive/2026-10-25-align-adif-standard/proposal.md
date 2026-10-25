# Proposal

## Why

The ADIF audit found ADI export framing/eligibility defects, stale descriptions and incomplete field coverage. The project needs a declared, tested interchange profile based on the official released specification instead of inferring conformance from an ADIF_VER string or historical references.

## What Changes

- Use the reviewed official ADIF 3.1.8 release as the initial protocol target and record future upgrades explicitly; these identifiers are not product release tags.
- Recheck official releases before ADIF work and release preparation, recording available versus implemented support and planning verified upgrades.
- Keep latest-standard support separate from tested provider compatibility profiles, including possibly older eQSL import/upload dialects.
- Correct ADI header framing and format/deprecation-aware export eligibility first.
- Add metadata-backed field documentation, meaningful validation and reviewed band data.
- Introduce a document representation preserving header, values and extensions; retain an explicit legacy QSO projection for service consumers.
- Plan coverage for the 28 absent QSO fields, while documenting that full field coverage is a project goal rather than a standard requirement.
- Keep ADX outside the first ADI milestone; it remains optional and must not be advertised as implemented.
- **BREAKING:** Strict rejection of invalid export data and changed lossless/normalization behavior require an explicit compatibility API/migration decision before implementation.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `adif-processing`: authoritative reference governance, conformant ADI export, document preservation, field metadata, validation and explicit ADX scope.

## Impact

Affected areas include AdifReader, AdifHelper, AdifRow, upload validation, eQSL's QSO projection, public XML documentation, package guidance and synthetic/official-format tests. This proposal does not change runtime behavior. See [the evidence and inventory](../../../../docs/adif-conformance-review.md).
