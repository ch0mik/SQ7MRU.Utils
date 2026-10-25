# Tasks

## 1. Review and scope

- [x] Inspect the official ADIF site and identify the reviewed released protocol revision.
- [x] Compare all current QSO field names/type descriptions and record the 28 missing fields.
- [x] Review parsing, export, band mapping and XML description gaps.
- [x] Add truthful README guidance and OpenSpec implementation requirements.

## 2. P0: ADI export correctness

- [x] Recheck the official latest released specification before implementation and record date, source, supported revision and new gaps.
- [x] Define the current-standard profile independently of the observed eQSL import/upload dialect and add older-provider regression fixtures.

- [x] Define the strict supported profile and compatibility behavior without only changing ADIF_VER.
- [x] Add independent golden fixtures for header, framing and export eligibility failures.
- [x] Correct header framing and prevent international/import-only content from being emitted incorrectly.

## 3. P1: Document model and validation

- [x] Introduce lossless header/value/extension representation with bounded parsing and cancellation.
- [x] Preserve the existing service-oriented QSO projection through an explicit compatibility boundary.
- [x] Validate tag grammar and incomplete records independently of CALL; document duplicate handling.
- [x] Add supported field types, bounds, status relationships and enum validation; document external registry/historical/geographic limits explicitly.

## 4. P1: Coverage, descriptions and bands

- [x] Add reviewed metadata and coverage for the 28 missing standard QSO fields.
- [x] Correct stale descriptions and add ADI/ADX and import-only restrictions to every relevant public XML comment.
- [x] Replace stale band intervals with reviewed data and boundary tests.
- [x] Verify examples, installed XML documentation and ADI behavior under different cultures.

## 5. P2: ADX decision and acceptance

- [x] Recheck the official released revision during release preparation and plan any newer-standard update without silently changing provider profiles.

- [x] Confirm the ADI-only milestone; keep optional ADX unsupported with explicit documentation.
- [x] Keep conditional ADX implementation out of scope for this milestone; retain schema/XXE/international requirements for a future ADX change.
- [x] Run independent conformance fixtures and relevant service regression tests.
- [x] Update baseline specs and archive this change only after implementation is complete.
