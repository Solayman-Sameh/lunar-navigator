# Feature Specification: Python Scientific Engine

**Feature Branch**: `002-python-science-engine` (directory identifier; no branch created or switched)

**Created**: 2026-11-15

**Status**: Technical planning complete for review. G verified/applied as constitution v1.1.0; N full precision and U complete-only v1 approved by the user. Shared-contract application/consumer coordination remains an implementation-entry gate. No Python implementation authorized in this stage.

**Input**: Dedicated scientific engine using `backend/docs/1_PYTHON_SCIENCE_PLAN.md`, the 30 clarification decisions and the user's Planning approvals/incremental priorities. Preserve the Unity feature, contracts and concurrent agents' files. The explicitly approved additive constitution amendment is applied after verification. Stop after Plan.

## Clarifications

### Session 2026-11-15

- Governance (D01–D02): clarification/design are permitted under constitution 1.0.0; preserve the approved-governance gate before implementation. Exact new additive proposal G is unratified. Root docs contract is the operational reference; preserve both copies and stop on divergence.
- Data/numerics (D03–D15): typed objects/object arrays, full-precision internal science, separate serialization validation, UTC, endpoint/cell limits, matched physical observer, verified lunar frame/registration, header-based parsing, periodic skyline interpolation and analytic −0.086937-degree dip are specified exactly. Recommended N changes authoritative exchange to full-precision numbers with display-only rounding and binary64 parity; owner approval remains required.
- Terrain uncertainty (D16–D21): invalid/missing/mis-mapped terrain produces explicit UNKNOWN states, bounded metrics, incomplete-sector evidence and exclusion from confident ranking. Unchanged v1 must not export unknown body flags; U proposes an optional version-gated known-mask extension for partial interchange.
- Mission/search (D22–D28): earliest arrival and search end define a start-inclusive/end-exclusive grid, full independent mission duration, exact five metrics, all mandatory constraints before score, deterministic best/earliest policies, honest empty/short lists, explicit misses/counts. Presets remain illustrative and configurable.
- Site mapping (D29–D30): stable science IDs require exact sourced observing points, not asset labels or region names. A final read-only check found Unity now uses the three science IDs; its candidate coordinates/zero elevations are still unverified scientific inputs. Unsupported DEM coverage makes terrain unavailable; no coordinate is invented, moved or silently substituted. Current regional products are broader than the earlier recorded 87–90 S-only assumption.

These record the source-derived Clarify session, not invented user answers (0/0 interactive questions). The subsequent Planning request explicitly approved G/N/U: [ratification evidence](governance-ratification.md) and [current contract coordination](data-contract-amendments.md) record their current status. N selects full precision; U selects complete-only v1, not its historical optional v2 proposal.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Inspect a verified site using real celestial data (Priority: P1 / science plan P0)

A planner selects a supported south-pole site, landing UTC, and mission duration to inspect actual Sun/Earth directions, timelines, and mission-wide availability. A geometric-only result provides an explicitly limited first delivery before terrain is validated.

**Why this priority**: Real, traceable observations and window calculations provide the first useful result and enable independent frontend integration.

**Independent Test**: Analyze one verified site using retained real Sun/Earth responses and a declared geometric horizon. Independently check times, directions, visibility, and mission metrics without opening Unity.

**Acceptance Scenarios**:

1. **Given** verified coordinates and real observations, **When** the baseline is requested, **Then** both target tracks begin at `2026-12-01T00:00:00Z`, have 600-second spacing through `2027-01-01T00:00:00Z`, and retain source evidence.
2. **Given** paired tracks and a valid aligned 72-hour request, **When** Site Inspector evaluates it, **Then** it returns Sun/Earth/joint timelines, all five metrics, visible intervals, and next losses within declared coverage.
3. **Given** unavailable/unvalidated terrain, **When** explicit fallback is selected, **Then** the result says `GEOMETRIC_ONLY`, identifies its idealized horizon, and makes no real-terrain claim.
4. **Given** another supported date or site, **When** the same duration and assumptions are applied, **Then** the two identified scenarios can be compared reproducibly.
5. **Given** missing source data or unverified coordinates, **When** production analysis is requested, **Then** it reports unavailable inputs and produces no fabricated science.

### User Story 2 - Assess terrain and its limits (Priority: P1 / science plan P0)

A planner sees how NASA LOLA topography changes visibility and where coverage, resolution, or uncertainty prevents a confident conclusion.

**Why this priority**: Near-horizontal polar directions make terrain obstruction consequential; missing terrain must never masquerade as clear sky.

**Independent Test**: Check a spherical surface, blocking wall, raised observer, rotated wall, and missing-data wedge; then inspect real DEM coordinates, heights, skyline, and coverage evidence.

**Acceptance Scenarios**:

1. **Given** an acquired, georeferenced LOLA DEM, **When** a skyline is generated, **Then** it contains 360 one-degree values, observer/range provenance, and explicit incomplete sectors.
2. **Given** a complete zero-height spherical surface and a 2 m observer, **When** the ideal skyline is checked, **Then** it is approximately `-0.086937°`; finite-raster sampling error is assessed separately from the analytic reference.
3. **Given** missing pixels, truncated extent, inadequate range, or unavailable site ground height, **When** processing runs, **Then** affected directions or the site remain incomplete/unavailable; interpolation cannot erase the original warning.
4. **Given** Sun/Earth directions with incomplete skyline support, **When** results are reported, **Then** visibility is UNKNOWN and whole-mission metric bounds remain explicit; no binary placeholder is interpreted scientifically and the scenario is excluded from confident terrain ranking. V1 publication is withheld for affected datasets; partial interchange requires approved U.
5. **Given** passed synthetic and real-data checks, **When** a terrain result is released, **Then** it may use `TERRAIN_AWARE_ESTIMATE` with stated limits; failed/unexecuted checks cannot earn that status.

### User Story 3 - Find best and earliest opportunities (Priority: P2 / science plan P1; required scope)

A planner searches the supported sites and arrival times for complete missions satisfying explicit constraints. Best Overall and Earliest Qualifying provide distinct, explainable selections.

**Why this priority**: Search depends on validated inspection. Its later delivery order does not remove it from required scope.

**Independent Test**: Hand-calculated candidate metrics prove filtering, ranking policies, ties, closest misses, and full evaluation of the last permitted arrival.

**Acceptance Scenarios**:

1. **Given** three eligible sites, 14 search days from `2026-12-01T00:00:00Z`, six-hour arrivals, and 72-hour missions, **When** evaluated, **Then** there are 56 arrivals per site and 168 scenarios; the last arrival is `2026-12-14T18:00:00Z` and its whole mission is evaluated.
2. **Given** hard mission constraints, **When** any one fails, **Then** that scenario is excluded before scoring and all failed constraints are recorded.
3. **Given** qualifiers, **When** Best Overall is selected, **Then** at most three rank by score, with earlier UTC then site ID breaking ties.
4. **Given** qualifiers at multiple arrivals, **When** Earliest Qualifying is selected, **Then** only sites at the earliest qualifying sampled arrival are returned, at most three, ordered by score then ID.
5. **Given** zero qualifiers, **When** the run completes, **Then** both qualifying lists are empty, counts reconcile, and up to five closest misses explain failures; thresholds are not weakened to manufacture winners.

### User Story 4 - Receive validated, reproducible exports (Priority: P1 / science plan P0)

An integrator receives offline files independently checkable against the shared contract, with traceable evidence and honest quality labels.

**Why this priority**: Independent agents need stable data semantics and a release that does not overwrite each other's work.

**Independent Test**: Validate a staged release without Unity, inject structural and mathematical faults, and confirm rejection while preserving an existing valid release.

**Acceptance Scenarios**:

1. **Given** applied approved contract interpretations and validated geometric results, **When** the first incremental export runs, **Then** one complete verified site plus index is useful; later releases expand toward all three. Accepted subsets/exclusions are explicit, statuses honest, with no Unity stub/code/.meta/settings edits.
2. **Given** site exports, **When** visibility and nine reference windows are independently recomputed, **Then** all match the approved numerical policy or publication fails with readable reasons.
3. **Given** a validated Finder result, **When** published, **Then** the index references an existing validated opportunity file with both policies and their assumptions intact.
4. **Given** identical retained inputs/parameters/version, **When** repeated offline, **Then** numerical results, quality flags, and ordering agree; only declared run metadata such as generation time may differ.
5. **Given** another process owns a destination or publication fails, **When** release is attempted, **Then** existing files remain intact and the conflict is reported; staged results remain reviewable.

### Edge Cases

- Unity asset IDs/names may align with the scientific catalog without independently verifying their exact coordinates, ground heights or observing-point provenance. Historical demo IDs are not the current mapping; a named region is not an exact observing point.
- Coordinates disagree by more than 0.05°; longitude crosses ±180°; azimuth crosses 359°/0°; a latitude is exactly a pole or ill-conditioned near it.
- Source errors inside structured responses, unexpected/ambiguous columns, blank flags, duplicate chunk endpoints, missing timestamps, incompatible time scales, or stale cache identity.
- A DEM covers the observer but not distant terrain; a polar mosaic excludes Malapert; site ground height is missing; every horizon bin is missing; an obstruction lies inside the near-field exclusion radius.
- NoData crosses azimuth zero, gaps are filled for numerical continuity, or valid pixels falsely suggest complete directional coverage.
- DEM, site, and ephemeris use different frames, radii, height datums, or observer altitudes.
- A body lies exactly on the skyline plus margin; rounding changes visibility, ranking, or constraint qualification.
- Duration is zero, negative, non-finite, off-grid, or beyond coverage; outages touch window edges; everything/nothing is visible.
- The final ephemeris row is the stop endpoint, not proof of an additional ten-minute interval after it.
- No/fewer-than-three qualifiers, score ties, zero maximum allowed outage, or unknown terrain affecting a proposed winner.
- Concurrent changes to contract copies, feature selection, or output ownership; failed or interrupted publication.

## Requirements *(mandatory)*

### Functional Requirements

Requirements define observable behavior and correctness. The existing science plan remains the technical baseline; [plan.md](plan.md) specifies the reviewed future implementation order without executing it. D01–D30 remain normative with the user's latest approval overrides recorded below and in [data-contract-amendments.md](data-contract-amendments.md). G is applied, N's full precision and U's complete-only v1 are selected; shared contract text has not been changed.

#### Scope and governance

- **FR-001**: Support `malapert_mountain`, `shackleton_rim`, and `connecting_ridge` with real ephemerides, verified coordinates, LOLA horizons, timelines, metrics, Site Inspector, Finder, both ranking policies, validated exports, provenance, uncertainty, and tests. Acceptance: Stories 1–4 and SC-001–SC-008.
- **FR-002**: Science MUST be usable and verifiable without Unity or a running web service. FastAPI, ML, arbitrary-coordinate live services, whole-Moon optimization, trajectories, power/thermal models, RF link budgets, and landing-safety certification are deferred. Acceptance: reproduce science from retained inputs without frontend/service startup.
- **FR-003**: Respect documented/process ownership. Planning writes only this feature directory except the user's expressly approved, verified constitution ratification. Future publication requires destination ownership and consumer reload coordination; no Unity code/scenes/settings/packages/stubs/.meta edits. Acceptance: scoped audit against workflow-record.md and ratification record.
- **FR-004**: Preserve Unity I–X, delivery/workflow and existing governance; additive XI–XVII is actually ratified in constitution v1.1.0 after explicit user approval and verification. The historical XI–XXV draft is not reconstructed. Acceptance: exact before/after hashes, preserved-text checks and ratification evidence; future implementation complies with v1.1.0.

#### Coordinates and real ephemerides — S1–S3

- **FR-005**: Each science coordinate MUST have an exact observing point, citation, verification status, latitude/east longitude, frame/realization, and elevation reference. Normalize longitude to [-180°,180°] and record conversion. Do not guess coordinates or silently substitute demo assets. Acceptance: three verified entries and comparison with actual Unity values.
- **FR-006**: Report disagreements over 0.05° with both sources; authoritative verified coordinates take precedence only in the science catalog. Reject exact ±90° for this azimuth convention; retain stated near-pole coordinates at |latitude|≥89.95° with conditioning warnings. Acceptance: mismatch, pole, and near-pole cases.
- **FR-007**: Retain actual JPL Horizons Sun (10) and Earth (399) lunar-surface apparent airless observer directions, verified settings/headers/raw responses/source identity. Use explicit UT/zero offset for the supported modern UTC dates and reject unsupported leap-insertion intervals, TT/TDB relabeling, or source time ambiguity (D07). Acceptance: executed source proof and matched first/middle/last-row independent comparisons under the source-precision tolerance in clarifications.md.
- **FR-008**: Baseline tracks MUST span `2026-12-01T00:00:00Z` through `2027-01-01T00:00:00Z` at 600 s, with 4,465 unique endpoint-inclusive rows per target/site, no gaps, and paired timestamps. Acceptance: exact count/start/end/cadence and chunk-boundary checks for all sites.
- **FR-009**: Parse by verified actual header meanings, not numeric position. Validate HTTP/JSON error/result, recognized signature/version, metadata, named airless fields, one SOE/EOE pair, finite angles and exact cadence. Preserve blank flags. Document aliases; reject n.a., incompatible settings and conflicting duplicate boundary rows (D13). Acceptance: retained real response plus error/version/header/blank-flag/chunk fixtures.
- **FR-010**: Matching cached inputs MUST support offline reuse without requests unless refresh is selected. Cache identity MUST distinguish coordinates, physical observer height/datum, target, dates, step, frame/model, and source. Changed inputs cannot reuse a stale named file as equivalent. Final retrieval failure retains error context and produces no invented tracks. Acceptance: offline, refresh, changed-input, and failure cases.
- **FR-011**: Check finite angles, azimuth [0°,360°), elevation [-90°,90°], Sun bounds ±(90−|latitude|+1.7)°, Earth bounds ±(90−|latitude|+7.2)°, and near-pole unwrapped Sun sweep magnitude 340–400° over the baseline where 90−|latitude|<1°. Investigate failures; never relax limits merely to pass. Acceptance: recorded checks and inspected plots; any necessary scientific correction requires evidence/review.
- **FR-012**: Ephemeris observer altitude MUST represent the terrain observer after verified datum/frame conversion, including ground plus mast height. For matching spheres altitude km=(R_dem+z+h−R_source)/1000; ellipsoidal sources require conversion of the same Cartesian observer. Preserve declared local radial/geodetic basis and lunar ME realization compatibility per D09–D11. A geometric fallback may explicitly use nominal ground on the reference sphere, never claim actual ground-height verification. Acceptance: documented conversion and matched observer before terrain release.

#### LOLA terrain and skyline — S4–S5

- **FR-013**: Use an actually acquired NASA LOLA elevation product. Record exact name/URL, retrieval identity, CRS/frame, registration/transform, bounds, resolution, units, elevation reference, NoData statistics, and site coverage. Acceptance: readable real raster and metadata; a product page is not acquisition evidence.
- **FR-014**: Verify coverage for each site and requested horizon extent rather than assume one regional mosaic suffices. Prefer the smallest adequate product/subset within the plan's approximate 1.5 GB full-download constraint; additional verified products or explicit fallback are allowed. Acceptance: per-site/range coverage table; no invented product filename, resolution, or URL.
- **FR-015**: Use actual lunar metadata/registration, distinguish projection from body-fixed realization, and meet D12's wrapped angular <=1e-6 degree, Cartesian <=0.05 m and projected <=min(0.05 m,0.01 pixel) round-trip tolerances away from singularities. Independent landmark agreement must fit its declared source uncertainty plus one pixel. Acceptance: numerical tests and sourced Shackleton interior/rim checks; unknown control-point accuracy or round-trip alone cannot establish real accuracy.
- **FR-016**: The skyline MUST represent greatest apparent blocking elevation by azimuth in a consistent curved lunar geometry. Defaults remain observer height 2 m, maximum range 150 km, and 360 values at azimuths 0…359°. Height/range MUST be configurable and recorded. Acceptance: analytic/synthetic cases and inspected real profiles.
- **FR-017**: An ideal sphere MUST satisfy `H = -acos(r_surface/(r_surface+h))` in degrees. At radius 1,737,400 m,h=2 m H=−0.08693657193373108 degrees; analytic agreement tolerance is 1e-8 degree. A sufficiently sampled complete spherical DEM including the tangent neighborhood must agree within the plan's 0.02 degree raster tolerance. The small-height approximation requires bounded error; −0.05 degree is not the correct 2 m value. Acceptance: D15 analytic and finite-resolution checks, curvature counted once.
- **FR-018**: Do not double-count curvature. Consistent terrain line-of-sight geometry MUST agree with the sphere; approximate wall formulas serve only as declared test approximations. Assess sparse pixels, finite range, and the plan's exclusion inside twice the pixel size for missed near-field blockers. Acceptance: 1,000 m wall at 10 km north within the plan's 0.15° tolerance, rotated peak at 90°, raised-observer case, and near-field/range sensitivity evidence.
- **FR-019**: Preserve invalid cells, extent/range/near-field truncation and filled-bin warnings. Short-gap numerical fill retains unknown quality. Mark any unbounded relevant missing terrain within requested range incomplete; ending before half range is a minimum warning trigger, not a completeness guarantee. Sectors are half-open and split at 0/360 (D16–D17). Ground sampling needs all nonzero-weight cells valid; mapping/ground failure makes that terrain pairing unavailable. Acceptance: wedge, short-gap, wrap, edge, invalid-height/mapping and all-missing cases.
- **FR-020**: Use explicit VISIBLE/HIDDEN/UNKNOWN for scientific visibility (D18). Missing support never becomes a measured 0/1; finite fills support plots only. Compute whole-mission lower/upper bounds rather than biased known-only means (D19). Exclude missions with any UNKNOWN required Sun/Earth sample from confident terrain ranking. V1 cannot publish a dataset containing UNKNOWN flags; retain incomplete science locally unless U is approved. Explicit geometric fallback is a whole-scenario separate model. Acceptance: missing terrain creates neither trusted visibility nor a trusted winner.
- **FR-021**: Separate source uncertainty from resolution/interpolation, coordinate/datum, height, range/near-field, binning, and sampling sensitivity. Use available source uncertainty/count layers; state unavailable components instead of inventing confidence intervals. Acceptance: per-site evidence at heights 0.5/2/10 m and ranges 75/150 km, plus one 72-hour window at 10-/30-minute steps.
- **FR-022**: A released site earns `TERRAIN_AWARE_ESTIMATE` only after required synthetic and actual DEM/frame/site/skyline checks pass and all essential exported samples/relevant skyline support are known. Incomplete analysis is not certified terrain-aware and remains local. Explicit whole-scenario geometric fallback may earn GEOMETRIC_ONLY; no silent terrain/flat splice. Acceptance: failed/skipped/unknown cases cannot promote status or manufacture binary values.

#### Site Inspector and mission metrics — S6

- **FR-023**: Accept supported site, aligned landing UTC, and positive duration containing whole samples; default 72 h and support 24/72/168 h. Reject invalid, off-grid, unsupported, and out-of-coverage requests with reasons; the science engine MUST NOT silently clamp or shorten them. Acceptance: first/last fitting and invalid-input cases.
- **FR-024**: Known Sun visibility uses strict elevation>skyline+solar margin (default zero); Earth margin zero; equality hidden. Normalize A modulo 360, k=floor(A), w=A−k and H(A)=(1−w)H[k]+wH[(k+1)%360]. Any incomplete nonzero-weight support means UNKNOWN; integer azimuth uses only that sample. Both uses D19's three-state conjunction. Treat bodies as apparent airless point centers. Acceptance: equality/margin/interpolation/wrap/knownness fixtures and separately audited arithmetic-boundary parity per D06/D14.
- **FR-025**: Cells cover `[t_i,t_i+step_s)` and missions `[landing,landing+duration)`. Endpoint-inclusive n instants establish n−1 cells, so require 0<=a, m>0, a+m<=n−1 and mission end<=source stop. The final visibility entry is instantaneous display state, not an extra cell. Acceptance: six cells=one hour, 72 h=432 cells, baseline=4464 usable cells, end-at-stop succeeds and exceed-stop fails (D08).
- **FR-026**: For wholly known windows return `f_sun`, `f_earth`, `f_both` as means over mission cells and `dark_max_h`, `comm_gap_max_h` as longest hidden runs times step hours, clipped to the mission. If a required state is UNKNOWN, report the explicit whole-window bounds in D19, not an invented scalar or known-only mean. Acceptance: all/none visible, interior/edge/unknown gaps, hand-worked ten-cell case and too-long rejection.
- **FR-027**: Return visible intervals with end-exclusive boundaries and next Sun/Earth loss within coverage. Distinguish already hidden, a future loss, no loss before end, and unavailable input. Never extrapolate transitions or claim finer than sample resolution. Acceptance: hand-built timelines for all outcomes and comparison of two scenarios.

#### Opportunity Finder — S9

- **FR-028**: Arrivals are L0+j*Delta<L0+W, j>=0; require aligned L0, positive whole-sample Delta and D, and W>0 elapsed search length. W need not be an integer multiple of Delta. Missions may exceed search end but all requested arrivals' complete missions must fit source coverage; otherwise reject the request, never silently drop late arrivals. Default remains 168 scenarios for three eligible sites. Report unavailable scientific sites separately (D21–D23/D28). Acceptance: boundaries, off-grid, independent duration and insufficient trailing coverage.
- **FR-029**: Apply all enabled minimum fractions with >= and maximum outages with <= before scoring, only for fully evaluable scenarios. Require finite fractions [0,1], outage limits>=0; thresholds remain user-configurable. Initial preset 0.30/0.40/36 h/48 h comes from contract examples, is illustrative, not NASA standards; record rationale/distributions without tuning winners (D25). Acceptance: equality, isolated failures, invalid requests and unknown scenarios.
- **FR-030**: Among qualifiers use `0.4*f_sun + 0.3*f_earth + 0.2*f_both + 0.1*(1-min(1,dark_max_h/duration_h))`. Record assumptions/weights; return at most three Best Overall results; tie-break earlier UTC then site ID. Acceptance: hand-calculated bounded scores, deterministic ties, and zero allowed darkness without division by that limit.
- **FR-031**: Earliest Qualifying first selects the earliest qualifying sampled arrival, then up to three sites at that exact arrival sorted by descending score then ID. It does not establish trajectory feasibility or return scenarios at later times. Acceptance: a later highest-score scenario produces different best/earliest results.
- **FR-032**: Retain original requested/evaluated/rejected/qualifying/unevaluable accounting and reasons locally. Known closest misses use D28's exact normalization and ordering. Unknown candidates are excluded, never scored or treated as known failures. Under the user's approved complete-only v1, an explicit allow-exclusions request may publish a smaller complete accepted cohort; wire candidate_sites/counts describe only that cohort and all exclusions/reasons are reported outside v1. Strict all-site mode otherwise fails. Zero eligible sites yields local empty/exclusion evidence, no misleading wire dataset. Acceptance: original/accepted sets, empty/short/unknown fixtures and reconciled counts.

#### Export and verification — S7–S8, S10–S11

- **FR-033**: Follow the selected complete-only v1 exchange profile: root object, typed objects/object arrays, parallel primitive series, 360 horizon values, known integer flags, UTC start/step/count, UTF-8 without BOM, finite full-precision values. Apply/record approved coordinated wording before implementation; no partial-data schema, v2 masks/counters or extra fields are introduced. Acceptance: staged semantic/structural tests, strict nonfinite/duplicate-key/version/type rejection.
- **FR-034**: Every released baseline site has nine wholly known windows: 24/72/168 h at December 1/8/15, 2026, 00:00 UTC. No partial v2 release/omitted unknown metric entry is selected. Early complete one-site sets are allowed; final three-site acceptance remains distinct. Index references exist and validate. Acceptance: nine complete windows per accepted site, eventual three supported entries, exclusion evidence and dangling-reference rejection.
- **FR-035**: Preserve available source/internal binary64 precision and independent pre-export/post-serialization checks. The user approved round-trip scientific JSON and display-only rounding, exact masks/counts, fraction/score tolerance 1e-12 and hours 1e-9, with 1e-10-degree interpolation/boundary audit. Unity science/parity uses double; rendering may cast. Shared old three-decimal wording must be coordinated/applied before implementation, not changed during planning. Acceptance: numerical-layer fixtures including 1/3, strict finite values and near-boundary/constraint/tie cases.
- **FR-036**: Independently validate index/site/opportunity structure and semantics: finite/ranged values, schema/status enums, IDs/references, times, array lengths, periodic visibility, reference metrics, mission coverage, qualification, scores/order, and counts. Reject invalid versions/statuses with readable diagnostics and failing exit status. Acceptance: deliberate corruption in every rule category is rejected.
- **FR-037**: Publish only a completely validated staged set after confirming destination ownership. No index may advertise absent/unvalidated files; failed/interrupted publication MUST preserve or restore a consistent previous release. Use approved scientific filenames for both stages; never overwrite another process's data/stubs. Acceptance: rejected-stage and interruption checks against a valid previous set.
- **FR-038**: Production science MUST never emit `ILLUSTRATIVE_DEMO`; earn `GEOMETRIC_ONLY` or `TERRAIN_AWARE_ESTIMATE` per site. Opportunity status conservatively reflects all represented inputs, never upgrading mixed quality. No new tier is introduced from the proposal. Acceptance: eligibility and mixed-quality cases with per-site evidence.
- **FR-039**: Trace results to exact coordinates/raw sources/terrain/transform/observer/range/step/parameters/version/checks. Bounds/original requested sets/exclusions/quality stay science owned; complete-only v1 carries no unknown flags, partial masks or hidden extension fields. Publication audit includes contract/profile/consumer/destination/barrier evidence. Acceptance: reproducible values, linked audit and refusal of unsafe partial interchange.
- **FR-040**: Use PASS/FAIL/NOT RUN records with evidence paths for the plan's S1/S3/S4/S5/S6/S7/S9 checks, plots, sensitivity, method, sources, and handoff. Distinguish synthetic fixtures from observations; skipped missing-cache checks are not passes. Acceptance: audit all delivery claims with no unexplained NOT RUN.
- **FR-041**: Disclose line-of-sight-only scope, DEM resolution/interpolation/gaps, height, time step, point-center model, and unquantified uncertainty. Use “environmentally promising candidate,” never guaranteed power/communications, safe landing zone, global optimum, or sub-sample timing claims. Acceptance: method/result/handoff language audit.
- **FR-042**: Handoff lists files, statuses, algorithm version, passed evidence, and limitations. Reproduce user-relayed integration issues from science-owned inputs before fixing this side; contract changes require user coordination. Acceptance: release record and reproducible parity procedure, with no direct agent messaging or Unity edits.

### Key Entities *(include if feature involves data)*

- **Verified Site**: Stable ID, exact observing coordinate, citation/verification, frame, ground-height reference, and terrain support; distinct from a fictional marker.
- **Ephemeris Dataset**: Target/physical observer, time semantics and coverage, paired direction arrays, exact source request, and raw evidence.
- **Terrain Product**: Acquired elevation raster, footprint, registration/frame/datum, resolution, uncertainty inputs, and provenance.
- **Horizon Profile**: Observer/range-dependent skyline, incomplete sectors, coverage evidence, and sensitivity record.
- **Analysis Scenario**: Site, landing UTC, duration, horizon model, timelines/intervals, five metrics, next losses, status, and limits.
- **Opportunity Request/Result**: Candidate/time grid, mission constraints, score policy, evaluated/rejected scenarios, best/earliest lists, and misses.
- **Release/Evidence Record**: Consistent versioned exchange set, reproducibility details, checks, limitations, and handoff.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All three sites have independently verified observing coordinates and comparison with Unity demo assets before release; zero unverified coordinates presented as supported science.
- **SC-002**: Each site has two real paired 4,465-row tracks at exact baseline/cadence; physical checks pass and first/middle/last comparisons agree within declared source-output precision.
- **SC-003**: All five synthetic skyline cases pass declared analytic/approximation tolerances; every terrain-supported site has real metadata/control-point/skyline evidence; zero missing sectors silently labeled complete.
- **SC-004**: Five mission metrics agree with independent calculations for all/none visible, interior/edge gaps, and a ten-cell fixture; nine reference windows per site are independently reproducible under the approved precision policy.
- **SC-005**: With three eligible sites, default Finder evaluates exactly 168 full missions; both policies, constraints, ties, empty results, and final arrival pass expected-result checks; no padded winners.
- **SC-006**: Every published file passes structural/semantic validation and every corrupted rule category is rejected; failed/interrupted release leaves no dangling/unvalidated index reference.
- **SC-007**: Two offline runs with identical inputs reproduce values/quality/order except declared run metadata; every scientific claim has executed evidence.
- **SC-008**: Each terrain-supported site reports height/range sensitivity; at least one 72-hour window reports 10-/30-minute sampling sensitivity; unavailable uncertainty components are explicit.
- **SC-009**: Specify/Clarify preserve shared files; Plan modifies only this feature and the explicitly approved verified constitution amendment. Both contracts, Unity feature/active pointer and other shared configuration remain unchanged. No implementation/data or claimed science-test PASS. All 30 decisions and actual G/N/U approval/application states remain traceable.

## Assumptions

### Authority and scope

1. Primary requirements: `backend/docs/1_PYTHON_SCIENCE_PLAN.md`, including S1–S11 order, checks, baseline, two-stage delivery, and Finder. Unity plan supplies integration context; proposal supplies rationale without importing optional services or broad geography.
2. Intended integration entry point is `docs/DATA_CONTRACT.md`; migrated `backend/docs/0_DATA_CONTRACT.md` is preserved. They are currently byte-identical. Neither is changed; future divergence blocks affected contract work pending user resolution.
3. Constitution v1.1.0 is now actually ratified from the verified newly prepared XI–XVII patch, preserving I–X and existing Unity governance. Historical XI–XXV text remains unavailable and is not reconstructed. See governance-ratification.md for exact mutation evidence.
4. Plan timeboxes/contest targets cannot bypass stage review. Latest delivery priorities make one-site real geometry/terrain P0 and three-site expansion/Finder/integration P1, retaining full feature scope. This stage creates design artifacts and ratifies approved governance only; no probes, science tests, runtime installs, ephemeris retrievals or DEM downloads. Historical narrower PGDA description remains superseded by D30/research.md.
5. Initially support only the curated catalog. Arbitrary-coordinate entry, new grids, finite-disk models, hazard screening, and expanded quality tiers remain deferred.

### Proposed corrections — neither contract copy is amended

Shared contract copies remain unchanged. Exact historical text is in [the original proposal](proposals/data-contract-amendments.md); [current approved changes/application gates](data-contract-amendments.md) select full precision and complete-only v1, including explicit accepted cohorts/exclusion reporting. Before implementation the owner applies/records coordinated wording and consumer acknowledgement; do not ask again for the approvals already given.

| ID | Observed issue | Proposed correction / decision | Acceptance evidence after approval |
| --- | --- | --- | --- |
| CR-01 | Migrated `backend/docs/0_DATA_CONTRACT.md` versus planned `docs/DATA_CONTRACT.md`; contents now match. | Confirm the root docs path as user-controlled canonical integration contract; retain migrated reference and agree revision/synchronization policy. No copying over either file now. | Canonical path/version/hash and divergence handling. |
| CR-02 | Primitive-only/no-dictionaries wording conflicts with nested objects/object arrays in examples. | Restrict parallel primitives to numeric time series; permit named typed nested objects and fixed-schema object arrays, banning arbitrary maps, root arrays, and arrays of arrays. Confirm with Unity owner via user. | Approved schema examples readable on both sides. |
| CR-03 | Three-decimal floats conflict with precise coordinates/flags/tolerances/decisions. | User approved full binary64 round-trip science JSON and display-only rounding; preserve shared copies now, apply coordinated text before implementation and confirm consumer double parity. | Exact masks/counts, fractions/score 1e-12, hours 1e-9, audited boundary/1/3 fixtures. |
| CR-04 | Missing terrain cannot safely become binary visibility. | Complete-only v1 selected; unknown analyses stay local, no terrain certification/ranking. Explicit accepted known cohort/exclusions allowed and reported. Historical v2 proposal deferred, not approved. | No fabricated binary unknowns; partial interchange refused; accepted/original cohort audit. |
| CR-05 | Plan calls −0.05° a 2 m sphere horizon; proposal also allows zero-plane fallback. | Use analytic dip in the same surface/observer datum, about −0.086937° at nominal radius/2 m. A zero-plane alternative must be explicitly chosen/labeled. Correct plan assumptions without silently changing schema. | Analytic and finite-resolution checks; coordinated fallback convention. |
| CR-06 | Specify observed fictional demo IDs; final Clarify check finds Site_1=shackleton_rim, Site_2=malapert_mountain, Site_3=connecting_ridge. ID alignment does not verify supplied coordinates/zero elevations. Shared pointer remains Unity. | Preserve science verification independently of frontend labels; report any sourced-point mismatch, leave Unity mapping to its owner, preserve shared pointer and stage on ownership conflict. | Current mapping record, independently sourced exact-point/height review and scoped write audit. |
| CR-07 | Endpoint-inclusive n=4465 plus next-step sample-cell wording implies extra coverage. | N proposes n−1 usable cells, a+m<=n−1, and half-open mission/search semantics; terminal flag is instantaneous display only. | End-at-stop passes; one-step-past fails; 4464 usable baseline cells. |
| CR-08 | Earlier Specify record described a narrower PGDA /90 page; current official page lists broad regional COGs. | Withdraw the inferred 87–90 S-only limitation. Retain per-site actual metadata/valid-footprint/range checks, additional coverage or explicit fallback; no chosen product or exact coordinate is invented (D29–D30). | Acquired raster metadata and exact-point/terrain mapping, not webpage existence. |

### Approved decisions and remaining coordination

1. **G** approved subject to verification; verification passed and constitution v1.1.0 was applied. [Evidence](governance-ratification.md) records retained Unity principles/governance and hashes.
2. **N** approved: full-precision binary64 scientific JSON, UI-only rounding, explicit tolerances and Unity double parity. Exact [coordinated wording](data-contract-amendments.md) must be applied/recorded by the user-owned contract process before implementation; no shared contract/C# edit in Plan.
3. **U** approved as complete-only v1. Incomplete results/candidates remain local/excluded with reasons, accepted known subsets explicit, fewer/zero winners honest. No partial schema is selected.

All 30 determinate clarifications remain traceable. Exact sourced observing points, acquired DEM datum/coverage, real queries and actual parity are future evidence obligations rather than guessed design values. Planning artifacts are complete and ready for `$speckit-checklist`/`$speckit-tasks`; implementation-entry contract/consumer gates are explicit. No tasks.md or Python implementation is created in Plan.
