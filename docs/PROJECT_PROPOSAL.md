# LunarNavigator: Interactive Lunar South-Pole Landing Site Evaluation

*Project proposal for the NASA Space Apps Challenge 2026*

## 1. Executive Summary

LunarNavigator connects interactive lunar exploration with quantitative mission analysis. Users examine south-pole reference locations in a three-dimensional Moon environment, choose a landing date and mission duration, and evaluate sunlight and direct-to-Earth visibility. A dynamic Opportunity Finder searches combinations of location and arrival time against user-defined requirements.

The application combines a Unity interface with a Python scientific service that retrieves genuine NASA/JPL Horizons ephemerides. Its validated release calculates geometric visibility, simultaneous Sun–Earth visibility, and uninterrupted outages using an explicit spherical lunar horizon. Results carry source provenance and a visible `GEOMETRIC_ONLY` classification. The project serves education and preliminary mission comparison while keeping its scientific scope clear.

## 2. Problem Statement

At the lunar south pole, low solar elevations make mission conditions sensitive to location and time. Sunlight availability influences energy planning, while Earth visibility affects opportunities for direct communication. A favorable condition at arrival may change during a mission, making duration as important as the landing date.

These relationships are difficult to communicate through static maps or isolated astronomical tables. Users need to compare complete mission windows, recognize continuous interruptions, and understand how alternative arrival times change the result. They also need a clear distinction between astronomical visibility, actual terrain obstruction, and engineering feasibility.

## 3. Proposed Solution

LunarNavigator places these questions inside an interactive lunar environment. Users move between global exploration and a local surface perspective, select a reference center, and configure a mission through the Dynamic Site Inspector. The interface presents visibility fractions, maximum outages, and Sun/Earth directions alongside an interactive timeline.

The Landing Opportunity Finder extends this workflow from inspection to comparison. Users edit an arrival search window, mission duration, candidate sites, visibility requirements, and outage limits. Ranked recommendations identify qualifying scenarios; selecting one synchronizes the site and mission configuration for detailed inspection. Empty searches report why the requirements were unmet and show the closest misses without presenting them as qualifying results.

## 4. How the System Works

Unity sends the selected location, UTC landing time, and mission duration to the FastAPI service. The backend validates the request and obtains matching Sun and Earth observer ephemerides from Horizons, either through a new HTTPS request or an exact, validated cache entry. Named locations use the service catalog; the API also supports explicit custom coordinates.

Python checks source metadata, timestamps, numerical ranges, and complete coverage before calculating visibility. The JSON response contains aligned directions, visibility states, mission metrics, intervals, and provenance. Unity adapts these results for visualization. Invalid or unavailable data produces an explicit error rather than a substitute astronomical result.

## 5. Scientific and Technical Methodology

Horizons supplies apparent azimuth and elevation for the Sun and Earth, target identifiers 10 and 399, from a custom observer on the Moon. Requests use UTC, east-positive longitude, and airless apparent angles. The catalog contains NASA Lunar Surface Data Book reference centers for Shackleton Rim, Connecting Ridge, and Malapert Massif. These coordinates identify regional reference points, not certified touchdown locations.

The horizon model uses lunar radius R = 1,737,400 meters and observer height h above that nominal sphere:

\[
H=-\arccos\left(\frac{R}{R+h}\right).
\]

Converting H to degrees gives approximately −0.086936572° at the default two-meter height. A target is visible when its center elevation is strictly greater than H. Simultaneous visibility requires both targets to pass.

Visibility fractions are visible mission cells divided by total mission cells. Longest darkness and Earth interruption are the longest consecutive hidden-cell durations. States are held over half-open intervals, so n boundary samples represent n−1 complete cells. Default sampling is ten minutes; transition timing remains cadence-limited.

Finder rejects scenarios that fail minimum Sun/Earth fractions or maximum darkness/Earth-outage durations. Passing scenarios receive

\[
Q=0.4f_{sun}+0.3f_{earth}+0.2f_{both}
+0.1\left(1-\min\left(1,\frac{d_{max}}{D}\right)\right),
\]

where D is mission duration and d_max is longest darkness, both in hours. Best Overall ranks by score; Earliest Qualifying restricts selection to the earliest qualifying sampled arrival. Ties use arrival time and site ID. At most three distinct site-and-arrival pairs are returned. The weights express application preferences, not NASA safety standards.

## 6. Technology Stack

The frontend uses Unity 6.6 (6000.6.3f1), HDRP 17.6.0, C#, UI Toolkit, and Unity Input System. Four camera modes provide global orbit, cinematic approach, local orbit, and surface observation. Timeline controls support playback, scrubbing, UTC readouts, and separate sunlight, Earth-visible, and simultaneous-visibility lanes.

The backend uses CPython 3.11.15, FastAPI, Pydantic, and Uvicorn. Reusable Python functions perform the scientific calculations; standard-library HTTPS, JSON, CSV, and hashing support acquisition and retention. Matplotlib produces diagnostic plots, and Pytest supports verification. Installed geospatial libraries provide a foundation for future terrain processing but are outside the current analytical model.

## 7. Innovation and Distinctive Features

LunarNavigator makes the consequences of mission timing visible. Changing the date or duration changes the scientific request, while the three-dimensional environment gives users spatial context for the returned directions and timelines.

Finder evaluates overlapping missions locally after acquiring one covering Sun/Earth series per site, avoiding a separate NASA query for every arrival. Users can compare scenarios under explicit constraints and two ranking policies. Retained source responses, request metadata, checksums, and visible limitations make the calculation traceable. The combination supports explanation as well as numerical comparison: users can inspect what a recommendation means and what assumptions produced it.

## 8. Verification and Results

The backend release baseline records **156 passing Python tests**, covering geometry, parsing, timestamp integrity, interval boundaries, cache identity, API validation, and ranking. Retained real-source proofs separately establish genuine NASA operation across December 2026 and February 2027: 24-hour and 120-hour requests produced 145 and 721 ten-minute boundary samples respectively. The saved [dynamic API proof](../backend/data/runs/api-live-20261115T185938Z-4b822681/comparison.json) distinguishes their source windows.

The [real Finder verification](../backend/data/runs/release-20261115/T032-evidence.json) evaluated 24 scenarios across three sites. Six qualified and three were returned; the selected Malapert scenarios had full sampled Sun and Earth visibility under the geometric model. These results apply to that search, not all dates.

The [release acceptance report](RELEASE_ACCEPTANCE_REPORT.md) records **5,311 passing Unity automated checks**, including repeated Play Mode checks and live HTTP integration. These are distinct from the Python tests and from executable acceptance. Existing standalone-player logs document launch in illustrative-demo mode; they do not establish final online-release acceptance or portable packaging. The report identifies readiness for a final Windows rebuild; public deployment is not claimed.

## 9. Benefits, Intended Impact, and Applications

For education, LunarNavigator makes astronomical time series easier to explore through spatial interaction and mission timelines. Students can investigate why continuous illumination and Earth visibility differ, and how mission length changes the interpretation of a favorable arrival.

For researchers and technically informed users, it provides a preliminary comparison workflow with explicit assumptions and reproducible source evidence. The intended benefit is faster understanding of timing trade-offs before more detailed terrain and engineering analysis. A shared visual interface also helps teams explain these trade-offs to audiences who do not work directly with ephemeris tables.

## 10. Scope and Scientific Limitations

The release is `GEOMETRIC_ONLY`. Its spherical horizon excludes local mountains and crater walls. No validated LOLA terrain-obstruction model is implemented. NASA imagery supports global rendering; illustrative local terrain is not certified landing-site geometry and does not determine scientific visibility. Sun-center visibility is not a solar-power calculation, and Earth-center visibility is not a communications link budget. No landing-safety certification is implied.

Finder supports three reference centers; custom Inspector coordinates remain explicitly unverified. Supported landing years are 2018–2029, with complete coverage ending no later than January 1, 2030 UTC. Inspector missions are capped at 744 hours; Finder's search span plus mission duration shares that cap. Cadence is 1–60 minutes, with at most 10,001 source instants and 1,000 Finder scenarios. Whole-cell alignment and combined limits can further restrict requests. Operation requires accessible Horizons or matching authentic cached data.

## 11. Future Development

Future work includes validated terrain-aware skylines from LOLA elevation products, an expanded sourced catalog, and additional exploration tools. Secure public backend hosting would extend access beyond local operation. More complete mission models could incorporate solar-array behavior, communications engineering, and environmental factors. Each extension requires its own scientific validation; none is part of the current release's demonstrated capability.

## 12. Official NASA Data and References

The analytical sources are [NASA/JPL Horizons documentation](https://ssd-api.jpl.nasa.gov/doc/horizons.html), its [observer manual](https://ssd.jpl.nasa.gov/horizons/manual.html), and the [NASA Moon to Mars Lunar Surface Data Book](https://ntrs.nasa.gov/citations/20250002845), Table A2.1-1, for reference-center coordinates.

Global rendering maps are credited to NASA/GSFC Scientific Visualization Studio/LRO/LROC through the [CGI Moon Kit](https://svs.gsfc.nasa.gov/4720/). Their visual use does not validate terrain-aware analysis. Asset provenance is retained in [Documentation/Sources/ATTRIBUTION.md](../Documentation/Sources/ATTRIBUTION.md).

For full equations, assumptions, operating limits, source paths, and retained validation evidence, see [docs/SCIENCE_BACKEND_REFERENCE.md](SCIENCE_BACKEND_REFERENCE.md).
