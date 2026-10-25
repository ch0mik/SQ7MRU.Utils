# Release notes

## Release automation

- Release versions are defined by GitHub tags, without hardcoded release numbers in source or documentation. Package metadata, the desktop manifest and HTTP User-Agent derive their versions from the build.
- Untagged builds use the SDK-evaluated version; package-consumer verification can discover the generated package version automatically.
- Desktop publishing now runs on `v*` tag pushes and published GitHub Releases. A separate `desktop-v*` tag is no longer required.
- NuGet publishing continues to run on `v*` tag pushes using Trusted Publishing.
- Desktop archives and SHA-256 checksums are attached to the release for the same version tag.
- Existing release titles and descriptions are preserved. Missing releases are created automatically after all desktop builds succeed.
- Reruns replace matching desktop asset filenames. Desktop runs for the same tag are serialized to avoid concurrent uploads.
- Manual workflow runs continue to produce workflow artifacts without publishing a release.

## Desktop platforms

Self-contained desktop archives are configured for six targets, each built and tested on a native runner:

- Windows: `win-x64`, `win-arm64`.
- Linux: `linux-x64`, `linux-arm64`.
- macOS: `osx-x64`, `osx-arm64`.

Windows distributions use ZIP archives; Linux and macOS distributions use TAR.GZ archives. Every archive has a SHA-256 checksum. macOS distributions remain unsigned.

## Download fixes and improvements

- eQSL card-generation spacing starts at 1 second and increases by 500 ms after each recognized rate-limit response. Learned spacing is shared across sessions until the application restarts; server `Retry-After` instructions remain respected.
- eQSL rate-limit messages returned with HTTP 403 are handled as rate limits instead of authentication failures.
- Service waiting times in the UI display fractional seconds.
- Corrected native file-creation mode passing on macOS ARM64 for writable certificates and reusable SQLite index locks.

## Documentation and validation

- Updated README, OpenSpec and release documentation for the six-platform release matrix and shared version tags.
- The latest local Windows test run passed 104 tests. All eight OpenSpec specifications passed strict validation.
- Workflow YAML and mocked publication scenarios were checked locally, including existing releases, release creation, creation races, failures and prereleases.
- Native execution on all six targets must be confirmed by GitHub Actions; local checks do not establish cross-platform success.
