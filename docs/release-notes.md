# Release notes

## Linux ARM64 storage fix

- Corrected architecture-specific Linux directory and no-follow flags, resolving Invalid argument errors that prevented certificate storage and SQLite indexing on ARM64.
- Native flags are selected from the running process architecture, preserving directory confinement, no-follow behavior and close-on-exec handling.
- Added regression coverage for the x64 and ARM64 Linux ABI values, including checks that can run on a Windows test host.

## Cross-platform NuGet validation

- One platform-neutral net10.0 AnyCPU package supports Windows, Linux and macOS on x64 and ARM64.
- CI and NuGet release verification cover all six native OS/CPU targets.
- The release pipeline builds one candidate package, verifies that exact artifact on all six targets, and publishes it only after every verification job succeeds.
- Isolated consumers check all three shipped DLLs for CPU neutrality and exercise native SQLite through the public API.
- The verified candidate is published without rebuilding it. A failed target blocks publication.
- Each verification target has a distinct test-results artifact.

## Documentation and validation

- Updated README, OpenSpec and package-validation documentation.
- Release versions remain defined by GitHub tags, without concrete release-tag numbers in the repository.
- The latest local Windows test run passed 106 tests; Release compilation and isolated NuGet consumer verification also passed.
- All eight OpenSpec specifications passed strict validation.
- Successful native execution on all six targets must be confirmed by GitHub Actions; local Windows checks do not establish cross-platform success.
