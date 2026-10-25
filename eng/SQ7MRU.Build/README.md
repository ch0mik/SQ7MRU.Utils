# Repository build tools

This non-packable C#/.NET 10 console project provides repository tooling. It is not referenced by the published library or desktop application. Its regression tests run through `SQ7MRU.Utils.Tests` on the existing CI matrix.

## Final desktop checksums

```sh
dotnet run --project eng/SQ7MRU.Build -c Release -- checksums artifacts
```

The directory must contain eight platform packages and their SHA-256 sidecars. The tool verifies filenames, formats, a common version and actual hashes before atomically writing a sorted `SHA256SUMS`. Unexpected files/directories, links, missing platforms and corrupt payloads fail promotion. The command does not create archives or publish a release.

## Reviewed ADIF metadata

```sh
dotnet run --project eng/SQ7MRU.Build -c Release -- adif-metadata artifacts/adif-review/ADIF_318.htm artifacts/adif-review/adif-metadata.json
```

Supply the already reviewed official HTML locally. There is no network download or runtime dependency on the site. A different revision or unexpected field/band inventory fails before replacing output. Review the generated JSON diff before copying it to `SQ7MRU.Utils/ADIF/adif-metadata.json`.

Both commands use the .NET SDK; no additional scripting-language runtime is required.
