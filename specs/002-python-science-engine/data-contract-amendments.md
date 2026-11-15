# Approved contract changes — application and consumer coordination

**Date**: 2026-11-15. **Owner decision**: full-precision science and complete-only v1 approved in the Planning request. **Application**: NOT APPLIED; both user-owned shared copies remain unchanged by this stage.

The original detailed exact replacement/addition text is [proposals/data-contract-amendments.md](proposals/data-contract-amendments.md), N1–N7. Its proposal status is historical. The user has selected N's full-precision option and complete-only U; its alternative rounded-science route and optional version-2 extension are **not selected**. The operational wording below reconciles that text with the latest explicit approval to report exclusions and return fewer/zero known recommendations.

## Exact text selected for coordinated application before implementation

| Section | Approved application |
| --- | --- |
| Contract location/ownership | Apply N1 exactly: canonical docs path, deliberate migrated-reference synchronization, process ownership and no shared-state/branch retargeting. |
| JSON structure | Apply N2 exactly: primitive numeric time series; fixed-schema objects/object arrays permitted; no arbitrary maps/root arrays/arrays of arrays. |
| Numerical precision | Apply N3's recommended replacement and validator text exactly: binary64, round-trip JSON, display-only rounding, strict finite values, explicit arithmetic tolerances/boundary audit. Do not select the three-decimal-science alternative. |
| Time/cells | Apply N4 exactly: explicit UTC, endpoint-inclusive n instants/n−1 cells, a+m<=n−1, half-open missions/search, no truncation or hidden final arrival omission. |
| Observer/frame/horizon | Apply N5 exactly: matched physical observer/datum, declared radial/geodetic basis, verified lunar frame/registration, analytic dip and periodic interpolation. |
| Complete-only data | Apply N6's complete-only site/model rules; use the exact cohort replacement below instead of its historical all-or-nothing Finder paragraph. No v2 fields. |
| Site mapping | Apply N7 exactly: IDs/asset labels do not verify coordinates/heights; no moving outside-DEM sites or unverified production values. |

Replace the historical N6 paragraph starting “Candidates with UNKNOWN required samples are unevaluable” with this exact coordinated text:

> Candidates with UNKNOWN required samples are unevaluable, not known constraint failures or closest misses. Incomplete scientific results are not certified or exported as terrain-aware. Keep geometric-only and terrain cohorts explicit and separate. Under complete-only v1, site files contain only known essential Sun/Earth samples and relevant skyline support. Strict all-site requests fail if any requested site cannot meet that gate. An explicit allow-exclusions request may accept a smaller complete cohort, but must record and report every excluded requested site/scenario and its reason in science-owned run/status evidence and the user handoff. `candidate_sites` and wire evaluated/rejected counts identify only that accepted complete cohort, never imply exhaustive evaluation of excluded original sites. Local audit preserves original requested/evaluated/rejected/qualifying/unevaluable counts. No partial visibility masks or unknown placeholders are added to v1. Zero qualifying scenarios among eligible complete sites yields empty qualifying lists; no eligible complete sites yields a local empty/exclusion report without a misleading scientific exchange file.

Retain N6's previously specified exact hard-filter/score/policy/miss definitions and the complete-only site rules. The original request/accepted cohort transition must be explicit in CLI and evidence, not a silent reduction of search scope. `evaluated_count−rejected_count` remains the accepted cohort's number of qualifiers before top-three limits.

Publication coordination text to add to section 6:

> Validate actual staged bytes independently, confirm destination ownership and retain rollback evidence. Fixed scientific filenames are individually replaceable, not an atomic multi-file snapshot. Coordinate a consumer reload/startup barrier during commit, replace validated files atomically with index last, verify final hashes and then perform a deliberate reload. Without the barrier or ownership, retain the release staged. Agents must not change consumer code to bypass this gate.

## Implementation-entry receipt

Record the applied canonical contract version/hash and whether the migrated reference is intentionally synchronized; do not let agents auto-copy over either. Obtain Unity-owner acknowledgement of:

1. C# double scientific arrays/math and float only for rendering.
2. Full-precision numbers and the explicit parity tolerances/boundary audit.
3. 4465 instants/4464 cells and half-open windows.
4. Explicit accepted complete cohorts, exclusion evidence and no partial interchange.
5. Same observer/frame/azimuth conventions, correct 2 m dip and deliberate release reload.

The user reports Gemini is being notified separately; this stage does not send messages or require approval of the same decisions again. Receipt/application and executable parity are distinct evidence, still pending. These are the first future implementation prerequisites, not unresolved design choices or permission to modify the contract during Plan.
