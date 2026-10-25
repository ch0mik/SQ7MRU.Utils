# EPC downloads

## Discovery and authentication

EPC uses one cookie-isolated HTTP session for login, category discovery and certificate requests. The callsign is trimmed and converted to uppercase; the password is sent unchanged. Login uses the form action and hidden fields. The authenticated Joomla logout form can retain the `login-form` ID, so the ID alone is not evidence of failed authentication.

The certificate accordion is located through the membership entry (`item-133`, with the membership article URL as a fallback). Its same-host Joomla article links are enumerated dynamically. New categories do not require extending a category-ID list. Labels may be translated and menu order may change. Other accordion menus, including account settings and password changes, are excluded. If the structured menu cannot be identified, discovery reports a format error rather than scanning all menus.

All matching certificate forms on each category page are inspected, including nested option groups and named submit buttons. Hidden fields and option values are preserved. Forms with interactive password fields, `com_users`, or `user.*` tasks are not submitted as certificate downloads. A form with multiple award selectors is reported as ambiguous rather than submitted with guessed combinations.

Informational pages without certificate forms are ignored. A category request failure is reported for that category and the remaining categories continue. Failed initial authentication and authentication rejection while fetching a certificate still stop the operation.

## Saved files and progress

Files are written below the selected output directory:

```text
<output>/
  EPC/
    <login>/
      <group>/
        <selected-award>.jpg
        <certificate-name-from-archive>.jpg
      Membership certificate/
        Membership certificate.pdf
  .sq7mru/
    downloads.sqlite
    downloads.lock
```

Group names come from the website menu. Image extensions follow detected content: JPEG, PNG or GIF. A membership certificate returned as PDF is saved with `.pdf`, even if its logical download item initially ends in `.jpg`.

EPC can return a ZIP when JPG was selected. Every validated certificate is unpacked directly into its group directory, retaining the archive filename and using the detected extension. No ZIP or additional award subdirectory is saved.

**Progress counts download selections, not physical files.** One selection may produce one certificate or a ZIP containing several certificates. A bundle's successful result reports the group directory. Its certificate filenames are recorded in SQLite. A skipped item can be reported under its original logical `.jpg` name even when the saved file is PNG, GIF or PDF.

## Resume behavior

A valid existing image or PDF is skipped without another certificate-generation request. A bundle is skipped only when its SQLite entry is present and every listed certificate passes validation. A missing or invalid certificate invalidates the bundle; its complete response is fetched and saved again. Login and menu/form discovery still run to determine available selections.

This behavior deliberately does not poll an already complete bundle for new remote contents. New selections in the menu are discovered, but a server-side addition inside an unchanged completed selection is not automatically detected. Removing one local bundle member invalidates the bundle and causes the next run to fetch it again.

## Technical index

The single index `<output>/.sq7mru/downloads.sqlite` stores logical bundle paths and certificate filenames, without passwords or cookies. It can still contain callsigns and belongs outside public version control.

SQLite works on an in-memory database on a worker thread. Standard SQLite snapshots are read and written through confined filesystem operations and atomic replacement. A persistent operating-system lock file serializes updates across sessions and processes; waiting supports cancellation and a 30-second timeout. The database is limited to 16 MiB. Invalid or oversized databases produce a storage error rather than being overwritten.

Legacy JSON indexes in either the group directory or its `.sq7mru` subdirectory are migrated only after validating the complete certificate set. They are removed only after a committed SQLite entry exists. Interrupted writes can leave already committed certificates, which remain available on the next run.

## Content validation

Images are decoded before acceptance, with a 32 MiB encoded limit, an 8192-pixel limit per axis and a four-million-pixel area limit. Opted-in PDFs have a 32 MiB limit and structural checks for the version header, EOF, startxref and trailer or XRef stream. PDF validation does not render the file or sanitize active content.

Archives allow 1–128 flat regular certificate entries, at most 32 MiB for the ZIP or any entry and 64 MiB expanded in total. Traversal paths, links, unsafe filenames, duplicate output names and invalid certificate payloads are rejected before writing any certificates. Individual files use atomic replacement; the bundle index is committed after its files. On Unix, created files use owner-only read/write permissions subject to umask. The macOS ARM64 interop explicitly passes the variadic file mode on the stack, as required by the Darwin ABI.

## Verification

Linux directory confinement selects `O_DIRECTORY` and `O_NOFOLLOW` from the running process architecture. Linux ARM64 uses different bits from x64; reusing x64 constants would request direct I/O instead of a directory and can fail before any certificate is saved. The selected no-follow flag applies consistently to traversal, reads, writes and index locks.

Synthetic regression coverage is in `EpcProtocolTests`, `ServiceIntegrationTests.EpcParsesHiddenLoginFieldsAndAwardForm`, `ServiceIntegrationTests.EpcContinuesPastUnavailableAndInformationalCategories`, `ImageArchiveTests` and `PdfStorageTests`. Package-consumer validation also exercises ZIP storage and its native SQLite dependency through the public API.

These tests verify controlled cases rather than every live account or remote page layout. Use synthetic data for additional fixtures; do not commit real certificates, archives, database files, cookies or authenticated HTML.
