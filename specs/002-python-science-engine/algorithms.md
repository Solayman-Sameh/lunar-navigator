# Detailed scientific algorithm design

Design only. The original science plan supplies module paths and pixel-binning method; approved clarification decisions and current user instructions supersede its rounding, approximate dip, endpoint and unknown-data assumptions.

## 1. Environment, catalog and observer

At setup verify 64-bit Python, binary64 Python float and NumPy float64, actual package imports and dependency versions. Do not infer compatibility from a migrated pyc file or a host interpreter. Lock versions only after successful smoke checks. Default unit work uses no network. GIS install failure holds terrain work without invalidating a tested geometric milestone.

Load three named catalog entries, but allow verified=false without placeholder numerical coordinates. For a production requested site require a finite sourced exact point, frame and known longitude convention. Convert 0…360 source longitude into [-180,180] once and record both; choose −180 consistently for the equivalent 180 meridian. Reject exact poles and warn for |latitude|>=89.95 degrees. Compare current assets read only; coordinate disagreement over 0.05 degrees is reported to the user. Do not use an asset's zero elevation as ground truth.

For spherical latitude phi and east longitude lambda (radians), use:

`up=(cos(phi)cos(lambda),cos(phi)sin(lambda),sin(phi))`

`east=(-sin(lambda),cos(lambda),0)`

`north=(-sin(phi)cos(lambda),-sin(phi)sin(lambda),cos(phi))`

Angles in persisted/CLI interfaces are degrees; trigonometry is radians. Verify unit length, mutual orthogonality and north/east orientation. For terrain surface radius R and valid ground height z0, observer O=(R+z0+h)*up. The same Cartesian observer must be converted to Horizons' actual reference geodetic surface. On verified matching spheres source altitude km=(R+z0+h−R_source)/1000. For an ellipsoid, use its actual radii/geodetic conversion rather than radial height substitution. If local source zenith differs, reconstruct the source unit direction from its az/el and rotate into this radial ENU before comparing a radial skyline. Preserve the apparent airless source meaning.

A geometric request explicitly uses a nominal R=1737400 m surface, z0=0 unless another verified reference height is selected, h=2 m. Label the actual-altitude assumption. Ground-height validation during a later terrain step changes observer/cache identity and can require new Sun/Earth requests.

## 2. Horizons acquisition, parser and cache

Use the existing requests approach, configured with explicit observer, lunar center, geodetic site, Sun/Earth targets, apparent airless quantity 4, CSV, modern UT/zero offset and fixed ten-minute time steps; request seconds in timestamp labels. Build encoded parameters, not shell URLs. Keep User-Agent `LunarNavigator-SpaceApps/1.0`, 90 s timeout, initial attempt plus three retries at 2/4/8 s. Respect bounded server Retry-After for transient 429/5xx; record retry evidence and fail after the declared acquisition budget, never silently change inputs/target/precision. No undocumented alternate host is invented.

Canonical cache key is a SHA-256 of deterministic request metadata including exact observer coordinates/datum/height/frame, endpoint/target, all scientific settings, start/stop/step, and cache format version. Each entry retains request manifest, raw structured response bytes/hash, raw result text, actual header map, parsed track and validation state. Cache-hit requires identity and retained validation/checksum agreement. A refresh creates a new retrieval record while preserving evidence of the old one. Never treat a filename such as horizons_site_sun.json as sufficient identity.

Parsing sequence:

1. Reject non-success HTTP and structured JSON error even with HTTP 200. Reject malformed JSON/nonfinite constants/duplicate keys in required envelopes; verify source signature and a supported parser-profile version (initially official API document 1.3).
2. Require nonempty result and verify target, lunar observer coordinates/radii/model, airless/time/step labels. Record exact printed precision and metadata. Unknown settings/frame/signature fail rather than being relabeled.
3. Require exactly one ordered SOE/EOE pair per response and inspect actual CSV heading meaning above SOE. Support explicitly documented/observed aliases; the manual's combined quantity-4 display heading is not a guessed fixed column index. Preserve blank flag fields with csv parsing.
4. Parse UTC timestamps with a declared supported source format, allowing documented seconds/fractions, never a locale-dependent “all floats” approach. Required n.a./missing/nonfinite angles invalidate the dataset. Normalize an actual 360-degree azimuth to 0; reject materially out-of-range values before normalization.
5. Pair Sun/Earth on exact instants only. Do not take an intersection to conceal missing rows. In seven-day chunk mode, equal duplicate boundary rows are deduplicated; conflicting duplicates fail and retain both responses.

Validate source/cadence before using data: expected `(stop−start)/step+1` instants, both exact endpoints, strictly monotonic unique times, exact integer 600 s differences and matching targets. Baseline n=4465. Treat unsupported leap insertion as an explicit unsupported interval, not a 600 s claim produced by naive timestamp arithmetic. Preserve time-scale/source prediction assumptions in metadata.

Run the original physical bounds and near-pole Sun-sweep tests, plot/inspect elevations, and compare matched first/middle/last results independently. Tolerance is half the last source decimal unit plus 1e-10 degree for conversion, not permission to compare different source revisions as identical. Investigate failed bounds without relaxing them to pass.

## 3. Terrain acquisition and coordinate validation

Take exact download links from the current PGDA listing. First inspect the starter selected in research.md; use ranged windows if supported and verify an actual opened/cached crop. If range reads fail, a small verified full product is acceptable within the plan's cutoff. Stop terrain setup on unavailable wheels or unusable data; do not compile GDAL or fabricate a file/URL. No product is considered valid by its page description alone.

Capture product and crop hashes/identity, retrieval source, driver, dimensions, dtype, units, affine, pixel registration, bounds, CRS WKT and all projection parameters, radius/frame/height reference, scale/offset, NoData/mask statistics and quality-layer coverage. Distinguish elevation above a radius from absolute radius. Apply file-specific units/scales once. PDS examples do not define the selected GeoTIFF's encoding. Reopen a persisted crop and verify metadata/validity preservation.

Build pyproj transformers from the actual lunar CRS with explicit longitude/latitude axis order. Never use Earth EPSG/WGS84 as a convenient proxy. Product-defined explicit lunar parameters are permitted only when their metadata establishes them. Frame compatibility and projected mapping are separate checks. Require round-trip latitude/wrapped longitude <=1e-6 degrees, Cartesian <=0.05 m, and projected error <=min(0.05 m,0.01 pixel). Independently sourced landmark error must fit its declared uncertainty plus one pixel. Numerical round-trip is not physical geolocation evidence.

Sample ground bilinearly using pixel-center registration; every nonzero-weight contributor must be valid. Mask NoData, nonfinite values, declared invalid range/quality flags and mapping failure. No universal arbitrary elevation cutoff. Direct-count=0 on a valid source-interpolated pixel is uncertainty evidence, not automatic NoData. A failed/unknown ground sample makes terrain unavailable; do not move the site or borrow a neighboring site's skyline.

## 4. Pixel-binned spherical skyline and coverage

Defaults are h=2 m, one-degree bins and 150 km surface range. Retain the baseline algorithm, with these executable steps:

1. Form a conservative projected rectangular read window enclosing the requested spherical distance disk. Account for product projection scale/edge distortion and a pixel buffer; verify enclosure from product-derived geometry rather than equating projected half-width with exact surface radius. A window clipped inside the required disk is incomplete, not silently sufficient.
2. Stream blocks of <=about two million pixels; reduce chunks if measured transient memory exceeds target. Convert pixel centers to spherical coordinates in the verified common frame. Calculate surface distance `R*atan2(norm(cross(up0,up_pixel)),dot(up0,up_pixel))`; keep the requested circular range rather than square corners. Do not use distorted planar distance as lunar surface distance.
3. For valid z form P=(R+z)*up_pixel. With d=P−O compute e=d·east0,n=d·north0,u=d·up0; az=atan2(e,n) modulo 360 and el=atan2(u,hypot(e,n)) in degrees. Cartesian geometry already includes curvature; subtract no d²/(2R) term.
4. Preserve the plan's exclusion inside twice nominal pixel size in the principal far-field maximum calculation. Separately inspect valid near-field support: if excluded cells can raise that skyline or local support is invalid/unbounded, flag affected directions incomplete. Subpixel topography remains a disclosed resolution limit, not a claim that missing nearby blockers do not exist.
5. Bin by `floor(az+0.5) mod 360`; bin k represents `[k−0.5,k+0.5)` modulo 360. Apply maximum reduction of el to each bin. Keep float64 maxima and actual observed support; no three-decimal rounding.
6. Coverage runs alongside maxima. Propagate missing/out-of-footprint cells' **angular footprints**, including corner/edge and wrap support, not just their center azimuths. Account for any unbounded relevant hole within the required disk. Valid maxima alone cannot prove a clear direction. Raster edges, inadequate crop/range, near-field exclusions and wholly empty bins retain explicit reasons. A direction ending before half range is necessarily incomplete; farther holes are also considered. Use conservative flags if precise footprint coverage cannot be established.
7. Short gap filling from valid neighbors within three bins may create a separate plot profile, never a scientifically known bin. Merge incomplete intervals as `[from,to)` with 0<=from<to<=360 and split zero-crossing sectors. No valid scientific profile/ground means no terrain analysis, not 360 invented finite measurements.
8. Cache raw maxima, known flags, sector/reason evidence and sensitivities by DEM/crop identity, point/frame/ground/mast, range, bins, validity settings and algorithm version. A change to any of these invalidates the profile cache.

Analytic sphere oracle: H=−degrees(acos(r/(r+h))), r=R+z_surface for a uniform spherical surface. At R=1737400 m,z_surface=0,h=2 m H=−0.08693657193373108 degrees; analytic tolerance 1e-8 degrees. A finite synthetic raster must include its tangent neighborhood (~2.64 km here), complete directional support and adequate sampling before comparison within 0.02 degrees. Wall oracle uses independently derived local components at surface angular distance alpha: n=(R+z_wall)*sin(alpha), u=(R+z_wall)*cos(alpha)−(R+z0+h). Compare atan2(u,n); retain 0.15-degree tolerance for the plan's approximate 10 km/1000 m-wall fixture. Raised-observer and rotated-wall tests must not mirror production projection code.

Real checks use distinct sourced Shackleton interior/rim control points (a rim site is not the crater interior), inspected skyline plots and h=0.5/2/10 m plus range=75/150 km sensitivity. Finite range and one-degree/DEM resolution remain model limits even when all within-model coverage checks pass; sensitivity is not proof that no farther obstruction exists.

## 5. Visibility, unknowns and mission metrics

For normalized azimuth A, k=floor(A), w=A−k, H(A)=(1−w)*H[k]+w*H[(k+1)%360]. At integer A only that entry is required. Any incomplete nonzero-weight support makes that target's sample UNKNOWN. For known samples Sun uses el>H+margin, Earth el>H, strict equality hidden. The margin is a declared scientific input, not a numerical tolerance; default zero.

Internal states are VISIBLE/HIDDEN/UNKNOWN. Missing source/coordinate/terrain data never become measured 0/1. Both is hidden if either is known hidden, visible if both known visible, otherwise unknown. Store true clearances and separate arithmetic-boundary diagnostics. Local incomplete analyses can report explicit whole-window bounds: unknown=0/1 completions for fraction lower/upper bounds, unknown=visible/hidden for minimum/maximum hidden-run duration. They are labeled mathematical bounds, not observations/confidence intervals or known-only means. They are not v1 output.

Known mission request uses a=(landing−t0)/step_s and m=D_seconds/step_s, both exact integers with a>=0,m>0,a+m<=n−1. Use samples a through a+m−1 for `[landing,landing+D)`. Terminal instantaneous state never contributes beyond stop. f_sun/f_earth/f_both use integer sums/m. Outages use a simple clipped run-length scan times step_s/3600; no interpolation manufactures sub-step timing. Visible runs have end-exclusive indices. Next-loss returns distinct ALREADY_HIDDEN, FUTURE_LOSS(index), NONE_WITHIN_COVERAGE, or UNAVAILABLE, never extrapolating beyond coverage.

For complete-only v1 require all exported n Sun/Earth states known and the relevant per-site checks passed. An analysis with required unknowns is incomplete and is **not certified or exported as terrain-aware**. Irrelevant warned sectors may remain in the profile if none of the exported directions needs them; no blanket claim of complete topographic coverage follows. A separately selected whole-scenario geometric run is permitted, with its own observer/status; do not fill terrain holes with its horizon.

## 6. Search, filtering, ranking and exclusions

Validate L0,W,Delta,D and thresholds first. L0 is aligned, W>0, Delta and D are positive whole sample multiples. Enumerate L0+j*Delta<L0+W. Each temporal mission must fit source coverage or reject the request; do not silently shorten the arrival search. Default produces 56 arrivals/site, last at 2026-12-14T18:00:00Z, 432 cells/72 h and 168 requested scenarios across three eligible sites.

Use cumulative integer sums for fractions and direct run scans for gaps; this workload needs no advanced optimization. Apply every enabled hard rule before score: f_sun>=min_sun, f_earth>=min_earth, dark<=max_dark, gap<=max_gap. All equalities pass; fractions must lie [0,1], outage limits nonnegative. Record illustrative preset 0.30/0.40/36h/48h and rationale/distributions without choosing a favored winner.

Known qualifier score is 0.4*f_sun+0.3*f_earth+0.2*f_both+0.1*(1−min(1,dark_max_h/D_h)). Never divide by a possibly zero constraint. Best Overall sorts descending full-precision score, then earlier UTC, then ordinal ASCII ID, top three distinct site/arrival scenarios. Earliest Qualifying selects the earliest qualifying sampled UTC, then score/ID at that exact UTC only, top three. No fuzzy tie comparator or re-sort of displayed rounded scores.

Unknown scenarios are unevaluable/excluded, not known constraint failures; no score. Return fewer/zero qualifiers honestly. For known failures choose up to five closest misses by number of failures, then summed fraction shortfall plus outage excess/D_h, then UTC/ID. Retain every failure name.

Maintain original requested set and requested=evaluated+unevaluable; evaluated=rejected+qualifying in local audit. Under the latest approval, an explicit `--allow-exclusions` search/release accepts a smaller **complete** cohort with full exclusion reasons printed/written and handed off; `--all` without this flag fails rather than silently shrinking. V1 candidate_sites/counts describe the accepted cohort, not a claim that excluded original sites were evaluated. With no eligible complete sites, produce local report/empty recommendations and withhold a misleading scientific index/opportunity file. With eligible known sites but no qualifiers, export valid empty lists and known misses. Geometric and terrain cohorts remain separate model requests.

## 7. Strict serialization, independent validation and handoff

Serialize scientific numeric values from float64/Python float without intermediate decimal rounding, shortest-round-trip or 17 significant digits; use strict finite checks and encoder nonfinite rejection. Decoder rejects nonfinite constants, duplicate keys, invalid root/types/array shapes/version/IDs/ranges. A root object with typed object arrays is permitted; no new exchange fields or partial v2 profile. UTF-8, no BOM. Display formatting is isolated and never feeds science.

Validate scientific arrays and fixtures **before** serialization, then deserialize actual staged bytes and independently reconstruct known visibility, metrics, qualification/scores/order, references and counts **after** serialization. Independent validation uses small reference formulas/counting distinct from optimized paths, not an exporter asserting its own success. Missing-cache or unexecuted checks remain NOT RUN.

Budgets: integer masks/counts exact; fraction and score absolute 1e-12; hours 1e-9; interpolated angle 1e-10 degrees. For independent C# replay require exact flags where abs(clearance)>1e-10 degrees. Enumerate/audit all near-boundary samples; use stored authoritative flags for shared metrics, never hide mismatches as PASS or change scientific strict > by adding epsilon. These are arithmetic budgets, not NASA data accuracy. Presentation rounds only for humans to three decimals, nearest ties-to-even, normalizing negative zero and displayed azimuth 360 to 0.

Stage a consistent complete accepted set below backend/data/staged/run_id. Independently validate all references plus nine baseline reference windows/site and optional Finder. Require approved applied contract hash/profile and Unity consumer acknowledgement, destination ownership and a reader reload/startup barrier. Back up previous owned science bytes, atomically replace each scientific file, write index last, verify committed hashes, then permit one reload. On error restore previous set before releasing barrier; retain raw/staged failure evidence. No barrier/ownership means no publication. Per-file atomicity is not multi-file snapshot atomicity.

Handoff includes accepted/original/excluded sets, exact filenames/hashes, per-file status, observer/frame/model assumptions, approved profile/version, source/dependency/algorithm identity, all evidence and limitations, numerical boundary samples and shared test vectors. Unity owner performs double-array/parity/frame integration and rendering conversion; Python agent never edits C# or sends another agent direct production messages.
