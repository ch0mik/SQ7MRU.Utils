## MODIFIED Requirements

### Requirement: Explicit format reference and compatibility boundaries

Public XML comments and READMEs SHALL identify the exact reviewed released ADIF specification from adif.org, distinguish standards rules from application policy, and disclose implemented ADI/ADX and field coverage. ADIF protocol revisions SHALL remain separate from product release tags. Historical ADIF 1.0 SHALL NOT be the primary authority for current ADIF 3 behavior.

#### Scenario: Reference upgrade

- **WHEN** a new official release is adopted
- **THEN** the reviewed revision, metadata, examples, XML descriptions and conformance fixtures are updated together
- **AND** the ADIF_VER output is not changed without a verified compatibility profile

#### Scenario: Consumer checks compatibility

- **WHEN** a consumer reads the ADIF API documentation
- **THEN** the reviewed specification, supported formats and current limitations are explicit
- **AND** CALL filtering, trimming and case normalization are identified as compatibility/service policies rather than generic format rules

#### Scenario: Reviewed standard and pending implementation

- **WHEN** ADIF behavior or public descriptions are changed
- **THEN** the exact reviewed official revision and affected constraints are checked and recorded
- **AND** documentation distinguishes verified implementation from outstanding conformance work

#### Scenario: New release check

- **WHEN** ADIF implementation, review or release preparation begins
- **THEN** maintainers check adif.org and its latest-release redirect for a newer released specification
- **AND** record the date, official URL, released revision, implemented revision and outstanding gaps
- **AND** a newer release triggers a reviewed update plan and tests rather than an automatic ADIF_VER change

#### Scenario: Provider compatibility boundary

- **WHEN** eQSL data differs from the latest standard profile
- **THEN** its observed import/upload behavior is documented and tested independently
- **AND** possible older provider behavior does not redefine the current standard or justify claiming unsupported latest features

### Requirement: Normalization and export

Strict interchange APIs SHALL preserve supported field values and distinguish absent from empty values. ADI export SHALL produce valid header/record framing, use only ADI-permitted fields/types and exclude import-only representations through validated mappings or explicit rejection. Case normalization and CALL filtering SHALL remain explicit service/legacy projection behavior rather than generic parser requirements.

#### Scenario: Valid ADI export

- **WHEN** a document with supported values is exported
- **THEN** its header starts with a non-tag character, EOH terminates the header, each record ends with EOR and declared lengths match the permitted data
- **AND** the advertised protocol profile has been validated independently

#### Scenario: Headerless record

- **WHEN** a valid CALL record ends with EOR without an EOH header
- **THEN** parsing accepts the headerless record and the explicit legacy projection retains its normalized-callsign behavior

#### Scenario: Nonrepresentable or deprecated data

- **WHEN** international-only data or an import-only representation cannot be exported through a documented standard replacement
- **THEN** strict ADI export returns an explicit diagnostic rather than emitting forbidden content or silently dropping it

### Requirement: Malformed records and disposal

The parser SHALL reject invalid field lengths, unterminated tags, invalid marker syntax and unfinished records regardless of CALL. It SHALL expose a documented duplicate-field policy, support cancellation and reject use after disposal. It SHALL distinguish format errors from adapter-specific requirements.

#### Scenario: Unfinished record without CALL

- **WHEN** input ends after a data field without EOR
- **THEN** strict parsing reports a malformed record even when CALL is absent

#### Scenario: Truncated value

- **WHEN** a declared length exceeds remaining input
- **THEN** the reader throws FormatException

#### Scenario: Marker with field syntax

- **WHEN** EOR or EOH is supplied with a declared field length
- **THEN** strict parsing does not accept it as an ordinary terminator

## ADDED Requirements

### Requirement: Latest standard and provider profiles

The library SHALL maintain a reviewed current-standard ADIF profile independently of provider compatibility profiles. eQSL import/upload SHALL use a documented, tested provider profile when older or nonstandard data requires it. New standard support SHALL preserve verified legacy compatibility and SHALL NOT force provider protocol upgrades or silently discard unrepresentable data.

#### Scenario: Standard update with older eQSL dialect

- **WHEN** support for a newer released ADIF revision is implemented
- **THEN** current-standard conformance fixtures and older eQSL regression fixtures both pass
- **AND** export to eQSL uses verified provider capabilities rather than assuming the latest revision is accepted

#### Scenario: Unsupported provider conversion

- **WHEN** a provider profile cannot represent supplied data
- **THEN** conversion returns an explicit diagnostic or uses a documented opt-in mapping instead of silently losing data

### Requirement: Document and extension preservation

The interchange document API SHALL retain header fields, supported QSO fields, exact data, type indicators and legal application/user-defined fields with their definitions. A separately documented legacy projection MAY retain existing service normalization/filtering behavior.

#### Scenario: Lossless extension round trip

- **WHEN** a supported ADI document contains USERDEF definitions, APP fields, empty fields and significant whitespace
- **THEN** import/export preserves their semantics and values without confusing embedded markers with structure

### Requirement: Reviewed field metadata and validation

Every QSO field in the reviewed official release SHALL have explicit coverage/type metadata and accurate public XML descriptions when exposed. Validation SHALL cover the supported fields' numeric/date/time/character constraints, enumerations, dependencies and import/export eligibility using culture-invariant behavior. Unsupported content SHALL receive explicit diagnostics in strict APIs.

#### Scenario: Field documentation and validation agree

- **WHEN** a field's accepted range, type, eligibility or dependent status changes
- **THEN** its metadata, XML comments and independent fixtures change together
- **AND** raw-string storage is not described as runtime validation

### Requirement: Reviewed band lookup

Frequency-to-band lookup SHALL follow the reviewed ADIF band table, including all listed intervals and their tested boundary behavior. Its documentation SHALL distinguish standard band labels from jurisdiction-specific transmission permissions.

#### Scenario: Previously missing or stale intervals

- **WHEN** lookup receives frequencies in the reviewed 8m, 5m, submm, 2.5mm or 2mm intervals
- **THEN** the results match reviewed metadata, with regression coverage for endpoints and out-of-range values

### Requirement: Explicit optional ADX scope

Documentation SHALL mark ADX unsupported until a separate implementation passes its own tests. ADI support SHALL NOT be represented as support for ADX international types. Any later ADX implementation SHALL use bounded XML processing, prohibit DTD/external entity resolution and verify the reviewed schema and character rules.

#### Scenario: ADI-only milestone

- **WHEN** only the ADI conformance milestone has been completed
- **THEN** the public capability statement claims the tested ADI profile without claiming ADX support
