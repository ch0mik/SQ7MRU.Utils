# ADIF conformance review

Reviewed on 2026-10-25 against [ADIF 3.1.8](https://www.adif.org/318/ADIF_318.htm), the released specification linked from [adif.org](https://www.adif.org/). The [latest-release redirect](https://www.adif.org/adif) is for discovery; implementation changes must identify the exact reviewed revision. ADIF revision numbers identify the interchange protocol, not this product's release tags.

This document retains the pre-change audit as evidence and records the implemented ADI milestone below. It is not an exhaustive certification of every external registry or historical ADIF condition.

## Implemented milestone

Local verification passed 206 .NET tests, the isolated installed-NuGet consumer (including current/legacy ADIF APIs and embedded metadata), all eight OpenSpec capabilities and a Windows x64 single-file publication. No live eQSL account or successful remote cross-platform execution is asserted.

- Rechecked the official release index on 2026-10-25; the released revision remains 3.1.8. The current standard export profile uses that reviewed revision; compatibility export/import are explicit and independent of product versions.
- Added all 28 previously absent properties: the model now contains 181 QSO fields, with corrected XML descriptions and normative references.
- Added `AdifDocument`, `AdifRecord`, `AdifField`, `AdifWriter` and explicit `Standard`/`EqslLegacy` profiles. Document parsing retains field/header values, types, extensions and empty values; text outside fields is not retained.
- Corrected ADI framing, invariant UTC timestamps, export eligibility, incomplete-record/marker handling, USERDEF/APP type validation and resource limits.
- Added supported type/range/enum/relationship checks and reviewed band metadata including endpoints. Metadata is embedded and updated offline through `eng/SQ7MRU.Build/AdifMetadataGenerator.cs`; no runtime network lookup occurs.
- Kept GetAdifRows as the tolerant service projection and verified older ADIF revisions and unchanged upload bytes with synthetic eQSL fixtures. Import-only data is accepted on import and rejected by standard export unless the caller explicitly selects compatibility behavior.
- ADX remains outside this ADI milestone. The strict profile is not an exhaustive external DARC/county/award registry, historical DXCC or geographic adjacency validator; numerical precision is bounded by decimal. Compatibility mode intentionally omits those current semantic restrictions.
- Run the conformance tests and installed-package smoke test before claiming this supported profile; broader conformance still requires domain-specific external checks. The sections below describe the original source gaps, not remaining missing properties.

## Pre-change scope and inventory

Before ADIF implementation, review and release preparation, check the [official index](https://www.adif.org/) and [latest-release redirect](https://www.adif.org/adif). Record the check date, resolved official source, released revision, implemented profile and gaps. A newer release requires review and tests; do not automatically change ADIF_VER or advertise unimplemented support.

eQSL may use an older or provider-specific ADIF dialect; this review does not establish its exact revision support. Maintain provider import/upload compatibility separately from the current-standard profile. New-standard support must retain older-provider regression fixtures and must not force unverified format upgrades on eQSL. Make unsupported conversions explicit rather than silently dropping data.

The code implements a string-backed QSO model and an ADI-style parser/exporter. There is no ADX API or lossless document/header model. The official QSO table contains 181 entries; `AdifRow` has 153 matching properties and no extra standard QSO names. Whitespace-normalized declared XML type names match the official type descriptions for all 153 existing properties. Matching types do not establish correct value validation or complete descriptions.

The standard permits selecting supported fields and does not require ADX. Missing fields are coverage gaps against this project's broader goal, rather than automatic proof of nonconformance. ADI syntax and export restrictions still apply to every supported field. CALL filtering belongs to the downloader's workflow policy, not the generic interchange grammar.

## Pre-change findings and priorities

| Priority | Finding in the current implementation | Required direction |
| --- | --- | --- |
| P0 | `ExportAsADIF` starts with `<PROGRAMID...>` and then emits EOH. Its header does not satisfy the current ADI header rule either, not merely the historical ADIF 1.0 rule. | Emit a valid header prefix, preserve header metadata and verify export with independent fixtures. |
| P0 | Reflection exports every populated property, including `_INTL` and import-only fields such as GUEST_OP and VE_PROV. | Use format-aware field metadata and enforce export eligibility. Do not silently destroy unsupported international data. |
| P0 | Export always declares ADIF_VER 3.0.6 without a compatibility gate. | Declare a protocol revision only when the generated content satisfies the supported profile for that revision. Changing the version string alone is insufficient. |
| P0 | An empty row array produces only a header, and an empty row produces only EOR. | Decide strict empty-input behavior explicitly; do not present structurally empty ADI output as a valid nonempty record document. |
| P1 | `GetAdifRows` trims all values and drops header, extension and unmodeled fields. | Preserve exact values, absent versus empty values, header data and extension metadata in an interchange document API; retain a compatibility projection for existing callers. |
| P1 | A CALL-bearing unfinished record fails, but an unfinished record containing other fields can be silently discarded. Duplicate fields overwrite earlier values. | Specify malformed-input and duplicate-field diagnostics separately from service-specific required fields. |
| P1 | Tags such as `EOH:1` and `EOR:1` are treated as terminators; extra tag segments and type indicators are not validated. | Validate grammar rather than matching only the first tag segment. |
| P1 | Arbitrary .NET strings are accepted; UTF-16 lengths, numeric/date values and field-specific constraints are not checked. | Implement format-appropriate character rules, invariant values and bounded validation. ADI international restrictions cannot be solved by changing UTF-8 byte counts. |
| P1 | The creation timestamp uses the current culture's calendar through DateTime formatting. | Use invariant Gregorian UTC formatting and test non-Gregorian process cultures. |
| P1 | `FreqToBand` excludes every exact endpoint, lacks 8m, 5m and submm, and uses stale upper/lower bounds for 2.5mm and 2mm. | Derive the mapping from reviewed band data, define boundary behavior and test every interval. |
| P1 | The model lacks 28 QSO properties listed below. | Add the selected coverage with metadata, XML comments and round-trip tests; separate import-only compatibility from current export support. |
| P2 | ADX and its international character types are unsupported. | Keep ADX explicitly unsupported until a separately tested XML implementation is added; ADX is not required for ADI conformance. |

Current strengths include case-insensitive tag lookup, declared-length scanning, embedded marker handling, headerless input, cancellation checks and bounded transport/storage payloads. These features do not prove semantic conformance. The artifact store's EOH/EOR/HTML screening is not a standards validator.

## Previously missing QSO fields

The following 28 fields were absent from the model and have now been added. This list was computed from the official QSO table, not from the table of contents or headers.

| Area | Missing names |
| --- | --- |
| Location | ALTITUDE, MY_ALTITUDE, CNTY_ALT, MY_CNTY_ALT, GRIDSQUARE_EXT, MY_GRIDSQUARE_EXT |
| Membership and subdivisions | MY_ARRL_SECT, MY_DARC_DOK |
| Confirmation | DCL_QSLRDATE, DCL_QSLSDATE, DCL_QSL_RCVD, DCL_QSL_SENT, EQSL_AG, QSLMSG_RCVD |
| Upload/download tracking | HAMLOGEU_QSO_UPLOAD_DATE, HAMLOGEU_QSO_UPLOAD_STATUS, HAMQTH_QSO_UPLOAD_DATE, HAMQTH_QSO_UPLOAD_STATUS, QRZCOM_QSO_DOWNLOAD_DATE, QRZCOM_QSO_DOWNLOAD_STATUS |
| Morse key information | MORSE_KEY_INFO, MORSE_KEY_TYPE, MY_MORSE_KEY_INFO, MY_MORSE_KEY_TYPE |
| Parks and flora/fauna | POTA_REF, MY_POTA_REF, WWFF_REF, MY_WWFF_REF |

Header fields and extensible `USERDEFn` definitions need a document model rather than more `AdifRow` properties. Unknown `APP_*` and user-defined fields require an explicit preservation/unsupported-field policy.

## XML description corrections identified and applied

| Existing comments | Review result |
| --- | --- |
| RX_PWR and TX_PWR | Their positive-only wording is stale; zero is permitted by the reviewed field definitions. |
| VUCC_GRIDS and MY_VUCC_GRIDS | Four-character-only wording is incomplete; the reviewed definition also allows six-character locators. |
| GRIDSQUARE and MY_GRIDSQUARE | Add the relationship to extension fields for longer locators. |
| QSL_RCVD_VIA and QSL_SENT_VIA | Document the distinction between completed conveyance and requested/intended conveyance, depending on status. |
| COMMENT, NOTES and QSLMSG, including international counterparts | Clarify their separate purposes rather than treating all as an interchangeable QSL message. |
| SAT_MODE | Explain the uplink/downlink band-code meaning. |
| `_INTL` properties | State that they are ADX-only, rather than implying the current ADI exporter can emit them conformantly. |
| Import-only fields and enum values | Describe import compatibility, replacements and export restrictions. Raw strings currently enforce none of these rules. |

Existing comment text also contains joined words and sample field lengths that become unreliable when spacing is changed. Rewrite descriptions in original concise English and verify every executable example. Maintain the distinction between a field's normative semantics and what the current code actually validates.

## Implementation sequence and acceptance gates

1. **P0:** Introduce reviewed field/export metadata, correct ADI framing and prevent invalid international/import-only exports. Add independent golden fixtures before changing output behavior.
2. **P1:** Add a lossless document API, strict diagnostics and an explicit legacy projection. Preserve the downloader's account/card behavior while moving service requirements out of generic parsing.
3. **P1:** Complete the chosen QSO coverage, field relationships, enum/date/range checks and band mappings. Update each XML comment together with metadata and meaningful tests.
4. **P2:** Decide and document ADX scope. If implemented, use safe XML settings, the official schema and international round trips; prohibit external entities/DTDs and bound input sizes.
5. Gate conformance claims on declared-profile tests, official resource fixtures where redistribution permits, independent parsing and package/XML documentation checks. Update the baseline specs only after implementation is verified.

## Source references

- [Field support and application policy](https://www.adif.org/318/ADIF_318.htm#Introduction)
- [Types](https://www.adif.org/318/ADIF_318.htm#Data_Types), [QSO fields](https://www.adif.org/318/ADIF_318.htm#QSO_Fields) and [band enumeration](https://www.adif.org/318/ADIF_318.htm#Band_Enumeration)
- [ADI](https://www.adif.org/318/ADIF_318.htm#ADI_File_Format) and [ADX](https://www.adif.org/318/ADIF_318.htm#ADX_File_Format)
- [Current parser](../SQ7MRU.Utils/ADIF/AdifReader.cs), [export/helper](../SQ7MRU.Utils/ADIF/AdifHelper.cs) and [model/XML comments](../SQ7MRU.Utils/ADIF/AdifRow.cs)
