# LUNAR NAVIGATOR
## Comprehensive Project Proposal and Technical Implementation Blueprint

**NASA Space Apps Challenge 2026 | Lunar Landing Site Navigator**  
**Document status:** Proposed product and technical design — not a claim of completed implementation  
**Prepared:** 15 November 2026  
**Audience:** New team members, developers, science/data contributors, reviewers, and competition judges  
**Working product:** Unity 6.6 (6000.6.3f1) HDRP (Windows desktop) + Python scientific engine + optional FastAPI service

> **In one sentence:** Lunar Navigator helps people select, evaluate, and compare potential landing locations and arrival times near the Moon's south pole by showing when the Sun and Earth are visible above the real local terrain horizon, then exploring the results in an immersive 3D Moon simulator.

---

## 1. Executive Summary

Selecting a lunar landing location is not simply a matter of choosing a flat-looking patch of ground. A mission must also have enough sunlight to support power and thermal operations and a usable view of Earth for direct communication. Near the Moon's south pole, the Sun often stays very close to the horizon. A nearby ridge can hide it for hours or days; the same terrain can obstruct Earth. A location that appears favorable at the exact arrival moment may be unsuitable over the following mission days.

**Lunar Navigator** is a proposed desktop mission-planning and education application addressing that problem. It takes a landing site and time interval—or searches a supported set of candidate sites and arrival times—and converts astronomy and terrain data into clear visibility timelines, mission-level metrics, and ranked opportunities. It presents this information through a high-quality, fullscreen Unity simulation: a realistic Moon, a cinematic approach to a chosen candidate, a local orbit view, and a terrain-level observer view.

The scientific system uses **JPL Horizons** for time-dependent Sun/Earth positions and **LOLA-derived digital elevation models (DEMs)** for local horizon obstruction. The computation does not need a machine-learning prediction model: the relevant celestial geometry is already modeled by established ephemerides. Our software's contribution is the combination of location, time, real terrain, mission requirements, and intuitive visualization. [S1–S4]

**Primary outcome:** a planner can understand not just *where* a candidate is, but *when and for how long* the candidate has useful illumination and direct-to-Earth visibility. The tool should also explain why alternatives qualify or fail.

**Important limitation:** this is an *environmental opportunity assessment*, not a certified landing-safety system, spacecraft trajectory optimizer, autonomous guidance system, solar power simulator, or validated communications link-budget solver.

### 1.1 Project at a glance

| Area | Agreed direction |
|---|---|
| Challenge focus | Lunar south pole; rapid comparison of landing sites and dates |
| Primary user | Mission planner; also educators and interested public |
| Operating modes | A. Site Inspector; B. Opportunity Finder with an “Earliest Qualifying” search option |
| Main search output | Up to three ranked **site + landing time** combinations; fewer or zero if constraints cannot be met |
| Required science | Sun/Earth azimuth and elevation + terrain-horizon calculation + mission-window metrics |
| Scientific data | JPL Horizons + NASA LOLA DEMs; site coordinates with provenance |
| UI | Fullscreen immersive Unity Moon with lightweight floating controls |
| Cameras | Global, Cinematic Approach, Local Orbit, Surface Observer |
| Initial scientific geography | Verified sites near the lunar south pole, not unvalidated Moon-wide coverage |
| Backend | Python and numerical/GIS libraries; FastAPI optional for live request/response |
| Deployment | Windows HDRP demonstration, public project link, independent competition demo link |
| Core decision | Correct and explainable science takes priority over unsupported “AI accuracy” claims |

---

## 2. What the NASA Challenge Actually Requests

The official 2026 challenge asks participants to create an **intuitive tool that compares candidate lunar landing sites and dates**, visualizing the Sun and Earth relative to the local horizon to assess potential sunlight and direct-to-Earth communication windows. It names pre-mission schedule shifts, comparison of alternative sites and dates, and accessibility for different skill levels as motivating problems. The description permits web apps, mobile apps, interactive simulations, or other formats. [S1]

The challenge **does not require** (a) astronauts to operate the tool in an emergency during descent, (b) a machine-learning model, (c) a global search covering the entire Moon, or (d) a mathematically guaranteed single best landing point. We are free to add useful ranking and search functions, provided we describe their limits honestly.

### 2.1 Why this matters

A launch or hardware delay can shift the planned arrival date. Conditions at the original landing site can then change even though the physical terrain itself has not changed. Earth and Sun geometry evolves; hills and craters change which directions are visible. The tool helps teams rapidly ask, “Is the original site still suitable?” and “What nearby site or later date gives a better operating window?”

### 2.2 Intended users and their questions

| User | Key question | Product response |
|---|---|---|
| Mission planner | Is this site suitable for a 72-hour mission starting on this date? | Terrain-aware timelines, visibility fractions, longest outages, explanatory flags |
| Systems engineer | Does a delayed arrival change power/comms availability? | Compare dates at the same site under identical mission requirements |
| Science team | Which feasible site offers useful access to a target region? | Filter candidates and inspect trade-offs; retain science preference as a human input |
| Educator/student | Why is the lunar south pole so hard to operate in? | 3D terrain, low Sun angle, shadows, horizon and time animation |
| Public visitor | What would the Sun and Earth look like from this lunar position? | Surface Observer with explanatory overlays and clearly identified data status |

---

## 3. Product Goals, Boundaries, and Success Criteria

### 3.1 Product goals

1. Quickly inspect one selected candidate over a specified mission duration.
2. Search a supported lunar south-pole site set for qualifying **location-time pairs**.
3. Communicate sunlight and Earth visibility as *windows over time*, not just angles at landing.
4. Make local topographic obstruction visible and explainable.
5. Enable understandable comparison of alternatives following arrival-date changes.
6. Present results through a polished 3D experience without hiding the quantitative evidence.
7. Preserve provenance, model assumptions, and validation status in each result.

### 3.2 Deliberate non-goals for version 1

- Onboard real-time emergency landing guidance or autonomous spacecraft control.
- Launch/transfer trajectory calculation, lunar orbit insertion, descent propellant, or landing hazard clearance.
- A claim that surface visibility alone guarantees electrical power, thermally safe operation, or radio communication.
- Whole-Moon terrain-aware accuracy without full validated dataset coverage.
- A PINN, XGBoost or genetic algorithm merely to predict ephemeris positions.
- Detailed finite-element thermal modeling, solar array orientation model, or RF link budget in the initial release.

### 3.3 Minimum demonstrable success

A user can select at least one **verified** south-pole coordinate and a UTC date, see real Horizons Sun/Earth position tracks, and understand the geometric visibility timeline. When validated terrain data is connected for that coordinate, the user sees the **terrain-adjusted** windows and at least two comparable scenarios; the system never silently labels geometric-only or demo terrain as terrain-validated. A site marker can be selected in Unity and corresponding time-series evidence displayed.

**Stretch success:** evaluate at least three supported candidates over several arrival times, reject failing scenarios under explicit thresholds, and present up to three explainable recommendations.

---

## 4. Definitive User Experience: Two Modes, One Scientific Engine

The user sees two main options on an unobstructed 3D Moon. Both use the same underlying astronomy, terrain, and window calculations. The difference is who supplies the candidate site and arrival time.

### 4.1 Mode A — Site Inspector

**Purpose:** analyze the conditions of a location chosen by the user. This is not a search for the best location.

**Required inputs**

- Landing site: choose a curated candidate, click a supported area on the globe, or enter latitude/longitude manually.
- Proposed landing date and time, expressed in UTC.
- Mission duration: for example, 24, 72, or 168 hours.

**Optional/advanced inputs:** observer height above terrain; desired time resolution; solar-visibility margin; comparison with a second date or site; mission constraints for pass/fail labeling.

**Output:** the site and arrival timestamp, Sun/Earth azimuth and elevation versus time, local horizon versus azimuth, sunlight and Earth-visibility timelines, joint availability, fraction of the mission with each condition, longest darkness, longest direct-to-Earth interruption, and next predicted visibility transition. Show if terrain and/or ephemeris data are unavailable.

**User story:** “My lander is planned for Site A on 10 December at 12:00 UTC. Show whether it can receive sunlight and see Earth during its next 72 hours.”

**Acceptance:** changing the date or mission duration recalculates the interval; the UI indicates whether values are actual ephemerides, geometric-only, terrain-aware, or illustrative.

### 4.2 Mode B — Landing Opportunity Finder

**Purpose:** automatically propose favorable site + time combinations among a defined, scientifically supported search set.

**Required inputs**

- Earliest allowed landing timestamp (UTC): the beginning of the arrival search.
- Landing search window: e.g., 7, 14, or 30 days after that timestamp.
- Mission duration after landing: e.g., 72 hours.
- Search area: default to supported lunar south-pole candidate sites; optional choose a supported subregion or a named site set.

**Advanced mission constraints (with *illustrative user-editable defaults*, not NASA standards):** minimum sunlight fraction, minimum Earth-visible fraction, maximum longest darkness, maximum communication interruption, optional minimum joint visibility, observer height, and scoring priorities.

**Output:** a ranked list of at most three **(site ID, landing UTC)** combinations satisfying all hard constraints, including score, science metrics, trade-offs, source quality, and interactive “Explore” actions. If none qualifies, display “No qualifying opportunities found” with the closest misses and failed constraints—never fabricate three winners.

**Two ranking options inside Mode B:**

- **Best Overall:** rank qualifying opportunities according to the selected trade-off weights; show date/time explicitly.
- **Earliest Qualifying:** first find the earliest arrival time at which at least one candidate qualifies; list qualifying sites near that opportunity, sorted by suitability. This replaces a redundant third mode.

**Key distinction:** the search window bounds *when landing may occur*, whereas mission duration determines *how long conditions must be evaluated after landing*. A mission may extend past the search window.

### 4.3 Example mission (illustrative requirements only)

| Input | Example |
|---|---|
| Earliest arrival | 2026-12-01 00:00 UTC |
| Search window | 14 days |
| Operating duration after landing | 72 hours |
| Region | Supported south-pole candidate set |
| Minimum sunlight fraction | 80% |
| Minimum Earth-visibility fraction | 70% |
| Maximum continuous darkness | User-defined (e.g., 6 h) |
| Maximum continuous communications outage | User-defined (e.g., 8 h) |
| Output limit | 3 qualifying opportunities |

These values are **examples of configurable mission requirements**, not NASA-approved operating limits and not measured lunar outcomes.

### 4.4 Why “available right now” is not enough

A site may have sunlight *at the arrival instant* and then enter shadow shortly afterward. A valid candidate must satisfy mission constraints over **[landing_time, landing_time + mission_duration]**. “Earliest” means earliest *environmentally qualifying* opportunity within the analyzed candidate set, not proof that a launch or arriving spacecraft can physically reach the site at that time.

---

## 5. Geographic Scope and Terrain Policy

The challenge is especially about **the lunar south pole**. For initial analysis, use the broad south-polar band near 80°S to 90°S as conceptual geographic scope, but enable calculations only where required validated input datasets provide coverage. The first fully supported area should be a smaller named region such as Connecting Ridge or Shackleton rim, subject to verified coordinate/dataset overlap. [S3–S4]

**Do not promise a global Moon-wide scientific search.** Users may navigate the entire rendered globe visually; they may only request *terrain-aware scientific assessment* at supported locations. Unsupported regions should be marked unavailable or, by explicit choice, downgraded to a clearly labeled geometric-horizon approximation.

A region selector is better than a “South Pole / Whole Moon” switch that implies unsupported coverage. Proposed progression:

1. Curated verified sites inside a single high-quality DEM.
2. Multiple small south-polar site regions, each with known DEM coverage.
3. Optional map-based region selection constrained to indexed, processed DEM coverage.
4. Only later, broad lunar coverage if scientifically validated and computationally feasible.

**Candidate provenance:** store site name, latitude, east longitude, coordinate frame, elevation source, relevant DEM, and a URL or citation. Do not assume that all planned or historic mission targets are geographically accurate based only on visually placed markers.

---

## 6. Scientific Data: Exactly What We Need

### 6.1 Source A — JPL Horizons: the ephemeris engine

**What it is:** NASA/JPL's online system for calculating the positions and apparent directions of solar-system objects over time. It is not just a static CSV download; it offers an **HTTP API**: code sends parameters to a URL, and a server returns computed table output. We parse and cache those results. [S2, S5]

**Information we provide:** observing location on the Moon (lunar latitude, longitude, reference height), target body (Sun or Earth), UTC start/stop timestamps, output sampling interval, observer ephemeris type, and requested quantities.

**Information we need back:** timestamp, apparent azimuth measured around the local horizon, and apparent elevation above/below the geometric horizon for Sun and Earth. Horizons can perform lunar body orientation and ephemeris geometry; our application must still check its output conventions and handle observer/terrain reference heights correctly.

**Indicative parameters (must be tested as an actual request):**

```text
Endpoint:   https://ssd.jpl.nasa.gov/api/horizons.api
EPHEM_TYPE: OBSERVER
CENTER:     coord@301          # Lunar surface coordinates
COMMAND:    10 or 399         # Sun or Earth
COORD_TYPE: GEODETIC
SITE_COORD: east_lon,lat,altitude_km
QUANTITIES: 4                 # Apparent azimuth/elevation
APPARENT:   AIRLESS           # No atmospheric refraction
START_TIME, STOP_TIME, STEP_SIZE, TIME_TYPE=UT
```

The service may return JSON containing a *text-formatted* ephemeris. The parser must isolate the rows between the `$$SOE` and `$$EOE` markers and inspect output headers before assigning columns. A response is not validated until a real request runs and a few values are independently checked.

### 6.2 Source B — LOLA and NASA DEMs: the terrain engine

**LOLA** stands for Lunar Orbiter Laser Altimeter, an instrument on NASA's Lunar Reconnaissance Orbiter. Laser-altimetry observations are processed into a **DEM** (Digital Elevation Model): a georeferenced grid in which each pixel stores a terrain elevation value. A GeoTIFF is a common file format. This data is not an API response in the same sense as Horizons; we download selected raster files and load them with GIS/numerical tools. [S3–S4]

**Regional product:** NASA's large-area south-pole LOLA DEM family provides regional coverage suitable for distant ridges and crater walls; its data page describes south-polar stereographic coordinates and the MOON_ME/DE421 frame. The team must select an exact file/resolution based on available storage and required range, rather than assuming one arbitrary tile covers every view ray. [S3]

**Site-specific product:** NASA's 5 m/pixel LOLA DEMs include Connecting Ridge, Shackleton rim and other named south-polar areas. Products also include slope and uncertainty layers. High nominal resolution is **not** equivalent to direct 5 m measurements everywhere: many pixels are interpolated. [S4]

**Tools:** `rasterio`/GDAL for GeoTIFF projection and sampling; `numpy` for ray/horizon arrays; `pandas` for time tables; optionally `pyproj` for coordinate transformation; plotting libraries for verification.

### 6.3 Source C — verified site catalog

A small CSV or JSON manifest stores eligible candidate sites, unique IDs, longitudes/latitudes, coordinate-frame notes, DEM coverage IDs, names, source citations, and optional science annotations. Prefer verified points inside actual DEMs. QuickMap and Moon Trek help inspect geography and verify approximate placement; they do not replace the gridded terrain and ephemeris calculations. [S6–S7]

### 6.4 Source D — visuals, separate from scientific measurements

**NASA CGI Moon Kit** provides global color and displacement imagery for 3D rendering. Use an appropriately sized version for the Unity globe, with NASA attribution. The kit explicitly distinguishes rendering-oriented maps from their scientific source products; the local horizon calculation must rely on validated georeferenced terrain, not purely decorative surface meshes. [S8]

### 6.5 Input and processed output formats

| Dataset | Typical form | Representative fields | Used for |
|---|---|---|---|
| Horizons observation | HTTP JSON containing text table, then CSV/JSON | UTC, azimuth, elevation, target | Direction in local lunar sky |
| LOLA terrain | GeoTIFF raster | X/Y projection, elevation (m), NoData, grid metadata | Distant and local terrain horizon |
| LOLA auxiliary layers | GeoTIFF raster | Slope, elevation uncertainty, count/effective resolution | Data quality and optional terrain constraints |
| Candidate catalog | CSV/JSON | ID, name, lat, east lon, DEM region, provenance | Controlled search set |
| Mission request | JSON | Time range, duration, site IDs, thresholds, priorities | Analysis/optimization request |
| Analysis result | JSON + optional CSV | Tracks, windows, metrics, ranking reasons, provenance | Unity visualization and reproducibility |

---

## 7. Coordinate Systems and Geometric Consistency

This is one of the most consequential engineering risks. The visual globe, Horizons observer location, LOLA DEM, and site catalog must all refer to the **same physical location and orientation**.

**Mandatory conventions to record:** latitude north-positive degrees (south-polar values negative); longitude east-positive degrees with explicitly stated wrapping range; observer height in meters above DEM ground; UTC timestamps in ISO 8601; local azimuth degrees clockwise from north where confirmed; elevation degrees positive upward; and the source's lunar body-fixed reference/ephemeris realization.

**Specific checks:**

1. Inspect every GeoTIFF's CRS, transform, units, NoData markers, bounds, and reference radius.
2. Verify whether `SITE_COORD` altitude uses kilometers and what reference body/height convention applies.
3. Confirm that east/west longitude conventions and Moon-body frame orientation agree before query/visualization.
4. Test the lat/lon-to-pixel mapping at independent recognizable control points.
5. Check sky azimuth orientation in the local east-north-up frame before plotting surface arrows.
6. Avoid treating nominal DEM heights, observer height, globe radius, and site altitude as interchangeable quantities.

**Optional vector formulation:** for spherical latitude `phi` and east longitude `lambda`, local basis vectors are

```text
up    = (cos(phi) cos(lambda), cos(phi) sin(lambda), sin(phi))
east  = (-sin(lambda), cos(lambda), 0)
north = (-sin(phi) cos(lambda), -sin(phi) sin(lambda), cos(phi))
```

If a body direction `s` is expressed in the **same** correctly oriented lunar-fixed frame, the local elevation and azimuth are approximately:

```text
elevation = asin(dot(s, up))
azimuth   = atan2(dot(s, east), dot(s, north))  [normalize 0–360 degrees]
```

These vectors are useful for rendering and testing; we should not reinvent Horizons' high-precision orbital calculations for the initial build. More accurate treatment may require a local geodetic normal rather than a simple radial up vector depending on the adopted shape/reference model.

---

## 8. The Visibility Mathematics

### 8.1 Step 1: Obtain the body's direction

For each time `t` and site, obtain Sun azimuth `A_s(t)`, Sun elevation `E_s(t)`, Earth azimuth `A_e(t)`, and Earth elevation `E_e(t)` from the validated ephemeris table. Azimuth indicates *which direction* a body lies; elevation indicates *how high* it appears in the local sky.

### 8.2 Step 2: Construct a terrain horizon profile

For each direction `a` (e.g., one sample per degree or finer), examine terrain along that outward ray, measure the elevation angle of every visible terrain point as observed from the site, and keep the **maximum blocking angle**. The result is `H(a)`: a 360-degree profile of the terrain's apparent skyline. The profile depends on the observer's surface position and height. It should include adequately distant topography, not just the small close-up mesh.

A first-order curvature-corrected approximation for range `d` is:

```text
H(a) ≈ max_d atan( (z(d,a) - z_site - h_observer)/d - d/(2 R_moon) )
R_moon ≈ 1,737,400 m
```

This approximation is useful for prototyping but must be checked against the DEM's projection, elevation datum, curvature and great-circle geometry. A more consistent spherical approach can evaluate the line-of-sight angle from the observer's Cartesian position to each terrain point; this avoids assuming a flat raster distance equals a lunar surface arc distance. Missing DEM cells and rays exiting the data extent must produce a quality warning, not silently zero-height mountains.

### 8.3 Step 3: Determine visibility

In the simplest center-of-disk approximation:

```text
SunVisible(t)   = 1 if E_s(t) > H(A_s(t)), else 0
EarthVisible(t) = 1 if E_e(t) > H(A_e(t)), else 0
BothVisible(t)  = SunVisible(t) * EarthVisible(t)
```

An optional **geometric-only** fallback uses `H(a)=0°`; it must be clearly labelled because it can be very misleading in the polar terrain. Finite angular radii of the Sun and Earth, antenna masks, nonzero observation heights and solar-array pointing can be treated in later accuracy tiers.

### 8.4 Step 4: Evaluate the *entire mission duration*

For candidate landing time `T` and required operating duration `D`, evaluate visibility over the interval `[T, T+D]`. Record:

- `f_sun` — fraction of mission duration with Sun visible.
- `f_earth` — fraction with Earth geometrically visible.
- `f_both` — fraction with both visible simultaneously.
- `dark_max` — longest continuous interval without visible Sun.
- `comm_gap_max` — longest continuous interval without Earth visibility.
- `next_sun_loss`, `next_earth_loss` — next transitions, when within the simulated horizon.
- Optional safety margin: time until a required visibility threshold is lost.

For equally spaced samples of duration `delta_t`, `f_sun ≈ (1/N) sum_i SunVisible(t_i)`, and similarly for Earth. Use duration-weighted integration if spacing varies, define endpoint handling consistently, and refine sampling near visibility transitions instead of claiming sub-minute precision from hourly samples.

### 8.5 Step 5: Apply requirements and rank

**Hard requirements** (configured by the mission, not by NASA as universal standards):

```text
f_sun >= min_sun_fraction
f_earth >= min_earth_fraction
dark_max <= allowed_max_darkness
comm_gap_max <= allowed_max_communications_outage
optional: f_both >= minimum_joint_fraction
```

If a candidate fails any mandatory requirement, show the failed rule and exclude it from the qualifying list. Among remaining candidates, use a transparent, normalized weighted score (or an explicitly described multi-objective ranking) to compare sunlight, communications, darkness, and timing.

**Important implementation caution:** a proposed expression `1 - dark_max / allowed_max_darkness` can become negative, undefined when the limit is zero, and poorly normalized. Prefer a bounded normalization based on well-defined physical scales or a simple lexicographic sort with documented priorities. Keep “Best Overall” and “Earliest Qualifying” different ranking policies over the same candidates.

### 8.6 Science is not the same as mission certification

A positive `SunVisible` flag indicates line-of-sight illumination, not measured watts at a solar array; a positive `EarthVisible` flag indicates geometric line of sight, not guaranteed radio-link performance. Terrain slope and obstacles matter to landing safety but cannot be validated just by our lighting/communication analysis. Reports must say **“environmentally promising candidate”** instead of **“safe landing zone.”**

---

## 9. Recommendation/Search Algorithm

**Search space:** a finite validated candidate set `S` and a series of allowed landing UTC timestamps `T`; each scenario is `(site, landing_time, mission_duration)`.

**Initial brute-force MVP (preferred over ML):**

```text
for site in verified_supported_sites:
    precompute_or_load_horizon(site)
    fetch_or_load_sun_earth_tracks(site, extended_time_range)
    for landing_time in allowed_arrival_times:
        interval = [landing_time, landing_time + mission_duration]
        visibility = compare_tracks_against_horizon(interval)
        metrics = integrate_visibility_and_find_outages(visibility)
        if all_hard_requirements_pass(metrics):
            qualifying.append((site, landing_time, metrics))
        else:
            rejected.append((site, landing_time, failure_reasons))
return choose_up_to_three(qualifying, ranking_policy)
```

**Efficiency:** request a track for each site over the overall arrival search window **plus the mission duration**, cache the result, and evaluate many sliding intervals without repeatedly calling Horizons. Precompute `H(az)` once per site/observer height (or cache by height). Coarse time steps find broad opportunities; refined intervals around threshold crossings improve report accuracy.

**Search is conditional on the candidate set.** The top score is not a mathematical guarantee of the globally best lunar point. Show candidate coverage, time step, date interval, constraints and algorithm version in results.

**No ML needed:** ephemerides and topographic ray checks are deterministic computations; a physics-informed neural network would add training/validation burden without clear value for this small supported site set. It could be investigated later as a surrogate speed-up, not as the trusted source of celestial positions.

---

## 10. Information Architecture and User Journey

### 10.1 What a first-time user sees

1. Start the app: an attractive, fully interactive lunar globe in dark space.
2. A small choice: **Inspect a Site** or **Find Opportunities**.
3. A concise floating form collects only essential mission details.
4. Results appear as small ranked cards or a compact timeline—not as permanent side panels.
5. Select a candidate → cinematic approach → local orbit → surface observation.
6. The user can scrub time and observe direction indicators and terrain shadows while checking numbers in a collapsible data overlay.
7. Compare candidates or dates, return to globe, or export scenario evidence.

### 10.2 Main interface principles

- The 3D universe fills the viewport; panels appear as translucent, dismissible overlays.
- The technical overlay displays *UTC time*, actual data provenance, selected site, and at least one visible science quality label.
- Critical charts/timelines must be readable without cinematic effects and should remain accessible while pausing the scene.
- Show the difference between **“Sun above geometric horizon”** and **“Sun above actual terrain horizon”**.
- Surface views must not place Sun or Earth in arbitrary artistic positions while displaying real scientific values.
- If using visually compressed celestial distances/scales, explain that they are presentation approximations.

### 10.3 All four camera modes

| Camera | User experience | Transition/controls |
|---|---|---|
| **1. Global** | Explore the entire globe, rotate around it, zoom, choose markers | Mouse drag orbit, wheel zoom, double-click/select to focus |
| **2. Cinematic Approach** | A seamless, accelerating/decelerating fly-in to the selected site | Select/replay/skip; preserve site context; finish in Local Orbit |
| **3. Local Orbit** | Inspect crater walls, terrain and marker from above at adjustable angles | Orbit, tilt, zoom; collision-aware; return to Global |
| **4. Surface Observer** | View the actual local skyline and Sun/Earth directions at selected time | Look around, toggle horizon/ray overlays, return to Local Orbit |

**Required guide:** Help icon plus `H` or `?` key opens a shortcut sheet with all actual bindings. Suggested: `1`–`4` modes; left-drag orbit/look; scroll zoom; `F` focus; `R` reset; `Space` play/pause; `Esc` dismiss/back; `Tab` hide UI. Display only controls that truly work.

### 10.4 Scientific presentation layers

- **Cinematic:** realistic scene lighting, minimal UI.
- **Scientific:** local horizon curve; Sun/Earth azimuth/elevation markers and numeric readout; line-of-sight status and time-series traces.
- **Comparison:** side-by-side metrics for 2–3 scenarios, with clearly labeled dates and site IDs.
- **Confidence/provenance:** visible label for actual Horizons, actual DEM, approximated terrain, or demo-only data.

---

## 11. System Architecture

**Preferred separation:** Unity presents and explores; Python calculates and records science. This avoids mixing astronomy, GIS processing, scene rendering, and ranking into a single application layer.

```text
[ Unity HDRP Desktop ]
  Input forms | Globe + 4 cameras | Markers | Timeline | Metrics
                |
       JSON over HTTP (optional live mode)
                |
            [ FastAPI ]
       /sites | /analyze | /opportunities | /health
                |
       [ Python Scientific Engine ]
      Site catalog / Horizons client + cache
      DEM loader / Horizon profiler / Visibility windows
      Constraint checker / Ranker / Serializer
                |
      [ Local Cache and NASA Source Data ]
       CSV/JSON ephemerides | GeoTIFFs | provenance

Alternative for reliable judging:
    Scientific engine --> validated offline result JSON --> Unity
```

### 11.1 Why FastAPI

FastAPI is a lightweight Python service that receives parameters from Unity and returns JSON. It is **not** what makes astronomy accurate, and it is **not required** for contest eligibility. It improves modularity and allows arbitrary live requests when the backend is running. The same science functions should operate without FastAPI for offline analysis and data export.

### 11.2 Suggested endpoint contracts

| Endpoint | Request | Response |
|---|---|---|
| `GET /health` | None | readiness/version/status |
| `GET /sites` | Optional region filter | eligible site IDs, coordinates, coverage/provenance |
| `POST /analyze` | Site ID or validated coordinate; landing UTC; duration; observer height | tracks, visibility windows, metrics, coverage/status |
| `POST /opportunities` | arrival range; duration; site set; hard constraints; ranking policy | qualifying up to 3; rejected summaries; search metadata |

**Example request (no fabricated scientific output):**

```json
{
  "earliest_landing_utc": "2026-12-01T00:00:00Z",
  "arrival_search_days": 14,
  "mission_duration_hours": 72,
  "region_id": "supported_south_pole_sites",
  "requirements": {
    "min_sun_fraction": 0.80,
    "min_earth_fraction": 0.70,
    "max_dark_hours": 6,
    "max_comms_gap_hours": 8
  },
  "ranking_policy": "best_overall",
  "max_results": 3
}
```

**Every result should include** `data_status`, `ephemeris_source`, `terrain_source`, `time_step_minutes`, `coordinate_frame`, `site_id`, `arrival_utc`, `mission_duration_hours`, `metrics`, `constraints_passed`, `failed_constraints`, and optional `warnings`. Real values are emitted only when computed from successful, validated source processing.

### 11.3 Suggested project structure

```text
LunarNavigator/
  UnityProject/                  # or current existing Unity project root
    Assets/Scenes/
    Assets/Scripts/Cameras/
    Assets/Scripts/Simulation/
    Assets/Scripts/UI/
    Assets/Art/ and Assets/Prefabs/
  backend/
    app/main.py                 # FastAPI adapter
    science/horizons.py
    science/terrain.py
    science/horizon_profile.py
    science/visibility.py
    science/metrics.py
    science/ranking.py
    data/sites.csv
    data/cache/
    tests/
  docs/
    scientific_method.md
    data_dictionary.md
    validation_report.md
    sources_and_credits.md
  README.md
```

If the existing Unity project already lives at `C:\Users\user\Documents\Unity\LunarNavigator`, **do not restructure it during the hackathon just to match this example**. The `backend/` directory can be added within that root, with care to keep Python files outside Unity's `Assets/` import path if appropriate.

> [!NOTE]
> **Existing Unity Prototype Status:** The current Unity project runs on **Unity 6.6 (`6000.6.3f1`)**. The existing visual scene is strictly **illustrative**: the local terrain patch, Sun, and Earth directions are procedural demonstrations, not data-driven calculations. Calling the current prototype scene "scientific" would overstate its current capability; authentic scientific validity begins once the backend data pipeline (JPL Horizons ephemerides and LOLA DEMs) is connected to drive the scene.

---

## 12. Verification, Scientific Validation, and Data Quality

The system must distinguish **source existence** from **successful computational validation**. The official URLs and available products have been researched; that does not prove any chosen coordinate, timestamp, output value, terrain profile or ranking has been validated in the team's environment.

### 12.1 Required science checks

1. Run a real Horizons API request for Sun and Earth at one test coordinate over several days; preserve the raw response and parsed CSV.
2. Verify request parameters, time scales, azimuth/elevation columns, target identifiers, and site-coordinate conventions from JPL documentation.
3. Load one real LOLA GeoTIFF; log CRS, bounding box, pixel size, NoData statistics, reference heights, and uncertainty layers where available.
4. Validate at least one known geographic point against a map viewer or separately documented dataset coordinates.
5. Generate a 360-degree horizon profile and sanity-check prominent ridge directions with a terrain profile/3D view.
6. Compare geometric-only and terrain-aware visibility for the same site and time range; differences should be interpretable.
7. Test simple synthetic terrain cases with known expected results (flat ground, blocking ridge, site on raised terrain).
8. Use a second timing resolution around rising/setting boundaries; measure sensitivity to sampling step.
9. Repeat analysis at different observer heights; note how projected horizon changes.
10. Record any missing DEM coverage and uncertainty rather than treating gaps as open sky.

### 12.2 Required product and UI tests

- Form input errors: invalid latitudes, missing site IDs, negative mission durations, impossible or contradictory constraints.
- Search boundary: arrival time on first/last allowed step; mission interval extending after search-window end.
- Ranking stability: filters before scoring; zero results; fewer than three results; ties; earliest-vs-best policy differences.
- Date consistency: all displayed times in UTC; comparison dates remain synchronized.
- Unity scene loads; switching 1→2→3→4→1 doesn't corrupt camera; marker selection stays correct.
- Demo data labels cannot be mistaken for source-validated predictions.
- A downloaded/offline build displays at least one real, cached, reproducible dataset.

### 12.3 Result confidence tiers

| Tier | Description | UI label |
|---|---|---|
| 0 | Placeholder illustrative directions/terrain for graphics testing | **ILLUSTRATIVE DEMO** |
| 1 | Verified Horizons Sun/Earth track, no local terrain obstruction | **GEOMETRIC HORIZON ONLY** |
| 2 | Horizons + real DEM terrain profile; basic tested line-of-sight checks | **TERRAIN-AWARE ESTIMATE** |
| 3 | Independent cross-checks, timing refinement, coverage/uncertainty assessed | **VALIDATED WITH STATED LIMITS** |

The higher tiers do not equal spacecraft-flight certification. Never use the word “accurate” without stating the specific comparison, resolution, and assumptions supporting it.

---

## 13. Implementation Work Plan and Ownership

The deadline rewards integrated results more than architectural perfection. Development should proceed in parallel, but with a single shared science-to-Unity JSON contract.

### Phase 0 — Lock decisions immediately

**Outputs:** product scope, two modes, supported first region, initial candidate list, Unity Windows HDRP target, UTC conventions, and backend JSON contract. **Definition of done:** every contributor works from this proposal and one short acceptance checklist.

### Phase 1 — First authentic ephemeris proof (P0)

**Tasks:** run `horizons_probe.py` (or a corrected replacement), save raw API responses, parse Sun/Earth tracks, plot both elevations against UTC, manually verify a few rows and conventions. **Exit criterion:** one demonstrably real Sun/Earth az/el series; not just an unexecuted Python script.

### Phase 2 — First DEM and terrain proof (P0)

**Tasks:** download a compatible south-pole DEM, verify geo-reference and candidate coverage, compute horizon profile at a chosen site and observer height, detect and flag missing coverage. **Exit criterion:** a plotted skyline and a test showing terrain-dependent obstruction.

### Phase 3 — Site Inspector math engine (P0)

**Tasks:** combine ephemerides and terrain, calculate flags and intervals, emit metrics and source metadata, test at least two scenarios. **Exit criterion:** reproducible analysis for one real site + date + mission duration with documented status.

### Phase 4 — Opportunity Finder (P1)

**Tasks:** populate a curated verified site set, evaluate multiple allowed arrival timestamps, filter requirements, rank qualifying scenarios, expose best-overall/earliest policies, return up to 3 and failure reasons. **Exit criterion:** tests proving candidates are rejected correctly and ranking is stable.

### Phase 5 — Unity integration and UX (P0/P1)

**Tasks:** preserve Codex's camera and scene progress; connect results by offline JSON first, optional FastAPI second; place sites correctly; show 3D exploration, direction markers, timeline, metrics popovers, overlay/help. **Exit criterion:** a real calculation can drive visible Unity state and a demo scenario can be replayed.

### Phase 6 — Scientific validation and submission (P0, run in parallel)

**Tasks:** record assumptions, validate at least one site, fix critical UI faults, make a short demo, upload a public final-project link, describe NASA data and AI use, test both URLs without login, and submit officially. **Exit criterion:** actual official confirmation of submission and usable public evidence, not merely “Save Progress.”

### 13.1 Responsibility matrix

| Workstream | Responsibilities | Artifacts |
|---|---|---|
| Scientific data | Horizons calls, raw data, site catalog, DEM metadata and coordinate checks | Raw responses, CSV, DEM crop, provenance |
| Science engine | Terrain horizon, visibility flags, windows, validation | Python library, tests, plots, analysis JSON |
| Search/backend | Hard constraints, ranking, optional FastAPI, cache | API schemas, results, test scenarios |
| Unity/visualization | Four cameras, globe, local terrain, floating UI, JSON adapter, timeline | Playable HDRP scene/build, controls documentation |
| QA/submission | Independent checks, feature-status ledger, demo, public links, disclosure | Validation report, public repository/build, 30-sec demo or 7 slides |

### 13.2 Order when time is extremely short

**Must ship first:** real Horizons data → basic science chart → supported site selection → minimum Unity playback → publicly accessible project/demo links → official submit. **Then:** validated terrain horizon → second site/date comparison → working ranking. **Last:** advanced shaders, broad region search, additional optimization and sophisticated terrain rendering.

---

## 14. Risks, Mitigations, and Explicit Trade-offs

| Risk | Consequence | Mitigation |
|---|---|---|
| Wrong coordinate frame/longitude sign | Correct-looking but geographically incorrect answers | Validate metadata and control points; write coordinate tests |
| Missing DEM data along long-distance ray | False sunlight/Earth visibility | Use wider regional terrain, flag incomplete horizon sectors |
| DEM interpolation and resolution error | Apparent precise shadows not supported by source quality | Expose DEM resolution and quality; test sensitivity to observer height |
| Horizons API slow/unavailable during presentation | Live analysis fails | Cache validated ephemerides; provide offline JSON scenarios |
| Overly coarse sample time | Missed short visibility windows | Document temporal resolution; refine transitions and test convergence |
| 'Sun visible' misread as sufficient power | Overconfident recommendation | Label metrics as geometric proxies, disclose excluded subsystems |
| 'Earth visible' misread as guaranteed comms | Overconfident recommendation | Explain that link budget and antenna pointing are excluded |
| Complex full-Moon region picker | Unsupported geographic claims | Restrict analysis to mapped/validated candidate sets |
| HDRP intended for browser | No WebGL build | Keep Windows desktop build; do not pivot to URP under deadline |
| Unity visuals consume all development time | No verified scientific contribution | Parallelize and insist on one real science-to-Unity demo |
| AI-generated visuals improperly disclosed | Submission-policy concern | Attribute source media; visibly mark AI-generated visual/video material |
| No accessible final link at deadline | Submission cannot be reviewed | Delegate submission early; test links in an incognito window |

---

## 15. Submission and Demonstration Strategy (2026)

The official 2026 Space Apps schedule says the submission period closes **Sunday, 15 November 2026 at 11:59 PM in the team's local time zone**. The site's challenge page allows an interactive simulation, and 2026 project guidance describes a short public demo link and a separate public final-project link. A Windows HDRP build is therefore a feasible format if reviewers can access the deliverable and its documentation; it is **not** equivalent to a WebGL website. Unity's platform matrix confirms HDRP does not target WebGL. [S1, S9–S11]

**Project Demo:** a public video of no more than 30 seconds **or** a presentation of up to seven slides (including title/closing material), in English or correctly subtitled. **Final Project:** a distinct public link such as a repository with source, packaged Windows build or download, README, and scientific methodology; make links open without account permission. [S10–S11]

**Submission content checklist:** project name and plain-English summary; what is actually implemented; NASA data source links and roles; other imagery/library references; transparent disclosure of AI assistance; limitations, setup steps, and accessible demo/final-project URLs; final official submission action (not only save progress).

### 15.1 Suggested 30-second demonstration storyboard

| Time | Visual | Narration/on-screen message |
|---|---|---|
| 0–5 s | Full Moon, product title | “At the lunar south pole, a ridge can decide whether a mission has power or contact with Earth.” |
| 5–10 s | Choose site/date/duration | “Lunar Navigator evaluates arrival times and supported landing sites.” |
| 10–17 s | Show authentic visibility timeline | “JPL Horizons and LOLA-based terrain analysis reveal sunlight and Earth visibility windows.” |
| 17–24 s | Ranked results and site fly-in | “Compare opportunities and understand the trade-offs.” |
| 24–30 s | Ground view + sources/limitations | “Explore the lunar horizon and plan with evidence.” |

Only use these statements to the extent the corresponding scientific features truly work in the submitted build. If ranking or LOLA analysis is incomplete, present those as planned extensions rather than accomplished features.

### 15.2 Suggested high-level submission summary (edit after verification)

> Lunar Navigator is an interactive lunar south-pole mission-planning prototype that helps users explore how a landing location and arrival date influence sunlight and direct-to-Earth visibility. The system uses NASA/JPL ephemeris information and, where validated, NASA lunar topography to examine local horizon obstruction and mission-duration windows. Users can inspect candidates, compare environmental trade-offs, and explore landing regions in a cinematic Unity simulator. The application is designed for rapid, transparent pre-mission scenario assessment—not autonomous landing guidance.

The final submission should distinguish implemented features from planned features and include the actual tested NASA sources and quality limitations.

---

## 16. Acceptance Criteria and Definition of Done

### 16.1 Scientific MVP (must be testable outside Unity)

- [ ] One live, successful Horizons query for Sun and Earth at a declared lunar observer site.
- [ ] UTC/angles/target identifiers correctly parsed; original raw response retained.
- [ ] One actual lunar DEM loaded with valid coordinate frame and spatial coverage documented.
- [ ] One horizon profile calculated and tested on synthetic and/or independently inspected terrain.
- [ ] Sun and Earth visibility flags calculated throughout a configurable mission duration.
- [ ] At least one quantitative timeline and visibility metrics report generated.
- [ ] Accuracy/status labels make geometric-only versus terrain-aware results unambiguous.
- [ ] At least two scenarios can be compared with repeatable inputs and outputs.

### 16.2 Opportunity Finder (stretch toward full scope)

- [ ] Search includes arrival-window end handling and full subsequent mission duration.
- [ ] Hard requirements applied before ranking; rejected reasons preserved.
- [ ] Best Overall and Earliest Qualifying policies produce deterministic outputs.
- [ ] Up to three returned; zero qualifying outcomes handled honestly.
- [ ] Ranked results record candidate set, time step, weights, and provenance.

### 16.3 Unity visualization and deployment

- [ ] Main fullscreen lunar globe renders reliably on target Windows hardware.
- [ ] All four camera modes function with explainable transitions.
- [ ] Markers match verified lunar coordinates at least in supported regions.
- [ ] UTC time slider and Sun/Earth direction indicators use identified data source.
- [ ] Floating dialogs do not interfere with orbit controls; keyboard help exists.
- [ ] At least one validated offline JSON scenario is bundled for presentation.
- [ ] Public project/demo links function; honest README and sources provided.
- [ ] Submission confirmed on official Space Apps site before deadline.

**Completion rule:** source code existing is not enough; features must be compiled, executed, observed, and marked with the actual level of verification achieved.

---

## 17. Open Decisions and Post-MVP Enhancements

The following are deliberately **not required to start the MVP**:

- Exact first set of scientifically supported site coordinates (to be selected after DEM coverage verification).
- Search grid spacing and number of time candidates (balance speed and detection of short windows).
- DEM horizon radial extent and angular resolution (validate accuracy versus runtime).
- Final ranking weights (must reflect a particular mission scenario; offer presets and explanation).
- Whether to add explicit terrain slope screening (requires verified slope layers and landing-specific interpretation).
- More refined Sun/Earth partial-disk visibility, observer mast heights, battery capacity, antenna pointing, and link budget.
- Whether to later deploy a URP web edition (separate build/platform project rather than an HDRP WebGL export).
- More automated continuous lunar-region optimization or optional surrogate models after baseline validation.

These are normal later-stage questions; their uncertainty should not delay the first authentic Horizons and LOLA integration.

---

## 18. Glossary for New Team Members

| Term | Plain-English meaning |
|---|---|
| Azimuth | Direction around the local horizon, usually clockwise from north |
| Elevation angle | Angle above or below the local horizon |
| Ephemeris | Calculated position or direction of celestial bodies at specific times |
| JPL Horizons | NASA/JPL service supplying high-precision ephemeris tables |
| LOLA | Laser altimeter instrument that measured lunar topography |
| DEM | Digital Elevation Model: georeferenced grid of terrain height values |
| GeoTIFF | Image-like geospatial raster with embedded map coordinate metadata |
| Terrain horizon | Maximum apparent skyline angle in a given azimuth from a site |
| Geometric horizon | Idealized horizon without intervening hills or crater rims |
| Libration | Apparent change in the Moon/Earth relative orientation over time |
| Direct-to-Earth (DTE) | Line-of-sight communication route without a relay spacecraft |
| Mission duration | Period of operation whose conditions are assessed after landing |
| Arrival search window | Period in which landing timestamps may be considered |
| Constraint | Minimum/maximum requirement that qualifies or rejects a scenario |
| HDRP | Unity's high-fidelity render pipeline for suitable desktop-class platforms |
| FastAPI | Python framework for exposing calculations as HTTP endpoints |
| PINN | Physics-Informed Neural Network; not necessary for basic celestial geometry |
| MVP | Minimum viable product: smallest functional, verifiable demonstration |

---

## 19. Sources, Verification Status, and Attribution

This proposal combines (a) the official challenge wording, (b) a teammate's supplied one-page scientific outline, (c) the earlier team MVP plan, (d) the team's stated design choices, and (e) the official technical references below. **The existence and documented purpose of these resources have been checked; actual local code, numeric ephemerides, terrain files, and Unity implementation have not been independently tested by this proposal.**

| Ref | Primary source | Role and status |
|---|---|---|
| [S1] | https://www.spaceappschallenge.org/2026/challenges/lunar-landing-site-navigator/ | Official challenge scope and flexible solution formats — checked |
| [S2] | https://ssd-api.jpl.nasa.gov/doc/horizons.html | Horizons API request/response documentation — checked |
| [S3] | https://pgda.gsfc.nasa.gov/products/90 | NASA regional south-pole DEM collection, COG and stereographic details — checked |
| [S4] | https://pgda.gsfc.nasa.gov/products/78 | NASA high-resolution site DEMs, elevation/slope/uncertainty and interpolation caveat — checked |
| [S5] | https://ssd.jpl.nasa.gov/horizons/manual.html | Horizons system manual and observer conventions — checked |
| [S6] | https://trek.nasa.gov/moon/ | Moon Trek for geographic exploration — secondary verification aid |
| [S7] | https://quickmap.lroc.asu.edu/ | LROC QuickMap for mapping and visual checking — secondary aid |
| [S8] | https://svs.gsfc.nasa.gov/4720 | NASA CGI Moon Kit for rendering assets and attribution — checked |
| [S9] | https://docs.unity.com/en-us/engine/6000.6/manual/render-pipelines/choose-a-render-pipeline/feature-comparison | Unity 6.6 HDRP/WebGL platform matrix — checked |
| [S10] | https://www.spaceappschallenge.org/2026/ | 2026 project submission date and closing time — checked |
| [S11] | https://github.com/nasa/spaceapps/discussions/1991 | NASA Space Apps organizer guidance on project/demo public links — checked organizer reply |
| [S12] | https://github.com/nasa/spaceapps/discussions/1947 | 2026 organizer guidance on short demo and AI-media disclosure — checked organizer reply |
| [S13] | https://svs.gsfc.nasa.gov/14959/ | NASA Moon 3D models as optional global visualization assets — checked |

### 19.1 Team-origin material

- Supplied teammate PDF **NASA1.pdf**: useful list of ephemeris/terrain/site inputs, local-frame vector equations, first-order horizon formula, visibility rules, and initial constraint-and-score approach. Treated as a *research draft*, not as already-validated code/results.
- Earlier **LunarNavigator_Project_Plan.md**: agreed two-mode/Earliest-option MVP direction, scientific quality warnings, offline result adapter, and parallel responsibility split.
- Team discussion: 3D Unity HDRP desktop presentation with four camera modes and minimally intrusive popup UI.

### 19.2 NASA and AI attribution policy

Credit all NASA datasets and NASA SVS imagery used in actual outputs. Disclose use of Codex, ChatGPT, Antigravity, other AI tools or generated media/code in submission fields as required; visually mark AI-generated images or videos according to 2026 guidance, and avoid unauthorized use/modification of NASA logos and marks. The proposal itself does not assume NASA endorsement. [S8, S12]

---

## 20. Final Recommendation and Immediate Starting Task

**Do not wait for a finished cinematic simulator before proving the science.** Continue Unity development in parallel, but start the science engine with this strict demonstration:

1. Select one lunar south-pole test coordinate and clearly identify it as test or verified.
2. Query Horizons for Sun and Earth over a real UTC date interval; save raw output.
3. Parse and plot actual azimuth/elevation tracks.
4. Load a LOLA DEM that demonstrably covers the selected site and important horizon rays.
5. Compute and plot a terrain skyline, then a terrain-aware visibility timeline.
6. Publish one reproducible analysis JSON and make Unity read it.
7. Only then generalize to the opportunity ranking engine.

**One-sentence definition of the finished concept:** *Lunar Navigator is an evidence-based, explorable lunar south-pole planning experience that turns NASA celestial ephemerides and topographic measurements into understandable site-and-time opportunities over the full length of a proposed mission.*

**End of proposal.**
