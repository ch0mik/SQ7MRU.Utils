# Public repository hygiene

## Versioned material

Commit source projects, synthetic tests and fixtures, GitHub Actions workflows, the package smoke-test script, SDK/build configuration, the license, README and OpenSpec specifications. `SampleApp` is an executable consumer example, not a disposable build artifact.

## Local-only material

The `.gitignore` excludes build output and `artifacts/`; local release archives, macOS DMG images and generated app bundles; real ADIF/ADI logs; card downloads; environment overrides and secret files; cookie jars; local service configuration; private certificates/keys; HTTP captures, dumps and diagnostic output; local AI/cloud configuration directories; editor and OS caches. Release ZIP/TAR.GZ/DMG containers are generated only by GitHub Actions; packaging scripts and source artwork belong in Git.

The download index `<output>/.sq7mru/downloads.sqlite`, its `downloads.lock` coordination file, and legacy JSON bundle indexes are also local-only. SQLite records relative bundle paths and certificate filenames, which can include callsigns; it does not store passwords or cookies.

Real ADIF logs can contain callsigns, locations, timestamps and contact history. Cookie files and HTTP captures can contain authenticated sessions or passwords. Do not attach those files to public issues or add them as test fixtures. Use synthetic data and placeholders instead. Tests embed small generated image fixtures and generate synthetic PDFs in C# source. Common downloaded bitmap and PDF extensions are ignored by default; source artwork under `SQ7MRU.Downloader/Assets/`, synthetic fixtures under `SQ7MRU.Utils.Tests/Fixtures/` and documentation PDFs under `docs/` have explicit exceptions. Those exceptions do not authorize publishing real certificates.

Synthetic standalone ADIF fixtures may be added under `SQ7MRU.Utils.Tests/Fixtures/`; that narrow exception does not authorize real account exports. Review such fixtures before committing. Example environment files named `.env.example` or `.env.sample` may be committed only with placeholders.

## Before committing

Keep release notes and specifications independent of concrete release-tag numbers. Define the release version on GitHub; do not update a hardcoded project or workflow version for each release. Dependency, SDK and protocol versions serve separate purposes and remain explicitly configured where required.

```sh
git status --short --untracked-files=all
git diff --cached --check
git diff --cached
git ls-files -ci --exclude-standard
```

The last command lists files already tracked despite matching ignore rules. `.gitignore` prevents normal staging of untracked files; it does not untrack existing files, inspect their contents, block `git add -f`, or remove secrets from history. If an actual credential was previously published, revoke/rotate it and address the affected history separately.

To investigate an ignore rule without creating sensitive data:

```sh
git check-ignore -v --no-index .env.local cookies.txt downloads/account.adif artifacts/tests/results.trx
```

Keep NuGet Trusted Publishing configuration outside source: `NUGET_USER` is a GitHub variable in the `nuget` environment or at repository scope, and the OIDC trust policy is configured in nuget.org. No API key or session-cookie value belongs in YAML or OpenSpec examples.

## Disposable files

`artifacts/` is reserved for regenerable local output. Diagnostic HTML, copied legacy source, one-off review reproductions, downloaded verification tools, temporary NuGet consumers and old desktop ZIPs are not product source. They can be removed after verification. Build output under `bin/` and `obj/` is also regenerable, but cleanup must not interrupt a running application or test process.
