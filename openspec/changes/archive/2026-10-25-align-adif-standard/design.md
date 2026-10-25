# Design

## Context

The current reader is a service-oriented, lossy projection rather than a general ADIF document API. Reflection-only export has no format/type/deprecation metadata. The [review](../../../../docs/adif-conformance-review.md) records the exact source gaps and current behavior.

## Goals and non-goals

- First deliver a tested ADI profile against the reviewed official release, preserving existing downloader behavior through an explicit adapter.
- Separate a wire document, optional typed QSO projection and per-service requirements. Do not turn SQLite or the UI into an ADIF schema.
- Complete the reviewed field inventory as a project goal without claiming that the standard requires every field.
- ADX implementation, service protocol rewrites and unrelated storage changes are outside the first milestone.

## Decisions

1. Keep versioned metadata for field names, types, export eligibility, relationships and deprecation; use it for validators and documentation checks. Review official machine-readable resources and redistribution conditions before vendoring data. Do not fetch a changing latest specification at application runtime.
2. Add a document API preserving header fields, declared type indicators, exact values and extension definitions. The existing GetAdifRows behavior stays a documented compatibility projection until callers explicitly migrate.
3. Validate ADI header/record grammar and format-specific character rules. A strict export fails with field-specific diagnostics when data cannot be represented; it must not silently discard international or unknown values. Import-only compatibility may normalize to supported replacements only through documented mappings.
4. Keep cancellation, size bounds and no-network parsing. Build the document before returning success; reject incomplete records regardless of CALL. Select a duplicate-field policy from the specification and document stricter project choices separately.
5. Choose a supported protocol profile explicitly. ADIF_VER reflects validated output, not a package release version or an unverified latest-version label.
6. If ADX follows later, use bounded XML parsing with DTD processing prohibited and external resolution disabled, official schema checks and UTF-8/international test cases. Do not reuse ADI restrictions blindly for ADX.

## Compatibility and migration

Check the official release index and latest-release redirect before implementation/review and during release preparation. Record the date, URL, released revision, implemented revision and remaining gaps. A newer standard is an upgrade target with metadata and test work, not an automatic version-string update or a runtime network dependency.

Maintain separate standard and provider compatibility profiles. eQSL may emit or accept an older dialect; its actual version support is not established by this review. Provider behavior must be derived from verified responses and sanitized synthetic regression fixtures. Supporting a newer standard must not force that format on eQSL uploads or remove necessary older import compatibility. Unrepresentable conversions require diagnostics or explicit mappings.

Changing exports may affect existing consumers that accepted malformed headers or deprecated fields. Keep compatibility projection and strict document APIs distinct, document migration and test eQSL/upload paths. No real account logs, credentials or live service requests are required by this work.

## Validation

Use independent golden files in addition to reader/writer round trips: two components sharing the same defect must not establish correctness. Cover all supported field metadata, import-only handling, extension/header retention, zero-length values, meaningful whitespace, malformed syntax, dates/numbers/enumerations, band boundaries and culture invariance. Update XML comments and installed-package checks together. ADX must remain explicitly unsupported until its own tests pass.
