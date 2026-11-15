# Phase 0 research — Python scientific engine

**Date**: 2026-11-15. Research/design only; no scientific request, raster download, dependency installation or engine test executed.

The installed Plan skill's research-agent step was performed with two read-only assignments: Horizons/time/frames/precision, and LOLA/skyline/coverage/tests. The primary agent inspected their findings and reopened primary sources. The following resolves architecture questions; actual acquired-data evidence remains a future gate.

| Topic | Decision | Rationale | Alternatives considered |
| --- | --- | --- | --- |
| Runtime/dependencies | CPython 3.12/64-bit, fallback 3.11; local venv and wheel-only GIS setup. Capture actual working versions after imports. | Matches baseline; avoids declaring untested package combinations or compiling GDAL under the deadline. | Host default/newer interpreter is not silently selected; speculative version pins are not reproducibility. |
| Architecture | Library plus thin argparse commands, dataclasses, direct filesystem manifests. | Small finite workload, independent of Unity/server availability. | FastAPI, database, repository framework, ML/PINNs and SPICE replacement add work without required benefit. |
| Delivery | One verified point -> real observations -> useful geometric JSON -> validated terrain -> other sites/Finder. | Latest user priorities supersede all-three-before-any-delivery assumptions. | Building all modules/sites before the first useful result risks delivering no science. |
| Ephemerides | Existing Requests-based JPL observer integration; explicit settings/header verification and exact raw retention. | Uses authoritative service and original plan. CSV heading spellings must be observed, not frozen from memory. | Migrated probe's “all floats” parser and zero altitude are diagnostic inputs, not accepted production behavior. |
| Cache | Canonical full request/observer/source identity hash, raw bytes, parsed content/hash and versioned manifest. | Named site/target files alone can incorrectly reuse different observer heights/models. | A new caching dependency is unnecessary. |
| Time and windows | UTC modern observer UT, fixed steps, 4465 instants/4464 cells; reject unsupported leap crossings. | Explicit endpoint semantics prevent extra fabricated coverage. | n usable cells, implicit local time, off-grid clamping and variable steps contradict approved semantics. |
| Frames/observer | Product-defined lunar CRS, ME realization evidence, exact ground+mast observer converted to source geodetic form. | A projection round-trip cannot establish physical frame/datum equivalence. | Earth CRS, assumed zero ground and reusing nominal-height tracks for actual terrain are rejected. |
| Starter DEM | Inspect the actual linked `LDEM_80S_80MPP_ADJ.TIF` first; page lists 181M and COG, south-polar stereographic, MOON_ME/DE421. Reassess actual valid coverage/datum before use. | Small regional starter covers the latitude class of candidate regions and fits the baseline download cutoff. | Listed 40 m product, 685M, is later refinement; 60S/240 m, 210M, is coarser; multi-GB 10/20 m products and immediate 5 m local processing delay the first usable result. |
| Terrain quality | Read actual masks/NoData/scale/offset/registration and source quality layers. A valid interpolated height is not automatically missing because direct return count is zero. | Distinguishes measured support uncertainty from absent data. | Neither pretend all pixels are direct 80 m observations nor declare all interpolated pixels unknown. |
| Horizon | Original Cartesian pixel binning, full precision, one-degree circular interpolation, explicit footprint/near-field/range coverage. | Retains baseline algorithm and counts curvature once. | Ray marching, flat-map distance as spherical range, and unflagged filled gaps are rejected. |
| Scientific JSON | Python binary64 and strict round-trip serialization; reject NaN/Infinity, duplicate keys and out-of-shape input. | Default JSON behavior alone is not strict scientific validation. | Three-decimal scientific fields and tolerance-only fixes are superseded by explicit user approval. |
| Uncertainty exchange | Complete-only v1, explicit accepted cohort/exclusion report; local incomplete evidence, no partial schema. | Latest user approval selects this route. | Historical optional v2 masks/counters are not approved for implementation. |
| Publication | Stage/validate, reader barrier, backups, atomic individual replaces, index last and rollback. | Fixed filenames lack multi-file snapshot atomicity with a concurrent unchanged consumer. | Index-last alone cannot guarantee consistency; versioned filenames require separate consumer/contract coordination. |

## Primary documentation inspected

| Source | Relevant verified documentation; limits |
| --- | --- |
| [JPL API](https://ssd-api.jpl.nasa.gov/doc/horizons.html) | Observer/CSV/time parameters and versioned envelope/error/result behavior. Current document version 1.3. No real response captured. |
| [JPL manual](https://ssd.jpl.nasa.gov/horizons/manual.html) | Site altitude/reference surface, modern UT/UTC, local apparent airless az/el and lunar ME/DE421 conventions. Actual center/model/observer output still needs inspection. |
| [PGDA regional products](https://pgda.gsfc.nasa.gov/products/90) | Actual listed 80 m starter, COG/projection/frame, companion count/effective-resolution/error products, Barker et al. 2023 and data DOI credits. File URL must be taken from the page, not constructed. No header/footprint/download tested. |
| [PGDA site products](https://pgda.gsfc.nasa.gov/products/78) | Region associations and source uncertainty/interpolation context. Exact observing points are not certified merely by region labels. |
| [LOLA RDR SIS](https://pds-geosciences.wustl.edu/lro/lro-l-lola-3-rdr-v1/lrolol_1xxx/document/rdrsis.htm) | Product-specific elevation/radius/scale distinctions. A 1737400 m reference in a PDS product is not permission to copy its IMG scale/offset into the selected PGDA GeoTIFF. |
| [Rasterio transforms](https://rasterio.readthedocs.io/en/stable/topics/transforms.html) | Pixel centers and affine/row-column conventions. Verify dataset registration to avoid a half-pixel error. |
| [pyproj Transformer](https://pyproj4.github.io/pyproj/stable/api/transformer.html) | Explicit coordinate order and product-derived transformations. Lunar datum equivalence remains separate evidence. |
| [Python JSON](https://docs.python.org/3.12/library/json.html) | Nonfinite values and duplicate keys are accepted by default; strict encoder/decoder checks are required. |

Unresolved architecture questions: **none**. Runtime acquisition obligations are deliberately not filled with assumptions: exact sourced site coordinates, metadata/height datum and validity of acquired rasters, observed Horizons headers/settings, actual leap/clock handling for requested dates, achieved memory/runtime, and independent consumer results. Each has a gate/test in the plan; failure retains earlier honest deliverables.

The shared contract still has its old prose. This is a known owner-controlled synchronization dependency with exact approved changes recorded, not a request to weaken precision or postpone planning. Implement only after the user-owned contract profile and consumer acknowledgement are recorded.
