# Scientific acceptance and test matrix

**Current execution status**: every future engine/data/Unity test below is **NOT RUN**. This stage verifies design/artifact consistency and approved governance only. Implement tests during the separately authorized implementation stage; do not create hollow tests mirroring assignments or infer PASS from a URL/file existing.

## Numerical budgets

| Check | Acceptance |
| --- | --- |
| Flags/integer cell/run counts | Exact agreement for known masks and outside the independent-replay arithmetic band. |
| Fractions and score | Absolute difference <=1e-12. |
| Duration/outage conversions | Absolute difference <=1e-9 hours. |
| Interpolated angle/parity math | Absolute difference <=1e-10 degrees. |
| Cross-engine near-boundary | abs(clearance)<=1e-10 degree is enumerated/audited separately; no discarded mismatch PASS and no epsilon change to strict scientific >. |
| Exact spherical dip | <=1e-8 degree against the analytic reference. |
| Sampled spherical DEM | <=0.02 degree with complete support, tangent range and adequate sampling. |
| Approximate wall fixture | <=0.15 degree, with independent spherical oracle also reported. |
| Projection numerical round-trip | Angular <=1e-6 degree wrapped away from poles; Cartesian <=0.05 m; projected <=min(0.05 m,0.01 pixel). |
| Independent source/control point | Same source/settings/precision; source half-last-decimal unit plus 1e-10 degree for az/el; geographic controls within documented source uncertainty plus one pixel. Unknown uncertainty cannot create a fabricated accuracy PASS. |

## Automated synthetic/unit/contract tests

| ID | Input / negative control | Required observable result | Requirements |
| --- | --- | --- | --- |
| T01 | Catalog with unverified/null coordinate, duplicate ID, degrees outside bounds, exact/near pole | Reject production unverified/bad point; preserve supported near-pole value with warning; longitude wrap/provenance exact. | FR-005–006 |
| T02 | Read-only asset values versus independently sourced point | Matching ID alone does not verify coordinate/ground; >0.05-degree mismatch reported. | FR-005–006,042 |
| T03 | Recorded real-format header plus blank flags/recognized aliases; numeric decoy columns | Parse named az/el/time correctly without taking every float or fixed guessed index. | FR-007,009 |
| T04 | HTTP-200 error, unknown API signature, missing/multiple SOE/EOE, n.a., NaN or wrong body/site/frame | Reject with precise source diagnostics and retain evidence. | FR-007,009–011 |
| T05 | Cache with changed mast/ground/coordinates/target/frame/dates/algorithm and corrupted checksum | Miss/reject stale cache; identical offline identity reuses bytes without requests; refresh retains prior evidence. | FR-010,039 |
| T06 | Missing/off-grid/duplicate timestamps, conflicting chunk boundary, Sun/Earth mismatch | Reject, never intersect away missing rows; equal duplicates deduplicate; exact first/end/cadence. | FR-008–011 |
| T07 | Endpoint-inclusive 4465-instant baseline; final fitting and one-cell overflow windows | 4464 usable cells; end-at-stop valid, any beyond stop invalid; no terminal extra interval. | FR-008,023,025 |
| T08 | Off-grid/local time, zero/negative/fractional-sample duration, unsupported leap insertion | Reject rather than snap/clamp/shorten; six cells=one hour. | FR-007–008,023,025 |
| T09 | Analytic common-frame basis and observer conversion; deliberately swapped longitude/axis/height datum | Orthonormal north/east/up; exact source/terrain Cartesian observer; swapped conventions fail. | FR-012,015–017 |
| T10 | Synthetic georeferenced raster with known cell centers, affine/scale/offset, height/radius variants | Correct ground and pixel mapping; negative controls detect half-pixel/double-scale/double-radius errors. | FR-013,015–016 |
| T11 | Complete zero-height spherical DEM including tangent region; h=0.5/2/10 m | Exact dip and sampled tolerances; no fixed −0.05-degree 2 m oracle; convergence recorded. | FR-016–018 |
| T12 | Independent 1000 m/10 km north wall and rotated east wall | Correct peak at north/east and plan tolerance; rear profile remains reference within 0.05 degree. | FR-016–018 |
| T13 | Raise observer relative to a fixed distant wall; change range/bin support | Lower wall elevation for raised observer; cache identity changes; geometry counted once. | FR-016–018,021 |
| T14 | NoData wedge including wrap, tiny gap, invalid bilinear ground, all-invalid raster | Knownness remains false after plot filling; half-open split sectors correct; missing ground/whole profile unavailable. | FR-019–022 |
| T15 | Far gap beyond half range, outside-DEM crop, near-field potential blocker and thin hole spanning cell edge | Conservative angular footprint/range flags, not false completeness from valid centers/maxima. | FR-014,018–021 |
| T16 | A=0/90/359/360/−1, integer azimuth, strict equality and nonzero solar margin | Periodic interpolation/equality correct; incomplete nonzero-weight support UNKNOWN; integer only uses required sample. | FR-024 |
| T17 | Hand-worked ten cells: S=1100111011, E=1010110011, step=600 s | f_sun=.7,f_earth=.6,f_both=.5; longest darkness and Earth gap both 2 cells=1/3 h. Exact masks/counts, scalar tolerances above. | FR-025–027 |
| T18 | All visible/hidden, one interior gap, gaps touching either edge, sliced mission | Fractions/run lengths independently counted and clipped, not borrowed from outside window. | FR-026 |
| T19 | Already hidden, future loss, no loss to stop, unavailable and three-state joint masks | Distinct next-loss states; no extrapolation; local unknown bounds explicit, never known-only mean. | FR-020,026–027 |
| T20 | Default 14-day/6 h/72 h grid; last candidate; insufficient trailing data | 56 arrivals/site, 168 for three; last complete mission evaluated; temporal insufficiency rejects request. | FR-028 |
| T21 | Each hard constraint alone fails, equality passes, zero allowed outage, invalid thresholds | Filter before score, all reason names; no divide by zero/relaxed winner thresholds. | FR-029–030 |
| T22 | Earlier adequate candidate, later better score, exact score ties, one site at two dates | Best and earliest differ correctly; score/UTC/ordinal ID ties deterministic; earliest never fills with later time. | FR-030–031 |
| T23 | Zero/one/two qualifiers and tied known misses | No padding; up to five misses use explicit normalized violation and every failure; counts reconcile. | FR-029–032 |
| T24 | Unknown/unverified/outside-DEM candidates and explicit accepted subset | Never score/certify unknowns; original requests/exclusions retained, known subset explicit; no v2 fields; zero eligible yields local report only. | FR-003,020,022,028,032–034,038 |
| T25 | Round-trip high precision coordinates/angles/1/3/near-threshold score; rounded display negative zero/azimuth | Serialized numbers retain binary64; pre/post checks independent; display-only rounding never changes scientific decisions. | FR-033,035,039 |
| T26 | NaN/Infinity JSON, duplicate keys, wrong root/types/enums/IDs/array lengths/horizon count/flags | Strict rejection with field diagnostics, not default permissive parser behavior. | FR-033,036 |
| T27 | Incorrect flags/metrics/scores/order/counts, dangling index, path traversal, wrong schema | Independent semantic validator rejects every category using accepted site bytes; exact reference windows checked. | FR-034–036 |
| T28 | Stage failure, ownership conflict, missing reader barrier, injected commit/rollback interruption | Prior validated release survives; no unvalidated advertised set; keep stage on gate failure; no stubs/.meta touched. | FR-003,037,042 |
| T29 | Same retained inputs/version twice offline, generation time changed | Scientific values/status/order/evidence identities repeat; only declared run metadata varies. | FR-010,039–040 |
| T30 | Evidence missing/skip, synthetic fixture misidentified as real, unsupported scientific language | NOT RUN never upgraded; no claimed safety/power/link certification; method/status/handoff truthful. | FR-002,040–042 |

## Real-data and consumer checks

| ID / evidence row family | Required actual action | Gate / priority |
| --- | --- | --- |
| R01 / S1-columns | One verified point; successful actual Sun and Earth responses; preserve headers/request/observer/site provenance and manually inspect raw rows. | P0 source proof; no fake PASS from migrated probe. |
| R02 / S3-bounds-sun, bounds-earth, sun-azimuth-sweep, tracks | Full single-site baseline count/physical checks/plot inspection and independent matched first/middle/last comparison. | P0 geometric data. Sweep applies only to near-pole sites per original test. |
| R03 / S7-contract-site | Independent scientific validation then staged-byte validation and nine single-site reference windows. | P0 useful GEOMETRIC_ONLY JSON. |
| R04 / S4-dem | Actually opened acquired raster/crop; exact product/link/hash/CRS/datum/registration/units/scales/masks; site and horizon-disk support. | P0 real terrain; a page is not metadata proof. |
| R05 / S5-real-shackleton, control-points | Independent recognizable/source-supported controls, including distinct crater interior/rim where applicable; numerical/physical checks separated. | P0 terrain coordinate gate; do not call rim coordinate the interior. |
| R06 / S5-real-plots, synthetic cases | Run T09–T16 and inspect actual profile/sector plots; actual observer/source compatibility established. | P0 terrain certification for that site only. |
| R07 / S5-sensitivity, S6-sens | h=.5/2/10 m, range75/150 km per supported site and one 72 h 10-/30-minute timeline comparison. Report max differences and limitations. | P0 validation; no arbitrary sensitivity number promoted to guaranteed physical uncertainty. |
| R08 / S6-unit, S7-contract-site | Ground-matched real tracks, terrain windows, independent metrics and strict serialized set validation. | P0 terrain-aware mission result, no required unknown support. |
| R09 / S7-contract-other-sites, S9-finder | Verify others independently; 168 default scenarios only when all eligible; log known/excluded sets, distributions/constraints/policies/misses. | P1 full feature, preserve useful earlier one-site release if not achieved. |
| R10 / Unity-double-parity, frame and reload evidence | Unity owner executes shared synthetic and retained-real vectors, double-array/math checks, direction/frame orientation, masks/metrics/time boundary comparison and deliberate reload. | P1 integration. Python agent supplies evidence only; no Unity edits. |
| R11 / S10,S11 | Clean-shell regeneration from pinned dependencies/retained inputs, source/credits/method/status audit, release receipt and all unexplained NOT RUN resolved or honestly scoped out. | Each delivered stage documents actual level; final all-feature claim needs all required evidence. |

## Requirement and success-criterion coverage

| Requirement range | Design/test coverage |
| --- | --- |
| FR-001–004 | Plan gates/order, T24/T28/T30, ratification record and protected-file audit. |
| FR-005–012 | T01–T09, R01–R02, observer/cache source evidence. |
| FR-013–022 | T10–T16/T19/T24, R04–R08, coverage/uncertainty state rules. |
| FR-023–027 | T07–T08/T16–T19, R03/R08, exact cell/metric semantics. |
| FR-028–032 | T20–T24, R09, explicit accepted cohort/exclusions. |
| FR-033–039 | T24–T29, R03/R08/R10, exchange and serialized semantic validation. |
| FR-040–042 | T30, R01–R11, status/evidence/handoff and no other-agent writes. |
| SC-001 | T01–T02/R01/R09: three independently verified exact points ultimately, never placeholder coordinates. |
| SC-002 | T03–T08/R02: exact paired real baseline and physical/source checks per supported site. |
| SC-003 | T09–T15/R04–R07: five geometry/gap cases plus actual terrain/control evidence. |
| SC-004 | T16–T19/T25/T27/R03/R08: independent five metrics and nine complete reference windows/site. |
| SC-005 | T20–T24/R09: correct default scenario count, both policies, empty/tied/excluded cases. |
| SC-006 | T26–T28: every invalid category rejected and safe publication/rollback. |
| SC-007 | T05/T29/R11: offline deterministic regeneration and executed evidence. |
| SC-008 | T13/R07: sensitivity and explicit unquantified error components. |
| SC-009 | Actual stage write audit: no implementation, shared pointer/contracts/Unity unchanged; only explicitly approved constitution ratification is the planning-stage shared exception. |

All runtime PASS entries must point to actual evidence files in the science report. Mark real tests explicitly (real_data/network) so default offline unit success cannot imply source/terrain/consumer validation.
