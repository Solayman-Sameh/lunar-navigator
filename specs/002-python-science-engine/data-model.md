# Phase 1 data model

**Precision**: Python float/NumPy float64 with verified binary64 environment; integer counters/flags; timezone-aware UTC at interfaces and exact integer seconds relative to t0 internally. Store no rounded scientific fields. No entities below are implemented in this stage.

## Scientific entities and invariants

| Entity | Fields / identity | Validation and relationships |
| --- | --- | --- |
| SiteRecord | id, name, lat_deg, lon_east_deg, coordinate_frame, point_source, verified, notes; ground datum/elevation source if established | ID in three-site catalog; finite latitude strictly between poles; longitude normalized [-180,180] with original convention retained; exact observing point citation required before verified=true. Asset label/zero elevation cannot verify a point. |
| ObserverModel | site identity/coordinate hash, physical Cartesian position, local ENU, ground/mast heights, source datum/radii/geodetic coordinates, model kind | Geometric nominal-sphere and real-terrain observers are different identities unless positions match. h>=0; default 2 m. Ellipsoidal conversion must preserve physical position and local basis meaning. |
| EphemerisRequest | observer, target 10/399, endpoint, parameter map, t0/stop/step, time/frame/settings, cache format version | Canonical request hash; range fits supported dates/time handling; target-specific raw evidence. Refresh selects a new retrieval, never silently replaces old provenance. |
| RawResponse | request hash, raw bytes/hash, retrieval UTC, HTTP/API signature/version, result/header evidence | Exact external bytes retained. Validated parsing does not overwrite raw evidence. Error/unknown signature not accepted as data. |
| EphemerisTrack | request/response identities, UTC instants or int64 offsets, float64 az/el arrays, source/frame/precision metadata | Strict unique monotonic fixed cadence; expected endpoints/count; all finite; az normalized [0,360), el [-90,90]; no scientific use of missing required sample. |
| PairedTracks | site/observer, Sun and Earth track IDs, t0/stop/step/n | Target timestamps and observer/settings compatible; baseline n=4465, cells=n−1=4464. Never intersect/drop mismatching rows to conceal a gap. |
| TerrainProduct | exact product/URL/hash or range-crop identity, CRS/frame/radius/datum, affine/registration, size/dtype/scale/offset/units, bounds/masks, auxiliary quality | Acquisition and metadata inspected; actual height/radius meaning established; persisted crop reopened with same georeference. Counts/effective resolution/error inform uncertainty, not automatic truth labels. |
| TerrainSupport | site-product mapping, projected/pixel coordinate, valid bilinear ground sample, requested disk, valid/missing masks, range/near-field assessment | All nonzero interpolation weights need valid heights. Out-of-footprint/mapping failure makes this pairing unavailable. A located point is not proof of full skyline coverage. |
| HorizonProfile | site/observer/product hash, algorithm version, 360 float64 maxima, 360 known flags, incomplete sectors/reasons, max_range_km, height, bin convention, sensitivities | Scientific unknown values remain masked; optional numerical plot fills are separate. Cache key includes ground, mast, range, frame, DEM, binning and validity rules. No nonfinite/placeholder values enter exchange as scientific values. |
| VisibilitySeries | paired tracks/profile or explicit sphere model, Sun/Earth state arrays, both states, clearances, margin, arithmetic-boundary audit | States internal {VISIBLE,HIDDEN,UNKNOWN}; strict > for known values. All exported n instantaneous body flags must be known in complete-only v1. Terminal state is display only for baseline metrics. |
| MissionRequest | site, landing UTC/index a, duration seconds/hours, m cells, horizon model, optional thresholds | a>=0,m>0,a+m<=n−1; duration and start grid aligned; no silent clamp. Distinct from arrival search duration. |
| MissionResult | request, five metrics, intervals, next-loss states, quality/limits, source/evidence IDs | Exact scalar metrics only when required samples are known; local incomplete result may contain labeled bounds. No incomplete result is certified as a terrain-aware deliverable. |
| SearchRequest | original requested sites, L0,W,Delta,D, four enabled hard thresholds, explicit model/cohort, exclusion policy | Positive W/Delta/D; L0/Delta/D grid rules; every requested candidate mission needs temporal coverage. Scientific site/terrain exclusions are explicit, not temporal truncation. |
| CandidateResult | site/arrival/duration, evaluable flag/reasons, metrics, all constraint failures, score if qualified | No score for unknown candidates. Failure of any enabled rule rejects before ranking. Known and unevaluable states remain different. |
| SearchAudit | requested/evaluated/rejected/qualifying/unevaluable counts, original and accepted IDs, exclusions, all metrics, policies and constraints | requested=evaluated+unevaluable; evaluated=rejected+qualifying. Preserve per-scenario reasons locally; original set never overwritten by an accepted subset. |
| ScientificRelease | run ID, approved contract profile/hash, accepted complete site files, index, optional complete-cohort opportunities, checksums, evidence/dependency report | Stage separate from publication. Index includes only validated complete files. Under v1 extra audit fields stay in science evidence, not secretly added to wire schema. |

## State transitions

Site: UNVERIFIED -> VERIFIED only with point/frame/source evidence; terrain support is independently UNAVAILABLE/INCOMPLETE/SUPPORTED. A verified point can lack terrain without moving its coordinates.

Source data: RETRIEVED -> PARSED -> VALIDATED, with FAILED evidence retained at any stage. Parsing is not validation, and a cached file is not validated merely because it exists.

Scenario: REQUESTED -> EVALUABLE or UNEVALUABLE -> QUALIFYING or REJECTED for evaluable cases. UNKNOWN inputs never take the REJECTED-by-known-constraint path. Separate explicit geometric requests can be evaluated even when terrain is unavailable; their model/status is preserved.

Release: STAGED -> VALIDATED -> READY_FOR_HANDOFF -> PUBLISHED only after contract/consumer/destination/reload gates. Failed staging leaves prior release alone. Publication failure uses backed-up validated set/rollback under a consumer barrier; no unverified partial release is announced.

## Exchange view

Retain the current v1 root object, site/ephemeris/terrain/visibility/reference_metrics objects, index sites object array and opportunity arrays. Emit only the selected complete site subset for an incremental release. A final complete feature baseline has all three and nine windows/site; an earlier one-site release is useful but does not claim all-three acceptance.

`ephemeris.n` remains number of instants. `visibility` n entries remain integer 0/1; `horizon_el_deg` contains 360 finite values and no scientifically relevant unknown support for exported times. An unused incomplete profile sector can remain warned; a missing direction actually needed by a Sun/Earth sample blocks that terrain release. No v2 masks, partial reference windows or unevaluable-counter fields are introduced into v1.

Finder's wire `candidate_sites` is the **explicit accepted complete cohort**. With `--allow-exclusions`, excluded sites/scenarios and the original requested set are reported in run/status evidence and the handoff; evaluated/rejected fields retain their existing definitions over the accepted cohort. A v1 file cannot present itself as an exhaustive evaluation of excluded sites. No unsafe geometry/terrain cohort mixture or winner padding occurs.

Serialization is strict UTF-8 without BOM, one root object, typed fields/arrays, no arbitrary maps/root arrays/nested arrays, NaN/Infinity/duplicate object keys rejected, round-trip numeric values preserved. Source/internal manifests may use richer structured objects because they are science-owned evidence, not Unity exchange.
