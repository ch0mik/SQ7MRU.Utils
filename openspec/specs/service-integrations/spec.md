# Service integration protocols

## Purpose

Document the supported eQSL, HRDLog and EPC adapter behavior, including legacy authentication compatibility.

## Requirements

### Requirement: eQSL login and accounts

The eQSL adapter SHALL authenticate the primary account before switching a linked account in the same cookie session. It SHALL preserve the legacy literal SelectCallsign value Log+In. Account selection SHALL examine the response after the third selection before reporting exhaustion.

#### Scenario: Third selection succeeds

- **WHEN** the third selection POST returns an authenticated page
- **THEN** account listing continues instead of reporting an authentication failure

#### Scenario: Unrelated form

- **WHEN** MyAccounts returns an unrelated hidden HamID without recognized account content
- **THEN** the adapter does not accept that field alone as an authenticated account list

### Requirement: eQSL downloads

The adapter SHALL export ADIF for selected accounts, preserve complete ADIF hyperlink paths and query parameters, and fetch graphical cards using QSO identity. When SUBMODE lookup explicitly reports no entry, it SHALL try MODE if different.

#### Scenario: Alternative mode

- **WHEN** the SUBMODE lookup has no QSO but the MODE lookup returns a card
- **THEN** the card is saved using its detected image format

#### Scenario: Absolute export link

- **WHEN** the export page contains a same-host root-relative or absolute download link
- **THEN** the adapter retains its directory and query components

### Requirement: Confirmed ADIF upload

The uploader MUST NOT automatically replay upload requests. A response SHALL count as success only when visible content has an explicit RESULT=OK marker or a positive added-record count and no recognized failure marker. Scripts, styles and comments SHALL NOT provide success evidence.

#### Scenario: Negative success substring

- **WHEN** the server responds Upload was unsuccessful
- **THEN** the API reports an unconfirmed outcome rather than success

#### Scenario: Positive marker

- **WHEN** the server returns RESULT=OK without a failure marker
- **THEN** the upload completes successfully

### Requirement: HRDLog and EPC adapters

HRDLog SHALL perform a read-only callsign search and retrieve the returned card identifiers. EPC SHALL submit login hidden fields and discover award-generation forms. DMC SHALL NOT be registered or exposed.

#### Scenario: Dynamic multilingual EPC navigation

- **WHEN** the authenticated menu contains same-host Joomla article links, including newly added category IDs
- **THEN** the adapter discovers them without a fixed category list, translated-label matching or positional boundaries
- **AND** on structured Joomla menus it locates the certificate list using the membership entry (`item-133`, with the membership article URL as a fallback) and enumerates that list only
- **AND** other accordion lists, such as account settings and password changes, are excluded; an unrecognized structured menu is not replaced with a scan of all menus
- **AND** membership and contest certificate pages are included; duplicate article IDs, external links and task links are excluded
- **AND** informational articles without download forms are ignored

#### Scenario: Multiple certificate forms

- **WHEN** a category contains several certificate forms, nested option groups or a named submit button
- **THEN** the adapter inspects every matching form and preserves its hidden fields, option values and submit value
- **AND** a service failure in one category is reported for that category while subsequent categories continue

#### Scenario: Login module or restricted menu article

- **WHEN** a discovered article retains an embedded password form beside usable certificate forms
- **THEN** its usable certificate forms are processed instead of rejecting the whole session
- **WHEN** an article returns a login form without certificate forms, or HTTP 401/403
- **THEN** password-only articles without certificate forms are ignored as non-download pages and HTTP 401/403 articles are reported as unavailable without stopping other categories
- **AND** account-management forms with com_users or user.* tasks are never submitted as certificate downloads
- **AND** if no certificate forms are found anywhere and password-only articles are present, a possible expired-session error is reported
- **AND** credential rejection at initial login and authentication rejection while fetching a certificate remain fatal authentication errors

#### Scenario: Certificate endpoint returns a document or page

- **WHEN** a certificate endpoint returns a structurally complete PDF rather than an image
- **THEN** the opted-in EPC item is saved with a PDF extension and can be recognized on subsequent operations
- **WHEN** it returns recognizable HTML instead of a certificate
- **THEN** the adapter reports its response kind and endpoint path without exposing query parameters or response content, and does not persist HTML as a certificate

#### Scenario: Previously downloaded certificates

- **WHEN** a subsequent EPC operation finds a previously downloaded award selection
- **THEN** a valid existing image or PDF, or a complete bundle verified through the SQLite index, is skipped without another certificate-generation request
- **AND** missing or corrupt files are downloaded again
- **AND** additional remote contents in an already complete bundle are not detected automatically; a missing local bundle member invalidates the bundle and causes a new request

#### Scenario: Duplicate HRDLog link

- **WHEN** a search returns the same card link twice
- **THEN** only one card is processed

#### Scenario: EPC award form

- **WHEN** an authenticated award form provides hidden fields and a JPG format selector
- **THEN** the adapter submits those fields and saves the returned card

#### Scenario: EPC ZIP response

- **WHEN** an award-generation form returns a ZIP containing multiple certificate images despite the JPG format selection
- **THEN** the EPC item permits bounded image archive processing and all images are saved directly in the EPC group directory alongside individual certificates

#### Scenario: EPC Joomla login and logout forms

- **WHEN** EPC returns a logout form using the login-form identifier after authentication
- **THEN** the adapter accepts the authenticated award navigation instead of treating the identifier as a failed login
- **AND** an interactive password field or explicit credential rejection in the initial login response still causes an authentication error

#### Scenario: EPC session continuity

- **WHEN** EPC login is submitted
- **THEN** the adapter uses the form action, preserves hidden fields, normalizes the callsign to uppercase without changing the password, and retains cookies across redirects and subsequent requests

## Implementation references

- [SQ7MRU.Utils/Infrastructure/EqslProtocol.cs](../../../SQ7MRU.Utils/Infrastructure/EqslProtocol.cs)
- [SQ7MRU.Utils/Infrastructure/EqslSession.cs](../../../SQ7MRU.Utils/Infrastructure/EqslSession.cs)
- [SQ7MRU.Utils/Infrastructure/EqslUploadResponse.cs](../../../SQ7MRU.Utils/Infrastructure/EqslUploadResponse.cs)
- [SQ7MRU.Utils/Infrastructure/HrdSession.cs](../../../SQ7MRU.Utils/Infrastructure/HrdSession.cs)
- [SQ7MRU.Utils/Infrastructure/EpcSession.cs](../../../SQ7MRU.Utils/Infrastructure/EpcSession.cs)
- [SQ7MRU.Utils/Infrastructure/EpcProtocol.cs](../../../SQ7MRU.Utils/Infrastructure/EpcProtocol.cs)

## Verification references

- `ServiceIntegrationTests.EqslLoginMatchesLegacyFieldsAndVerifiesProtectedAccountPage`
- `ServiceIntegrationTests.EqslLinkedAccountSwitchRequiresPrimaryAuthenticationInTheSameSession`
- `ServiceIntegrationTests.HiddenHamIdAloneDoesNotProveAnAuthenticatedAccountList`
- `ServiceIntegrationTests.AcceptsSuccessfulThirdAccountSelectionResponse`
- `ServiceIntegrationTests.UploadRequiresAnUnambiguousPositiveResult`
- `ServiceIntegrationTests.HrdSearchDeduplicatesLinksAndSkipsExistingValidImages`
- `ServiceIntegrationTests.EpcParsesHiddenLoginFieldsAndAwardForm`
- `ServiceIntegrationTests.EpcContinuesPastUnavailableAndInformationalCategories`
- `EpcProtocolTests`
- `PdfStorageTests`
