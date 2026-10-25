# Release notes

## NuGet API documentation

- Added adjacent triple-slash XML comments to public methods, contracts, constructors and options, including parameters, returns, cancellation, failures and ownership/replay behavior where applicable.
- Converted 153 ADIF field descriptions to structured XML summaries and values, preserving supplemental enumeration notes.
- Linked the ADIF API documentation to the ADIF 1.0 specification and documented compatibility boundaries, including library normalization policies and the exporter's different header convention.
- The NuGet package includes a library-focused README and XML documentation beside the infrastructure, Core and Application DLLs for IDEs and agents.
- Missing public XML comments fail library builds. Package-consumer verification checks the README and XML API files before installing the package.

## Fixed

- Eliminated duplicate desktop workflow runs when a version tag is created and its GitHub Release is published.
- Tag pushes now trigger NuGet publishing only.
- Desktop publishing starts on the published release event and attaches assets to that existing release.
- The desktop workflow no longer creates releases or modifies their titles and descriptions.

## Publishing behavior

- Desktop builds retain the six x64/ARM64 targets and add Windows x86 and Linux ARM32, for eight desktop targets in total.
- Executables and SHA-256 checksums are uploaded after every desktop matrix job succeeds.
- Reruns replace matching asset filenames; desktop runs for the same tag remain serialized.
- Manual desktop runs produce workflow artifacts without publishing release assets.
- Release versions continue to come from GitHub tags, without concrete tag numbers in source or documentation.

## Documentation and validation

- Updated README, OpenSpec and release documentation to describe the separate NuGet and desktop triggers.
- Workflow YAML and mocked existing/missing-release upload scenarios passed local checks.
- All eight OpenSpec specifications passed strict validation.
- Existing workflow runs retain their original YAML; the corrected workflow must be committed before the next release tag is created.

## Single-file distributions and 32-bit targets

- Desktop release assets are individual self-contained executables with the .NET runtime included; a separately installed .NET runtime is not required.
- Desktop project defaults also enable self-contained single-file output for local publication with an explicit RID.
- Native package libraries are bundled for automatic extraction at startup. System GUI and C-library dependencies still apply.
- Managed symbols are embedded and external PDB files are omitted; the workflow rejects publish outputs containing more than the expected executable.
- Excluded library API XML sidecars from desktop publication so adding NuGet documentation does not break the single-executable output contract. XML documentation remains included in NuGet.
- Added `win-x86` and `linux-arm` desktop publications. The 32-bit outputs are cross-published from x64 Windows and ARM64 Linux hosts; source tests run on those host architectures.
- `linux-x86` is not published because the selected .NET SDK has no application host for it.
- Local publishing produced exactly one executable for Windows x64, Windows x86 and Linux ARM32. These checks validate output generation, not native execution of every target.

The latest local Windows test run passed 107 tests. Workflow YAML and all eight OpenSpec capabilities passed validation.
