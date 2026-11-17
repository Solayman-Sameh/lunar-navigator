# Scientific backend reference

**v1.1.0 application · GEOMETRIC_ONLY · 17 November 2026**

The Python service computes sampled Sun/Earth visibility against a nominal lunar
sphere. It does not evaluate local terrain obstruction or landing safety. Source
code is the behavioral authority. The [HTTP contract](../backend/docs/API_CONTRACT.md)
and [legacy Unity static-file contract](DATA_CONTRACT.md) describe distinct formats.

## Components

| Source | Responsibility |
| --- | --- |
| `backend/science/models.py` | Requests, observer conventions and UTC/grid validation |
| `backend/science/horizons.py` | Acquisition, response parsing, cache identity and integrity |
| `backend/science/coordinates.py` | Coordinate conversions and explicit horizon model |
| `backend/science/visibility.py` | Aligned time series, visibility, intervals and outages |
| `backend/science/sites.py` | Source-cited regional reference catalog |
| `backend/science/finder.py`, `opportunities.py` | Covering source acquisition, filters and ranking |
| `backend/science/api.py` | FastAPI endpoints, request validation and result provenance |
| `backend/run_backend.py` | Packaged loopback service and launcher session lifecycle |

Runtime pins are in `backend/requirements-runtime.txt`. The full development/tool
environment is in `backend/requirements.txt`. Installed raster/GIS libraries are
not evidence of an implemented terrain-aware scientific model.

## Sources and coordinates

Horizons targets are Sun **10** and Earth **399**. Requests use custom lunar
observer `coord@301`, UTC, east-positive longitude and apparent airless azimuth/
elevation. The current parser accepts supported UTC headings with minute or second
precision, locates named azimuth/elevation columns and validates the expected grid.

The three reference centers in `backend/data/sites.json` are Shackleton Rim,
Connecting Ridge and Malapert Massif. Coordinate citation verification is distinct
from terrain registration, ground elevation and touchdown certification. Custom
Inspector points are explicitly unverified. Malapert's internal catalog ID remains
`malapert_mountain` for compatibility.

## Horizon and visibility

The default observer is 2 m above a sphere of radius 1,737,400 m. The spherical dip
threshold is H = -acos(R / (R + h)) in degrees, approximately -0.08694 degrees at
2 m. A target is visible only when its center elevation is **strictly greater**
than H. Both-visible requires Sun and Earth visibility at the same instant.

Calculations use Python binary64 floating-point values. Display rounding happens
in the interface. Target-center visibility does not model a partially visible
solar disk, terrain penumbra or any atmospheric correction.

## Time model and metrics

Default cadence is ten minutes. An aligned series with n boundary instants defines
n-1 complete cells. States are held from each boundary to the next over half-open
intervals. The terminal sample adds no extra mission duration. Requests must fit
complete acquired coverage and align to source cells; the service does not invent
missing coverage or extrapolate scientific results.

Visible-cell count divided by total mission-cell count gives `f_sun`, `f_earth`
and `f_both`. Longest consecutive hidden-cell count times the cadence gives
`dark_max_h` and `comm_gap_max_h`. Returned intervals and transition brackets are
cadence-limited, rather than interpolated physical contact times.

## Opportunity search

The Finder acquires one covering Sun/Earth pair per selected site and evaluates
overlapping mission windows locally. The arrival search interval is half-open.
Minimum Sun/Earth fractions and maximum darkness/Earth interruptions are hard
filters. Qualifying scenarios receive:

`Q = 0.4*f_sun + 0.3*f_earth + 0.2*f_both + 0.1*(1-min(1,dark_max_h/duration_h))`

Best Overall ranks by score; Earliest Qualifying restricts selection to the earliest
qualifying sampled arrival. Ties are deterministic. At most three distinct
site/arrival pairs are returned. Weights and requirements express application/user
preferences, not NASA approval criteria. Sampling can miss better arrivals between
tested times. Closest misses in an empty search are not qualifying recommendations.

## API and operating limits

The service exposes `GET /health`, `GET /sites`, `POST /analyze` and
`POST /opportunities`. The application release is 1.1.0; the API/OpenAPI version
remains 1.0.0, algorithm identifier 0.1.0 and response `schema_version` 1. These
are separate compatibility identifiers.

- Complete service coverage: 2018-01-01 through 2030-01-01 UTC.
- Inspector mission maximum: 744 hours.
- Finder search span plus mission duration: at most 744 hours.
- Cadence: integer 1–60 minutes, with exact cell alignment.
- At most 10,001 source instants per target and 1,000 Finder scenarios.
- Up to three catalog sites; custom Inspector coordinates exclude the exact poles.

Combined limits can reject a request even if its individual fields fit their ranges.
Health confirms service identity and limits; it does not probe NASA connectivity.

## Provenance, cache and errors

Scientific responses identify `GEOMETRIC_ONLY`, the request, source window, raw
SHA-256 hashes, retrieval metadata and model limitations. Cache identity includes
the source request. Reuse requires matching authentic validated data. Corrupt,
incomplete, unavailable or mismatched data produces an explicit failure; a demo is
not substituted as a successful scientific result.

In packaged use, logs/cache/results live under `%LOCALAPPDATA%\LunarNavigator`.
The launcher binds its owned backend to loopback on an ephemeral port and verifies
its PID/session health before starting Unity. Development defaults to port 8000
and storage under `backend/data/api/`. Runtime results are not source-controlled.

## Reproducible evidence

`backend/tests/fixtures/horizons/real-diagnostic/` contains a small genuine retained
NASA pair for an unverified diagnostic observer at (-89.5, 0), 2 m height,
December 1–4, 2026 UTC: 73 hourly samples per target. Original response bytes and
their hashes are preserved. Receipt/output paths are made relative for portability.
The fixture README explains its scope. It is not an offline runtime database.

Real-data regression tests validate provenance, grids, physical bounds, derived
CSVs and the diagnostic mission metrics. Other tests use explicitly synthetic
fixtures to exercise edge cases. Run the suite as documented in the root README.
The [release validation record](RELEASE_VALIDATION.md) distinguishes executed tests
from live-network and clean-machine claims. Frozen HTTP examples remain under
`backend/docs/api-examples/`; they are examples, not current predictions.

## Interpretation limits

No validated local DEM obstruction, slope/hazard assessment, power system, thermal
model, RF link budget, receiving ground station or relay network is implemented.
The displayed local terrain and enlarged celestial visuals are illustrative.
The nominal sphere plus mast height is not verified local lunar ground height.
Future ephemerides inherit the source's retrieval-time UTC/EOP assumptions, and
cache integrity does not prove the prediction is the newest available.

## References

- [Horizons API](https://ssd-api.jpl.nasa.gov/doc/horizons.html)
- [Horizons observer/time conventions](https://ssd.jpl.nasa.gov/horizons/manual.html)
- [NASA Lunar Surface Data Book](https://ntrs.nasa.gov/citations/20250002845)
- [CGI Moon Kit](https://svs.gsfc.nasa.gov/4720/)
- [Rendering attribution and scope](ASSET_ATTRIBUTION.md)
