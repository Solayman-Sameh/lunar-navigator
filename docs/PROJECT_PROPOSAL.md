# LunarNavigator — project proposal

**NASA Space Apps Challenge project · v1.1.1 revision · 19 November 2026**

## Purpose

LunarNavigator helps students and mission-planning audiences explore how location,
arrival time and mission duration affect sunlight and direct-to-Earth visibility
near the lunar south pole. It joins an interactive Unity Moon environment with a
Python scientific service using NASA/JPL Horizons ephemerides.

The product is an educational and preliminary comparison tool. Its analytical
scope is **GEOMETRIC_ONLY**, and its visible local terrain is illustrative.
It does not certify safe landing locations or operational mission feasibility.

## Problem

The south-pole Sun stays close to the horizon. A favorable arrival does not imply
favorable conditions throughout a surface stay. Sunlight and Earth visibility
also describe different constraints, and short continuous interruptions can matter
even when the overall visibility percentage appears high.

Static maps make these relationships difficult to explain. Users need a way to
inspect complete mission windows, compare alternative arrivals and understand the
assumptions behind each result.

## Implemented solution

- **Site Inspector:** select a source-cited regional reference center or click the
  Moon for a custom coordinate; configure a UTC landing time and surface stay.
- **Mission Assessment:** inspect sunlight, Earth and simultaneous visibility,
  maximum darkness and communication interruption, with current-result provenance.
  Changing the selected coordinate or mission invalidates stale assessments.
- **Mission timeline:** inspect separate Sun, Earth and combined visibility tracks,
  scrub time, and examine sampled intervals.
- **Opportunity Finder:** search sampled arrival dates for up to three reference
  sites under minimum visibility and maximum outage requirements. Rank qualifying
  scenarios by Best Overall or Earliest Qualifying; empty searches explain failure.
- **Spatial exploration:** global, approach, local-orbit and surface-observer views,
  with keyboard movement and mouse controls that respect interface interactions.

The v1.1.0 application adds corrected coordinate picking and movement handling,
a refined Mission Assessment interface, consistent branding, and a Windows setup
that installs the application and its private scientific runtime together.

## Architecture and delivery

The frontend uses **Unity 6000.6.3f1**, **HDRP 17.6.0**, C#, UI Toolkit and Unity
Input System. FastAPI/Pydantic validate requests; Uvicorn serves the Python backend.
The Windows package includes **CPython 3.11.9 x64** and pinned runtime dependencies.
A .NET Framework 4.8 launcher starts and identifies its backend, waits for health
readiness, launches Unity, and cleans up its child processes on exit.

Distribution uses **LunarNavigator_Setup.exe** as a GitHub Release asset. Users
choose their installation directory and do not install Python or Unity separately.
Uncached ephemeris acquisition needs internet. Exact validated cached requests can
be replayed offline. Logs, cache and results use per-user application storage.

## Scientific method

Horizons supplies apparent, airless azimuth/elevation for Sun target **10** and Earth
target **399**, from a lunar observer (`coord@301`). The parser validates identity,
coordinates, timestamps, grid completeness and numerical bounds. Source metadata
and SHA-256 hashes accompany retained results.

For nominal lunar radius R = 1,737,400 m and observer height h, the modeled horizon
is H = -acos(R / (R + h)), converted to degrees. Visibility requires target-center
elevation strictly above H. Defaults use a 2 m observer and ten-minute sampling.
States are left-held over half-open time cells; transition accuracy is cadence-limited.

The Finder applies explicit constraints before ranking. Its score is
`0.4*f_sun + 0.3*f_earth + 0.2*f_both + 0.1*(1-min(1,dark_max_h/duration_h))`.
These weights are application preferences, not NASA safety criteria.

The three catalog sites are **Shackleton Rim**, **Connecting Ridge** and
**Malapert Massif**, based on NASA reference-center coordinates. These centers are
not certified touchdown points. Inspector custom coordinates remain unverified.

## Verification

The v1.1.1 update adds contained-period Finder searches, editable number slots and
owned-folder uninstall cleanup. Current checks passed 166 backend/deployment tests,
13 launcher checks, 24 numeric-entry checks, keyboard-event checks and five Finder
UI request checks. Real NASA acquisition and a contained Finder search were checked
with the bundled runtime. Isolated clean, preservation and v1.1.0 upgrade/uninstall
cases passed. See [v1.1.1 validation](RELEASE_VALIDATION_v1.1.1.md) for limits.

The following records describe the earlier v1.1.0 work:


The v1.1.0 preparation run passed **158 backend/deployment tests**, **13 launcher
regression checks**, and **two additional bundled-runtime lifecycle checks**.
The icon refresh player build completed with zero errors and zero warnings.
A portable, retained NASA diagnostic pair is included with its regression tests;
synthetic fixtures are labeled separately.

The current interface and About panel were visually inspected in Play Mode.
An isolated file installation, shortcut creation and generated-uninstaller icon
check succeeded; uninstall removed the test installation. Registry writes and
capture of newly launched installer windows were restricted in the build environment.
These checks are not a clean-machine or live-network certification. See
[release validation](RELEASE_VALIDATION.md) for the exact evidence and limits.

The latest clock-fix Windows build completed in 191.5 seconds with zero errors and
warnings. The exact staged EXE was launched with its private backend and completed
an uncached NASA analysis while retaining 1x playback and the selected mission start.
Native pause/resume and live UTC behavior were also checked; this was not a repeat
clean-machine installation test. Current details are in
[clock packaging validation](CLOCK_PACKAGING_VALIDATION.md).

Historical aggregate test counts are not presented as newly executed v1.1.0 results.
Public release publication is a separate step from local build verification.

## Impact and future work

The interface makes timing trade-offs accessible to learners and helps technical
teams compare scenarios before detailed terrain and engineering analysis. Future
work could add independently validated LOLA terrain horizons, a broader catalog,
solar-array and energy models, RF link budgets and hosted service options. Each
extension requires its own validation; none is part of the current scope.

## Limitations

No terrain obstruction, landing hazards, slopes, power budgets, antenna/receiver
model, relay service or flight certification is calculated. Sun/Earth center
visibility does not guarantee useful power or reliable communications. Custom
Inspector points and illustrative local geometry must not be interpreted as
terrain-validated landing assessments.

Service coverage runs from 2018-01-01 through 2030-01-01 UTC, with the entire
mission inside coverage. Maximum Inspector duration is 744 hours. Finder uses a total period of at most 744 hours, with each full mission contained
inside it. The legacy API arrival-span mode instead caps span plus mission duration. Cadence is 1–60 minutes; requests are
limited to 10,001 source instants and 1,000 Finder scenarios. Exact cell alignment
and combined limits can further restrict valid requests.

## Sources and further documentation

- [NASA/JPL Horizons API](https://ssd-api.jpl.nasa.gov/doc/horizons.html)
- [Horizons user manual](https://ssd.jpl.nasa.gov/horizons/manual.html)
- [NASA Moon to Mars Lunar Surface Data Book](https://ntrs.nasa.gov/citations/20250002845)
- [NASA/GSFC CGI Moon Kit](https://svs.gsfc.nasa.gov/4720/)
- [Scientific implementation reference](SCIENCE_BACKEND_REFERENCE.md)
- [HTTP contract](../backend/docs/API_CONTRACT.md)
- [Asset attribution](ASSET_ATTRIBUTION.md)

The Markdown proposal is the maintained public revision. Older local PDF/Word
exports are excluded from the release source tree to avoid conflicting versions.
