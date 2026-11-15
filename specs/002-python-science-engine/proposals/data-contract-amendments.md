# Shared data contract — exact proposed amendments

**Status**: FOR OWNER APPROVAL ONLY. Neither contract copy is edited.
**Targets if approved**: `docs/DATA_CONTRACT.md`, then a deliberately synchronized migrated reference as directed by the owner.
**Basis**: contract SHA-256 `6565A6DBDA2A03E86E71779AC9A1F48EB6CE233AA9E034EFF9A04E9B62FA263B`.
**Related decisions**: [D01–D30 and approval packages](../clarifications.md).

N is the recommended coordinated v1 semantic clarification with no field-name/shape changes. U is optional partial-result interchange and requires a version-2 reader. Neither is authorization to edit Unity files. Consumer validation/math changes must be handled by the Unity owner via the user.

## N1 — canonical location and ownership

After “Place this file at `docs/DATA_CONTRACT.md` in the repo.” add:

> `docs/DATA_CONTRACT.md` is the canonical user-controlled integration reference. `backend/docs/0_DATA_CONTRACT.md` is a preserved migrated reference. Before contract-dependent publication compare versions and content hashes; divergence requires user resolution, never automatic overwrite. Any approved revision must identify which copy is synchronized and retain the user-approved text.

After the ownership table add:

> Path assignment does not authorize overwriting a file actively owned by another process. Science stages and validates outputs before confirming exclusive destination ownership. Unity owns `.meta` files. Agents never change each other's active feature selection, specifications, Git branches, or shared configuration. Conflicts are reported through the user.

## N2 — structured JSON wording

Replace this exact convention:

> Arrays are **parallel arrays of primitives**. No dictionaries, no top-level JSON arrays, no nested arrays (Unity `JsonUtility` cannot parse them).

with:

> Time-series arrays are parallel one-dimensional arrays of primitives. A file has one root object. Named nested objects and arrays of fixed-schema objects are allowed, including `sites`, `incomplete_sectors`, `reference_metrics`, result lists, and `closest_misses`. Arbitrary key/value dictionaries, root arrays, and arrays of arrays are excluded by this project profile. Object fields must match the agreed typed schema. Internal Python catalogs are not Unity exchange documents.

This makes the prose consistent with existing examples. No arrays are flattened and no examples are silently rewritten.

## N3 — numerical precision and parity (recommended)

Replace:

> Floats rounded to 3 decimals. No NaN or Infinity anywhere (use the `incomplete_sectors` list for missing terrain).

with:

> Preserve available source precision and binary64 scientific calculations. Authoritative JSON numeric values must preserve binary64 round-trip values (shortest round-trip decimal or 17 significant digits); do not round scientific coordinates, observer heights, angles, margins, thresholds, metrics, or scores to three decimals. Three-decimal rounding is presentation only, nearest decimal with ties-to-even; normalize displayed negative zero and display rounded azimuth 360 as 0. No NaN or Infinity. Missing terrain is quality information, never an invented measured zero.

Add to section 6:

> Validate full-precision science before export and independently reconstruct serialized authoritative data after export. Both layers require exact integer flags/counts, fractions within 1e-12, and durations/outages within 1e-9 hours. Python/C# parity calculations use binary64; rendering may cast to float. Periodic interpolation agreement tolerance is 1e-10 degrees. Strict scientific visibility remains clearance > 0 with equality hidden. Independent replay must agree on flags for abs(clearance)>1e-10 degrees. Samples within that arithmetic boundary band must be enumerated and audited separately; use authoritative stored flags for shared metrics, never suppress mismatch counts or call unresolved discrepancies PASS. The band is not a terrain uncertainty, physical accuracy estimate, or visibility margin. Scores agree within 1e-12; validate constraints from unrounded metrics and preserve exported result order for ties. Display-rounding validation uses its own 0.0005 quantization bound and never recomputes scientific decisions from display values.

The science plan's 1e-6 test and Unity plan's 1e-3 check must be coordinated with this approved policy during planning; neither plan file is edited now. A 1/3 fraction is validated as a full-precision value, while a displayed 0.333 is tested only for correct rounding.

**Alternative, not recommended**: retain three-decimal scientific exchange only after separately approving canonical quantized-data reconstruction, scientific/wire difference reporting, displaced-coordinate treatment, scalar tolerance, and refusal of opportunity publication when quantization changes qualification/order. Widening tolerances alone does not resolve angle-derived flag disagreement. N recommends the full-precision replacement above; alternatives remain unselected until the owner responds.

## N4 — UTC, endpoints, and interval rules

Replace the time convention with:

> All interface times are explicit UTC in `YYYY-MM-DDTHH:MM:SSZ`. Ephemeris instant i is `t0_utc+i*step_s`; do not store timestamp arrays. Use verified observer UT/zero-offset source output; for the supported modern dates Horizons UT means UTC. Fixed sampling requires explicit leap-insertion handling; unsupported intervals crossing a leap insertion are rejected rather than forced into uniform spacing.

In section 4.2 replace the parenthesized reference-metric sample/window introduction and window-fit bullet with:

> The baseline stores n endpoint-inclusive ephemeris instants. Usable mission cells are i=0…n−2, covering `[t_i,t_i+step_s)`. The final row is the stop endpoint, not an extra cell. A mission covers `[landing,landing+duration)` with integer m=duration_h*3600/step_s>0 and aligned index a>=0; require a+m<=n−1 and mission end no later than requested/acquired stop. Never clamp or truncate. The n visibility entries include the final instantaneous state for display, but only the n−1 usable cells contribute to baseline metrics. Baseline n=4465 therefore covers 4464 cells/31 days.

Add to section 4.3:

> Earliest landing L0 is inclusive. Candidate arrivals are `L0+j*landing_step_h` strictly less than `L0+search_days*24h`. Landing start is ephemeris-grid aligned; arrival step and mission duration are positive whole numbers of ephemeris steps. Mission duration is independent of arrival-search length and may extend beyond it, but every candidate's full mission must fit source coverage. Insufficient trailing coverage rejects the request, never silently drops the last arrival. Baseline 14 days/6 h gives 56 arrivals/site, final arrival 2026-12-14T18:00:00Z, and 168 requested scenarios for three sites.

## N5 — observer, frames, and horizon

After the coordinate/observer conventions add:

> Record lunar frame realization and height datum, not only latitude/longitude. The observer is ground position plus the declared height above it. For matching spheres, source altitude km is `(R_dem+ground_height_m+observer_height_m−R_source)/1000`; the simplified `(ground_height_m+observer_height_m)/1000` requires equal radii. An ellipsoidal source requires geodetic conversion of the same physical Cartesian observer. Unsupported datum/frame conversions block terrain publication. Geometric fallback may explicitly use a nominal R=1737400 m sphere and 2 m mast when ground altitude is unknown; disclose that model assumption and query that observer, not an allegedly verified actual elevation.

> Local azimuth is clockwise north/east/south/west, elevation relative to the declared local horizontal. The plan's spherical local up is radial, not slope normal. Transform differing source local bases before comparison. Verify actual Horizons center orientation/radii/site output and DEM frame/registration; generic ME labels or a good projection round-trip alone do not prove frame equivalence.

> The reference-sphere horizon is `H=−acos(r/(r+h))` in degrees: −0.08693657193373108 degrees at r=1737400 m,h=2 m. A constant −0.05 degree value is not this model's correct 2 m dip. Terrain geometry already incorporates curvature; do not subtract curvature twice.

Add to the horizon constraints in section 4.2:

> At azimuth A normalized to [0,360), k=floor(A), w=A−k, interpolate `(1−w)*H[k]+w*H[(k+1)%360]`. At integer A only that entry contributes. A nonzero-weight incomplete support prevents a known visibility conclusion. Sectors are half-open `[from,to)`, with 0<=from<to<=360; split wraparound into `[from,360)` and `[0,to)`. Numeric gap fills retain coverage flags. Full-range, internal-hole and near-field coverage matter; ending before half range is a mandatory warning trigger, not permission to omit farther unbounded gaps.

## N6 — complete-only v1 and search semantics

Add to sections 3/4.2:

> `data_status` describes source/model tier, not completeness. TERRAIN_AWARE_ESTIMATE requires acquired validated terrain and passed required checks; gaps never become flat terrain automatically. Internal visibility is VISIBLE/HIDDEN/UNKNOWN. Under v1's binary-only schema, publish only site datasets with known Sun/Earth visibility for every exported sample. Keep incomplete analyses and explicit bounds in scientific evidence; do not encode UNKNOWN as 0, 1, or NaN. Existing `incomplete_sectors` can describe profile gaps that do not affect exported known samples; a full profile need not be certified complete merely because all requested celestial directions are known. Otherwise U is needed for incomplete interchange. GEOMETRIC_ONLY is a separately requested whole-scenario idealized horizon, not a silent patch over missing terrain.

Add to section 4.3:

> All enabled hard requirements must pass (`>=` minimum fractions, `<=` maximum outages) before ranking. Thresholds are finite, user-configurable and illustrative, never universal NASA standards; initial example preset 0.30/0.40/36h/48h remains labeled. Keep the existing score. Best Overall sorts descending full-precision score, earlier UTC, then ordinal site ID; allow different arrivals at the same site. Earliest Qualifying minimizes landing UTC first, then ranks qualifying sites at that exact arrival by score/ID. Do not pad empty or short lists.

> Candidates with UNKNOWN required samples are unevaluable, not known constraint failures or closest misses. Keep explicit quality cohorts; never silently mix geometric and terrain confidence. Under v1 publish a Finder file only when the whole requested cohort is evaluable. `evaluated_count` counts evaluated scenarios, `rejected_count` counts known hard-constraint failures, and their difference is the number of qualifiers before the top-three limit. Closest misses sort by failure count, then total violation `max(0,min_sun_fraction−f_sun)+max(0,min_earth_fraction−f_earth)+max(0,dark_max_h−max_dark_h)/duration_h+max(0,comm_gap_max_h−max_comm_gap_h)/duration_h`, then arrival/ID; return up to five and retain every failed constraint name. Do not divide by a possibly zero outage threshold.

## N7 — verified site mapping

Replace section 5's claim that the IDs match Site_1..3.asset with:

> The supported scientific catalog keys are `malapert_mountain`, `shackleton_rim`, and `connecting_ridge`. Current frontend assets use these IDs, but matching asset labels and candidate coordinate values do not verify exact observing points or ground elevations. Science owns exact-point/source/frame/ground-height verification; Unity owns frontend mapping. A region/product association is not point certification. Unverified points are unavailable for production science. Outside-DEM sites are terrain unavailable until additional verified coverage exists; do not move/clamp the site or reuse another skyline. Explicit geometric fallback requires declared reference-height assumptions. Report mismatches through the user.

## U — optional smallest safe incomplete-data schema

**Recommended default**: retain complete-only v1 above until the owner explicitly needs incomplete results in Unity. This requires no structural change. Rich incomplete results remain available in scientific evidence.

If partial interchange is required, two additive primitive mask arrays are the minimum sample-level correction. They are shape compatible with structured JSON but **not semantically safe for old readers**, which could ignore them. Therefore approve a version gate together with the fields:

1. Set `schema_version` to **2** on the entire release index, site files, and opportunity file. Old readers must reject version 2; a reader that ignores versions is not a compatible consumer and blocks release. Do not mix versions silently in one set.
2. Add `visibility.sun_known` and `visibility.earth_known`, both integer 0/1 arrays of length n. 1 means the corresponding binary visibility is known; 0 means UNKNOWN, and the corresponding legacy visibility slot is finite placeholder 0 with no visible/hidden meaning. All consumers, metrics, and validators must check knownness first. For known entries retain the existing strict visibility formula. Unknown entries do not undergo binary-formula assertion or mission scoring.
3. Reference metrics include **only wholly known windows**; omit affected windows rather than write invented point estimates. Quality-aware clients show omitted windows as incomplete. Explicit lower/upper bounds can remain in scientific evidence without adding new numeric metric fields in this minimal correction.
4. Add opportunity integer fields `requested_count` and `unevaluable_count` and an object array `unevaluable`, whose entries have `site_id`, `landing_utc`, and primitive `reasons` string array. Require requested_count=evaluated_count+unevaluable_count and unevaluable_count=length(unevaluable), with rejected_count<=evaluated_count. Known qualifiers=evaluated_count−rejected_count. Rejected/closest-miss lists exclude unknown scenarios.
5. Keep existing data_status enum values. A terrain-estimate tier with unknown samples is explicitly incomplete; unsupported source/observer/mapping prevents a dataset, rather than earning a misleading tier. Unknown is not a new status that replaces provenance.

Illustrative **shape only**, not computed science or a full contract document:

```json
{
  "schema_version": 2,
  "visibility": {
    "sun_margin_deg": 0.0,
    "sun_visible": [1, 0],
    "earth_visible": [0, 0],
    "sun_known": [1, 0],
    "earth_known": [1, 0]
  }
}
```

The second sample is unknown for each body, not an observed hidden sample. A missing mask or wrong length in version 2 is invalid, never default-all-known. No version-2 export occurs until both owners agree on schema and the frontend owner demonstrates version/mask handling. This proposal does not add fields to the authoritative contract now.
