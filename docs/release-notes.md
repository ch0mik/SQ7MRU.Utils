# Release notes

## ADIF support and backward compatibility

- Add all 181 QSO fields from the reviewed ADIF specification, with updated XML API descriptions.
- Introduce field-preserving document import/export and separate standard and eQSL compatibility profiles.
- Correct ADI header framing, invariant Gregorian UTC timestamps, export eligibility and band mappings.
- Add bounded parsing, cancellation and supported type, range, enumeration and relationship checks.
- Preserve older ADIF imports and existing eQSL behavior. Tests cover ADIF 1.0, 2.2.7, 3.0.6 and the reviewed current revision.
- Keep original payload bytes and version declarations when uploading to eQSL.
- Reject ineligible data in standard export instead of silently discarding it; older/provider output requires explicit compatibility selection.
- Check official ADIF releases before future implementation, review and release preparation.

## C# build tooling

- Replace Python checksum and metadata scripts with the non-packable SQ7MRU.Build C#/.NET project.
- Generate and verify SHA256SUMS through dotnet in GitHub Actions.
- Move checksum regression tests into xUnit and add metadata extraction/inventory tests.
- Verify that the C# generator reproduces the reviewed ADIF metadata without semantic changes.

## Linux documentation

- Document the specific Ubuntu 24.04 GUI and native runtime dependency packages.
- Clarify that Linux builds include .NET but still require system libraries and an X11/XWayland graphical session.
- Link official Avalonia and Microsoft requirements and explain distribution-specific package differences.

## Validation and scope

- 206 .NET tests passed locally on Windows.
- Installed NuGet consumption, OpenSpec validation and single-file Windows x64 publication passed.
- Workflow YAML and the C# metadata generator were checked locally.
- ADX remains optional and unsupported. The standard profile is not an exhaustive external-registry, historical DXCC or geographic-adjacency validator.
- Live eQSL account compatibility and remote platform execution still require verification in their respective environments.
