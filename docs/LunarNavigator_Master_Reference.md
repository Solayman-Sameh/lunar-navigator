# LUNAR NAVIGATOR
## Master Project Reference and Continuity Handoff

**NASA Space Apps Challenge 2026 · Lunar Landing Site Navigator**  
**Prepared:** 15 November 2026  
**Purpose:** A single reference for the team preparing the project description and presentation, engineers implementing the application, and anyone resuming the project in a new conversation.  
**Reading guide:** Sections 1–13 describe the intended product and technical design. Section 14 is a factual engineering handoff based on the latest reported results; it is updated separately from the product specification.

> **Project in one sentence:** LunarNavigator is an interactive lunar south-pole planning application that combines NASA celestial-position calculations and terrain elevation data to evaluate sunlight and direct-to-Earth visibility at proposed landing sites and dates, compare mission windows, and explore the results in a photorealistic Unity simulation.

---

# PART I — THE PRODUCT

## 1. The challenge and the problem

The 2026 NASA Space Apps *Lunar Landing Site Navigator* challenge asks for an intuitive tool that helps mission planners, educators and the public compare lunar landing sites and dates by visualizing the apparent positions of the **Sun and Earth relative to the local horizon**.

At the Moon's south pole, the Sun often stays close to the horizon. Surrounding ridges and crater walls may block sunlight even if the Sun is geometrically above a perfectly flat horizon. This affects when solar-powered equipment might receive illumination. A similar horizon obstruction may conceal Earth and prevent direct line-of-sight communication without relay satellites. The geometry changes with time, so a delay in a planned landing date may alter a previously acceptable operating window.

**Our problem statement:** Teams need a fast way to determine how a particular landing place and arrival time affect illumination and Earth visibility over the **entire mission duration**, and to compare alternative site-and-time combinations when the mission plan changes.

The application is a **pre-mission environmental opportunity evaluator**. It does not autonomously guide a spacecraft during descent, guarantee safe landing, calculate a transfer trajectory, or certify solar-power or radio-link performance.

## 2. The proposed solution

LunarNavigator unites four functions in one Windows desktop application:

1. **Explore the Moon** using a full-screen 3D globe and four connected camera perspectives.
2. **Inspect a landing site** by selecting a location, UTC landing time and mission duration, then viewing Sun/Earth positions, terrain obstruction and operational windows.
3. **Find landing opportunities** by searching supported south-pole candidate sites and allowed landing times, filtering by mission constraints and returning up to three ranked scenarios.
4. **Compare alternatives** by changing the date, site or mission requirements, with charts and animation showing why a candidate qualifies or fails.

A recommendation is always a **site + landing timestamp + mission-duration assessment**, not an unspecified point on the Moon. Fewer than three recommendations, including zero, are valid outcomes.

## 3. Operating modes and exact user inputs

### 3.1 Mode A — Site Inspector

**Purpose:** Assess a site that the user has already chosen.

| Input | Meaning | Typical example |
|---|---|---|
| Site | Curated candidate or custom lunar coordinates where supported | Shackleton Rim |
| Latitude/longitude | Selenographic coordinates, south latitudes negative, longitude east-positive | Verified coordinates from site catalog |
| Landing date/time | Start of surface mission, in UTC | 2026-12-01 00:00 UTC |
| Mission duration | Number of hours of surface operations to evaluate | 72 hours |
| Optional requirements | Minimum Sun/Earth visible fractions and maximum outage lengths | Mission-specific values |
| Observation height | Height of observer/antenna above the local terrain | 2 meters, configurable in science engine |

**Output:** Sun and Earth azimuth/elevation versus time; local terrain skyline; visibility masks; times of visibility loss/recovery; Sun-visible fraction; Earth-visible fraction; simultaneous-visible fraction; longest uninterrupted darkness; longest direct-to-Earth blackout; and any uncertainty or limited-data warning.

A site can be inspected without specifying arbitrary pass/fail thresholds. When thresholds are provided, they classify whether the **environmental visibility conditions** meet those requirements.

### 3.2 Mode B — Landing Opportunity Finder

**Purpose:** Find promising combinations of candidate site and arrival time.

| Input | Meaning | Example |
|---|---|---|
| Earliest landing time | Earliest UTC timestamp considered for arrival | 2026-12-01 00:00 UTC |
| Search window | How far ahead arrival times are sampled | 14 days |
| Mission duration | Hours assessed *after each candidate arrival* | 72 hours |
| Search region/sites | Initially verified, supported south-pole candidates | Shackleton Rim, Connecting Ridge, Malapert region |
| Required sunlight fraction | Minimum share of mission time with the Sun visible | 80% |
| Required Earth visibility fraction | Minimum share with Earth geometrically visible | 70% |
| Maximum continuous darkness | Longest acceptable uninterrupted dark interval | 12 hours |
| Maximum communications blackout | Longest acceptable uninterrupted Earth-obscured interval | 6 hours |
| Ranking policy | Best suitability, or earliest qualifying landing | Best Overall / Earliest Qualifying |

All example percentages and thresholds are **illustrative user-defined mission requirements**, not NASA-wide engineering standards. Defaults are convenient starting points and must be editable if a true dynamic Finder is implemented.

**Search-window rule:** For a landing at time `L` and duration `D`, the program evaluates the mission interval `[L, L+D)`. The landing time must be within the allowed arrival search interval; the full mission may extend beyond that arrival interval, but the data coverage must include it. The application never silently shortens a requested mission window.

**Best Overall:** Reject candidates that fail hard requirements; then score the remaining scenarios and return up to three strong site/time alternatives. **Earliest Qualifying:** Prioritize the earliest qualifying arrival time, using suitability score as the secondary choice. The search resolution (e.g. every six hours) must be visible because shorter opportunities can be missed between samples.

### 3.3 Dynamic analysis: online-only calculation service

**Product decision:** LunarNavigator uses one **online-only** scientific workflow. The Unity Windows app sends each Site Inspector or Opportunity Finder request to a running Python **FastAPI** service. The service calculates or retrieves the requested Sun/Earth tracks, combines them with available terrain-horizon data, evaluates the requested mission duration and returns JSON results. The interface updates the visual scene, charts and ranking after the response.

- **Dynamic landing dates and durations:** The user may request any date/time within the validated operational range of the astronomy service. The backend fetches the relevant ephemeris range when needed; it does not require a pre-generated file for every calendar date.
- **Dynamic search constraints:** The user may change the mission duration, minimum visible fractions and maximum outage lengths. The backend recalculates the evaluation and ranking.
- **Dynamic sites/coordinates:** Horizons supports custom lunar observer positions, but **terrain-aware** analysis is available only where suitable, verified lunar elevation data covers the site and relevant horizon directions. Else, return clearly labeled `GEOMETRIC_ONLY` results where valid, or an actionable error; never claim missing terrain is known.
- **Server-side caching:** Previously calculated ephemerides and site skyline profiles may be reused to reduce response time and Horizons traffic. Caching is an implementation optimization, **not a separate offline mode**.
- **Connectivity:** The Windows app requires access to a running backend and, when data is not cached, the backend requires access to JPL Horizons. A disconnected app displays a clear service error. It must never silently replace scientific results with synthetic data.

**Deployment:** Prefer a hosted HTTPS FastAPI service so users launch the Windows application without manually starting Python. A locally running FastAPI server is a development/test option. The service, Unity HTTP integration, network error states and scientific responses must all be tested before describing dynamic online analysis as an implemented capability.

### 3.4 Geographic scope

The project focuses on the **lunar south pole** (roughly 80°S–90°S for initial regional exploration). Users can visually rotate the whole Moon, but scientifically supported results are limited to sites and regions for which the required data has been acquired and verified. A global-search toggle is not part of the initial validated scientific scope. The three starting site labels used by Unity are `shackleton_rim`, `connecting_ridge` and `malapert_mountain`; their **numerical coordinates must be independently checked** and consistent between science JSON and Unity site assets.

## 4. User experience and simulator layout

The 3D environment occupies **the entire screen**, not a smaller viewport placed between permanent sidebars. The main image is a detailed lunar globe floating in dark space. The scientific controls are a limited set of **floating, translucent, collapsible overlays**.

| Screen element | Role |
|---|---|
| Top-left identity | Project name, short subtitle and data-source status |
| Top-center app-mode selector | Site Inspector / Find Opportunities |
| Top-right compact icons | Sites, Layers, Settings, Help |
| Contextual site/mission popup | Coordinates/site, UTC landing, duration, constraints, Analyze |
| Contextual metrics popup | Visibility percentages, longest dark/communication gaps, qualification and caveats |
| Contextual skyline chart | Azimuth around horizon versus terrain elevation; Sun/Earth directions and unknown sectors |
| Bottom timeline | UTC time, playback, scrubbing, speed, Sun/Earth/both visibility bands, mission interval |
| Camera-mode controls | Global / Approach / Local Orbit / Surface Observer |
| Scientific status badge | `ILLUSTRATIVE_DEMO`, `GEOMETRIC_ONLY` or `TERRAIN_AWARE_ESTIMATE`; invalid data must show an error |

### 4.1 Four camera modes

- **Global Orbit:** View the complete Moon; mouse-drag orbit, zoom, inspect markers and select a candidate.
- **Cinematic Approach:** Smoothly fly from global view toward the chosen site using stable eased/logarithmic distance motion; allow replay, skip and safe interruption; finish in Local Orbit.
- **Local Orbit:** Circle and tilt around the selected area, zoom to inspect craters and ridges without crossing the ground.
- **Surface Observer:** Ground-anchored view from the configured observer height, with the local horizon, compass, Sun/Earth directions and optional scientific overlays.

A keyboard guide should list only implemented shortcuts; intended mappings include `1`–`4` for cameras, left-drag to orbit/look, mouse wheel to zoom, `F` focus, `R` reset, `Space` pause/play, `H` Help, `Tab` hide UI, and `Esc` back/dismiss. Mouse controls must not activate while dragging a UI slider.

### 4.2 Visual accuracy versus scientific accuracy

The global Moon should use real NASA lunar visual assets when available. Local high-detail foreground terrain may be illustrative and must not be described as a registered LOLA reconstruction unless it truly is. A scientific skyline calculated from DEM heights is a separate dataset; when the ground mesh is illustrative, show an explicit label distinguishing these two elements. Sun and Earth visual disks may use adjusted display distances/sizes, but their **directions** should reflect the selected ephemeris data. The absence of a substantial lunar atmosphere means no Earth-like horizon haze or sky scattering.

### 4.3 Intended presentation sequence

Start with the global Moon → choose a candidate marker → cinematic approach → local orbit/surface horizon → advance UTC time to reveal a Sun or Earth visibility transition → open Site Inspector/metrics → switch to Opportunity Finder and choose a ranked alternative. A demo should use genuine validated results when available, and clearly label synthetic values if not.

---

# PART II — SCIENCE AND DATA

## 5. Data sources and what they mean

### 5.1 NASA/JPL Horizons: ephemerides

**What it is:** JPL Horizons is a service that computes where Solar System bodies appear from specified observing locations and times. It is not a single fixed CSV dataset. A program issues an **HTTP GET API request**, and the service returns an ephemeris table. Our parser extracts the relevant columns and saves tabular/JSON data.

**Example observer request:** Target Sun (`COMMAND=10`) or Earth (`COMMAND=399`); observer based on custom lunar site coordinates (`CENTER=coord@301`, geodetic site triplet, with site longitude/latitude/altitude conventions verified against the manual); observer table; azimuth/elevation quantity; UTC start/end and sampling interval; no lunar atmospheric refraction. The authoritative API documentation is https://ssd-api.jpl.nasa.gov/doc/horizons.html and the manual is https://ssd.jpl.nasa.gov/horizons/manual.html.

**Expected parsed output fields:** `t_utc`, `az_deg`, `el_deg`, separately for Sun and Earth, then aligned to a common UTC sample grid. Actual requests/responses and parser interpretations must be preserved and validated. The core celestial dynamics are calculated by Horizons, not learned by a neural-network predictor.

### 5.2 NASA LOLA: topographic elevation

**LOLA** (Lunar Orbiter Laser Altimeter) is an instrument aboard the Lunar Reconnaissance Orbiter. Its measurements support lunar **digital elevation models (DEMs)**: georeferenced rasters (typically GeoTIFFs) where pixel values represent surface elevation in meters, along with mapping/projection metadata. Elevations are not directly a photorealistic texture; they are inputs for calculating skyline heights and optionally constructing 3D terrain meshes.

**Recommended sources:**

- NASA regional south-polar DEM collection: https://pgda.gsfc.nasa.gov/products/90 — wide-area elevation needed for the distant horizon.
- NASA high-resolution south-polar landing-site DEMs: https://pgda.gsfc.nasa.gov/products/78 — approximately 5-meter pixel products for supported sites, including slopes and elevation-uncertainty products. Use to refine the near-field surface and skyline, not as a replacement for distant terrain coverage.
- NASA south-pole DEM mosaic: https://pgda.gsfc.nasa.gov/products/81 — high-resolution regional coverage for an appropriate latitude subset, subject to file size and coverage.

**Terrain processing:** Open GeoTIFF with Rasterio/GDAL, inspect coordinate reference system, projection, lunar reference frame, pixel size, NoData mask and height reference. Transform lunar site coordinates to DEM pixels with pyproj. Compute surrounding terrain directions relative to the site. Track missing DEM coverage and interpolation uncertainty. Combine regional and detailed DEMs only if their vertical and horizontal references match.

### 5.3 Supporting visualization and map resources

- LROC QuickMap: https://quickmap.lroc.asu.edu/ — geographic inspection, imagery and verification support.
- NASA Moon Trek: https://trek.nasa.gov/moon/ — interactive geographic visualization and terrain context.
- NASA CGI Moon Kit: https://svs.gsfc.nasa.gov/4720 — globe texture, height/displacement and visualization resources for Unity.

These are supporting sources. The mathematical core depends on **Horizons ephemeris and appropriately verified DEM terrain**.

## 6. Scientific coordinate conventions

- Latitude: degrees, positive north; south-pole locations have negative values.
- Longitude: east-positive. Site JSON uses the normalized range `[-180°, 180°]`; source conventions must be converted explicitly.
- Azimuth: degrees clockwise from the local north direction, with 90° east; normalized into `[0°,360°)`.
- Elevation: degrees above the local horizontal plane, positive upward.
- UTC: ISO-8601 timestamps with `Z`; sample `i` occurs at `t0 + i × step_s`.
- Observer height: meters above the DEM terrain at the observing point; default 2 m for the first version.
- Astronomy and topography: confirm the Moon-fixed frame, longitude sense, reference surface and observer position are aligned before combining directions.

At latitude `φ` and east longitude `λ` (radians), a spherical local frame can be defined as:

$$
\begin{aligned}
\hat{u} &= (\cos\phi\cos\lambda,\ \cos\phi\sin\lambda,\ \sin\phi),\\
\hat{e} &= (-\sin\lambda,\ \cos\lambda,\ 0),\\
\hat{n} &= (-\sin\phi\cos\lambda,\ -\sin\phi\sin\lambda,\ \cos\phi).
\end{aligned}
$$

Given a unit vector `s` toward the body, local elevation and azimuth are:

$$
\operatorname{el}=\arcsin(\hat{s}\cdot\hat{u}), \qquad
\operatorname{az}=\operatorname{atan2}(\hat{s}\cdot\hat{e},\hat{s}\cdot\hat{n}).
$$

These equations are useful for converting between Horizons directions and Unity's coordinate vectors and for sanity checks. Horizons can supply observer azimuth/elevation directly; its coordinate conventions still need validation.

## 7. Terrain horizon and visibility mathematics

### 7.1 Local terrain skyline

For each azimuth `A`, the skyline `H(A)` is the greatest angular elevation of surrounding terrain in that direction, as seen from the observer. A practical exact-vector approach places each DEM pixel into a lunar Cartesian frame, computes the vector from observer to pixel, projects into the local east/north/up axes, and takes:

$$
A = \operatorname{atan2}(d_E,d_N), \qquad
h = \operatorname{atan2}(d_U,\sqrt{d_E^2+d_N^2}),\qquad
H(A)=\max h.
$$

Distance, Moon curvature, observer height, DEM NoData cells, and field of view all matter. For a *spherical, obstacle-free* Moon, the horizon dip from height `h_o` above radius `R` is approximately:

$$
\delta\approx-\sqrt{2h_o/R}\quad\text{radians}.
$$

At `R = 1,737,400 m` and `h_o=2 m`, `δ ≈ -0.08694°`. A purely geometric fallback is not terrain aware.

### 7.2 Visibility of Sun and Earth

For center-of-disk visibility at sample `t_i`:

$$
V_{\mathrm{Sun}}(t_i)=\mathbf{1}\{E_{\mathrm{Sun}}(t_i)>H(A_{\mathrm{Sun}}(t_i))+m_{\mathrm{Sun}}\},
$$

$$
V_{\mathrm{Earth}}(t_i)=\mathbf{1}\{E_{\mathrm{Earth}}(t_i)>H(A_{\mathrm{Earth}}(t_i))\},\quad
V_{\mathrm{both}}=V_{\mathrm{Sun}}V_{\mathrm{Earth}}.
$$

`E` is celestial elevation, `A` is azimuth, `H` is terrain skyline, and `m_Sun` is an optional clearance margin. A visible Sun is an **illumination opportunity**, not a calibrated solar-power production estimate. Earth visible means geometric direct-to-Earth line of sight, not a verified communication link.

If terrain is missing along a relevant azimuth, the visibility state is **unknown** until proven otherwise; the program cannot quietly count missing sectors as clear. The release-v1 exchange format should only publish unambiguous complete scientific windows, or explicitly label a geometric-only fallback.

### 7.3 Mission-level metrics

For a mission containing `N` samples separated by `Δt`:

$$
f_{\mathrm{Sun}}=\frac{1}{N}\sum_i V_{\mathrm{Sun}}(t_i),\quad
f_{\mathrm{Earth}}=\frac{1}{N}\sum_i V_{\mathrm{Earth}}(t_i),\quad
f_{\mathrm{both}}=\frac{1}{N}\sum_i V_{\mathrm{both}}(t_i).
$$

Additional outputs are `dark_max_h` (longest consecutive `V_Sun = 0` interval in hours), `comm_gap_max_h` (longest consecutive `V_Earth = 0` interval), and chronological visibility windows or countdowns. Time samples represent half-open intervals `[t_i,t_i+Δt)`; an analysis needs complete coverage through the mission end. Boundary refinement and apparent body radii can improve transition accuracy later.

## 8. Landing Opportunity Finder algorithm

1. Load a verified set of candidate sites within the selected supported region.
2. Enumerate arrival times across the allowed date range at the declared search step.
3. For each site/time pair, obtain or reuse cached ephemerides and a site-specific skyline; assess the full mission duration.
4. Reject scenarios with missing required data or failing mandatory sunlight/Earth/outage constraints.
5. Rank qualifying candidates according to the selected policy. A transparent illustrative suitability function is

$$
S=0.4 f_{\mathrm{Sun}}+0.3 f_{\mathrm{Earth}}+0.2 f_{\mathrm{both}}
+0.1\left(1-\min(1,D_{\max}/D_{\mathrm{mission}})\right).
$$

6. Return at most three recommendations, each including the site, landing UTC, mission duration, score, metrics, flags and explanatory reason. For no matches, return an empty list and explain which requirements were impossible to meet.

Scores are comparative for a **specific mission profile** and supported candidate set, not universal physical 'landing safety' probabilities. Search sampling and limited data coverage must be disclosed.

---

# PART III — SOFTWARE ARCHITECTURE

## 9. Exact implementation technology

| Layer | Technology / version | Role |
|---|---|---|
| Main simulator | **Unity 6.6 (`6000.6.3f1`)** | Windows desktop interactive 3D application |
| Rendering | **HDRP 17.6** | High-fidelity materials, lighting, lunar visual rendering |
| Unity logic | **C#** | Cameras, timeline, site markers, interaction, data loading |
| Unity interface | **UI Toolkit** | Fullscreen overlays, popovers, metrics and charts |
| Input | **Unity Input System** | Mouse and keyboard controls |
| Scientific runtime | **CPython 3.11.15 x64** (reported working venv) | Celestial and terrain computations |
| Network input | **requests** or Python standard `urllib` | Horizons API calls and error handling |
| Data frames | **pandas**, **NumPy** | Ephemeris tables, vector math, time-series metrics |
| Numerical utilities | **SciPy** | Useful interpolation, signal/time-window processing |
| Geospatial terrain | **Rasterio** and **pyproj** | GeoTIFF/DEM sampling and lunar map projection handling |
| Visualization/testing | **Matplotlib**, **pytest** | Scientific plots and automated verification |
| Exchange format | **JSON**, optional CSV | Python-to-Unity scientific data and audit outputs |
| Online scientific service | **FastAPI** with an ASGI server (e.g. Uvicorn) | Required intended backend for dynamic mission requests; not yet verified in the current build |

HDRP is for desktop-class rendering and not a direct Unity WebGL export pipeline. The primary deliverable is a **Windows standalone executable** plus project/demo links.

## 10. Online system architecture and request flow

**Primary online path:** Unity collects the selected site/coordinates, UTC landing date, mission duration and constraints → sends HTTPS JSON request to FastAPI → Python checks server-side caches and supported DEM coverage → obtains Sun/Earth ephemerides from JPL Horizons if needed → computes/loads the local terrain skyline → calculates visibility and mission metrics (or ranks opportunities) → FastAPI returns a versioned JSON response → Unity updates the celestial directions, surface skyline, timeline, metrics and recommendation cards.

**Server-side cache:** Reuse validated Horizons responses and terrain-horizon profiles for repeat queries. Cache entries carry their source metadata, algorithm version and coordinate/time parameters. This reduces API load but does not constitute a user-facing offline mode.

Planned FastAPI endpoints:

| Route | Input/output concept |
|---|---|
| `GET /health` | Health and data-service status |
| `GET /sites` | Supported verified sites and DEM coverage |
| `POST /analyze` | Site coordinates or ID; UTC start; duration; optional thresholds → time series and metrics |
| `POST /opportunities` | Earliest landing UTC, search span, mission duration, candidate sites and constraints → up to three ranked scenarios |
| `GET /data-coverage` | Supported geographic analysis areas, service time range and terrain coverage |

**Reliability and deployment:** Unity must display loading/progress, timeouts, unreachable-server errors, rate-limit messages and unavailable-terrain explanations. The service should validate requests, limit unbounded searches, use secure HTTPS and prevent fabricated results. It must not silently fall back to synthetic analysis when the server is down. A Windows executable still needs the FastAPI service deployed and accessible to complete live calculations. The live endpoint and Unity HTTP client remain **to be implemented and verified** according to the last reported engineering status.

## 11. Shared JSON data contract

**Authoritative project path:** `docs/DATA_CONTRACT.md`. This is an integration agreement; there may be a migration copy in `backend/docs/`, but one official version must be coordinated between both agents. Real results are under `Assets/StreamingAssets/analysis/`; synthetic files are named `stub_*.json` and carry `ILLUSTRATIVE_DEMO`.

**Current v1 data classes:**

- `index.json`: schema version; list of sites and filenames; optional opportunities file.
- `<site_id>.json`: site identity/coordinates; ephemeris time grid and Sun/Earth azimuth/elevation arrays; terrain horizon `horizon_el_deg`; incomplete-sector objects; integer visibility flags; reference metrics and scientific provenance.
- `opportunities.json`: original request, candidate site IDs, scoring rule, ranked lists, closest misses and counts.

**Current v1 conventions:** `sun_az_deg`, `sun_el_deg`, `earth_az_deg`, `earth_el_deg`; `t0_utc`, `step_s`, `n`; 360 skyline values at 1° increments; `incomplete_sectors` as objects `{ "az_from_deg": ..., "az_to_deg": ... }`; site longitudes east-positive; UTC; flags encoded as `0/1`. A 31-day time span sampled every 10 minutes contains **4,464 full ten-minute cells** and **4,465 boundary timestamps**; the implementation must not confuse them.

**Precision coordination:** The original v1 contract says round scientific floats to three decimals, but science clarification approved binary64 precision for calculations/JSON, with rounding only for display. Gemini reports C# scientific arrays have been upgraded to `double[]`. The **approved contract amendment has not yet been applied** in the latest Codex status. Before publishing real files, Python and Unity must validate the **same final, ratified version**, especially numeric precision, endpoint indexing and incomplete-data handling.

**Provenance levels:**

| Status | Meaning |
|---|---|
| `ILLUSTRATIVE_DEMO` | Synthetic; for interface demonstration only |
| `GEOMETRIC_ONLY` | Genuine Horizons directions but no validated terrain skyline |
| `TERRAIN_AWARE_ESTIMATE` | Genuine Horizons plus validated LOLA-derived skyline, with test evidence |
| `INVALID_DATA` (UI handling) | A delivered file was rejected due to contract or data validation failure |

Terrain-aware labels must never be assigned solely because a 3D terrain mesh happens to look realistic.

## 12. Software implementation structure and verification

**Single project root:** `C:\Users\user\Documents\Unity\LunarNavigator`. The architecture intentionally places the Python science engine and Unity project in the same repository for simplified data exchange.

```text
LunarNavigator/
  Assets/                     Unity scene, C# scripts, UI, NASA visual assets
    Scenes/LunarNavigator_Main.unity
    StreamingAssets/analysis/ JSON data read by Unity
  Packages/                   Unity dependencies
  ProjectSettings/            Unity project configuration
  backend/                    Python scientific code, venv, tests, cache, DEMs
  docs/                       Authoritative contract, science and Unity QA logs
  specs/002-python-science-engine/  Scientific feature spec, plan, tasks
  .agents/ and .specify/      Spec Kit workflow files
  Builds/Win64/               Standalone Windows distribution
```

**Science processing modules:** Horizons HTTP/caching/parser; site catalog and coordinate-frame conversion; DEM opening/projection; skyline generation; time-window visibility metrics; deterministic opportunity search; contract validation; CLI export; scientific tests and evidence.

**Unity modules:** four-camera manager, Moon/terrain rendering, site assets and markers, simulation clock, analysis repository/JSON deserialization, scientific parity utilities, skyline ring and chart, Site Inspector, Opportunity Finder, timeline, Help, Editor test menus, Windows build.

**Verification gates:**

1. Horizons request succeeded and complete original response was saved; actual columns/times parsed and compared.
2. Site coordinates are verified from authoritative references and match Unity site assets.
3. DEM CRS, pixel size, datum, bounds and NoData behavior are documented; observer and skyline geometry validated with synthetic wall/flat terrain tests.
4. Every time window has required Sun/Earth samples and no silently unknown terrain directions.
5. Python export validates against the final contract; Unity parses the real file and recomputes key metrics with comparable numeric results.
6. Four-camera, input, timeline, popover and Windows executable smoke tests pass with real data.
7. Data status and scientific limitations remain visible; no fabricated values or unsupported precision claims.

---

# PART IV — DELIVERY AND CONTINUITY

## 13. Competition deliverables and references

**Project format:** Unity HDRP Windows desktop simulator; it does not need to be a browser app. **Official project submission:** a publicly accessible project page with English project details, actual NASA data usage, tool/source references, and required disclosures. Provide **two separately accessible links**: (1) Final Project (repository/build/documented playable prototype); (2) Project Demo (video no more than 30 seconds **or** slide deck of at most seven slides). Public links should be checked without login permissions. Official closing time stated for the 2026 event: **Sunday 15 November 2026, 11:59 PM local time**.

**Suggested seven-slide story:** (1) Title and challenge; (2) Problem at the south pole; (3) Two application modes; (4) NASA Horizons + LOLA data; (5) Visibility equations and ranking logic; (6) Actual application demo and measured results; (7) Value, limitations, project link. Only show claimed results that have actually been validated in the delivered build.

**Primary references:**

- Official challenge: https://www.spaceappschallenge.org/2026/challenges/lunar-landing-site-navigator/
- JPL Horizons API: https://ssd-api.jpl.nasa.gov/doc/horizons.html
- JPL Horizons manual: https://ssd.jpl.nasa.gov/horizons/manual.html
- Regional south-pole LOLA DEM: https://pgda.gsfc.nasa.gov/products/90
- High-resolution south-pole LOLA site DEMs: https://pgda.gsfc.nasa.gov/products/78
- South-pole LOLA DEM mosaic: https://pgda.gsfc.nasa.gov/products/81
- LROC QuickMap: https://quickmap.lroc.asu.edu/
- NASA Moon Trek: https://trek.nasa.gov/moon/
- NASA CGI Moon Kit: https://svs.gsfc.nasa.gov/4720
- NASA Space Apps 2026: https://www.spaceappschallenge.org/2026/

## 14. Engineering continuity snapshot — as of the latest supplied reports

*This factual appendix exists for project continuity, not to be copied verbatim as an award claim. All items reflect agent reports and user-supplied screenshots, not an independent audit of the user's Windows machine.*

### 14.1 Reported Unity state

- Windows Unity project version **6000.6.3f1**, **HDRP 17.6**; `Assets/Scenes/LunarNavigator_Main.unity` exists and is loaded.
- A lunar globe, four camera modes, interactive site markers, fullscreen UI Toolkit overlays, Site Inspector, Opportunity Finder, a three-lane timeline, local skyline chart/ring and synthetic demo-data provider have been implemented, according to Gemini's reports.
- `Assets/StreamingAssets/analysis/` supports stub JSON and is intended to load real science JSON. Gemini reported correcting `incomplete_sectors` parsing to object records, enforcing 360 skyline values, and switching science calculations to `double[]`.
- Gemini reported **5,179** Unity assertions passing in the latest P0 contract gate and a playable Windows build at `Builds/Win64/LunarNavigator.exe`, with `Builds/LunarNavigator_Win64.zip` prepared earlier. Those counts are agent-reported and mainly reflect synthetic data and programmatic checks; they are **not** proof of real NASA-data parity.
- The user provided an Editor screenshot showing a detailed Moon/terrain and a functional-looking mission UI. Gemini was subsequently instructed to improve close-range terrain rendering, exposure, camera presentation and responsive layout; that work was still running in the latest message.
- Current demo badge in the screenshot: `ILLUSTRATIVE DEMO`. Visual and scientific demo quality must be reassessed after the final Unity pass.

### 14.2 Reported scientific backend state

- Python scientific work lives under `backend/` in the same Unity project root.
- CPython **3.11.15 x64** virtual environment exists. The user resolved third-party installation; Codex subsequently reported all eight scientific libraries importing and **T001 PASS**.
- Spec Kit documents exist in `specs/002-python-science-engine/`; 42 requirements, 30 clarifications, scientific constitution **v1.1.0** ratified, a technical plan and **27 implementation tasks**.
- Codex reported the Horizons parser/diagnostic client implementation and **60 tests passed**, but requests failed **before an HTTP response** in its execution environment. **No saved genuine Sun/Earth response pair, actual CSV or science plot was available** in its latest report.
- Task state in latest Codex status: `T001 PASS`, `T003 incomplete`, `T004 PASS`, `T005 diagnostic PASS (production gated)`, `T006 incomplete`. A verified production site catalog and some data-contract approvals remain outstanding.
- Codex was instructed to diagnose network/DNS/TLS/proxy issues, provide a reliable Windows terminal/manual browser acquisition fallback, retain actual original NASA responses, and stop after a real Sun/Earth data milestone. No evidence of completion of that follow-up has yet been supplied.
- **No verified terrain-aware numerical result or real JSON integration into Unity has been established from the supplied reports.**

### 14.3 Unresolved blocking decisions and immediate path

**P0 — unblock real NASA data:** Run the Horizons downloader from a regular Windows terminal if the Codex sandbox cannot connect. Save actual Sun and Earth original responses locally; parse them into aligned UTC tracks; inspect CSV and elevation plots. This can proceed before custom site verification but must remain labeled a diagnostic site test.

**P0 — coordinate + contract gate:** Check the verified coordinates of the three candidate sites; align Horizons observer/frame conventions with the DEM projection; apply the approved precision/endpoint/uncertainty contract amendment only after coordinating the Unity consumer. Do not silently edit one agent's copy.

**P0 — minimum usable science:** Obtain genuine Horizons Sun/Earth tracks and validate `GEOMETRIC_ONLY` visibility for one site and mission window; exercise the same output schema through the online analysis endpoint and Unity client when available. If LOLA data arrives, compute the skyline and upgrade to terrain-aware *only after* evidence-based verification. Previously generated JSON may be used as test fixtures, not as an alternate offline product mode.

**P0 — online calculations:** Once real Horizons acquisition and the science calculation engine work, implement the FastAPI `/analyze` endpoint and Unity HTTP client, test a user-entered date and mission duration end-to-end, then add `/opportunities`. A hosted service is the deployment target; the networked workflow is **not yet verified**.

**P0 — submission:** Prepare a public, accessible Windows project/build/repository link, a 30-second video or at most seven slides, the official English project description, actual NASA data credits, and test links in a private browsing window. Confirm formal *Submit for Judging*, not just draft save, before the closing time.

**P0 — preserve honest claims:** No label or award-description claim should exceed the dataset actually integrated and validated. If a live API or a terrain-aware model is not present in the delivered release, say what is implemented rather than presenting a planned feature as complete.

### 14.4 First message to use in a new conversation

> We are developing **LunarNavigator**, a NASA Space Apps 2026 lunar south-pole site/date evaluation simulator. Read the attached `LunarNavigator_Master_Reference_and_Handoff.md` completely, especially Section 14, and treat it as the project continuity baseline. The Windows Unity project uses Unity 6.6 (`6000.6.3f1`), HDRP 17.6, C#, UI Toolkit, and an in-repository Python 3.11 scientific backend. The agreed final deployment uses a hosted online FastAPI service; the standalone Unity program is a client and requires network connectivity for live calculations. It has two application modes (Site Inspector and Opportunity Finder) plus four cameras. JPL Horizons supplies Sun/Earth observer angles; LOLA DEMs supply the terrain skyline. **Latest blocker:** the Codex science engine's environment can run Python and parser tests, but real Horizons HTTP results have not yet been successfully saved in the shared files; the agreed final architecture is **online-only Unity → FastAPI → scientific engine**, with server-side caching but no offline user mode. Gemini's Unity UI is reported functional with synthetic demo data, while real data parity is pending. I want to continue from the most urgent uncompleted milestone, not redo planning. Ask me for the latest Codex/Gemini logs only if needed to verify changes after this snapshot.

---

**End of master project reference.**