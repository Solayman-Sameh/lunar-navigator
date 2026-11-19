# LunarNavigator live API — lunar-analyze-v1

Status: FROZEN FOR FINAL HTTP INTEGRATION — 2026-11-15. Both /analyze
December/February proof and T032 /opportunities real Windows NASA proof PASS.
Independent source replay: backend/data/runs/release-20261115/T032-evidence.json.
Working route/field names and v1 profiles are unchanged. This document is the
HTTP integration authority; the separate legacy file contract is unchanged.
This API is a separate live profile. It does not revise docs/DATA_CONTRACT.md or
publish any files into Unity. FastAPI OpenAPI and Swagger are available at
`/openapi.json` and `/docs`; the generated request schema is retained alongside
this document in `api-openapi.json`.

## Running the service

From the project root in normal Windows PowerShell:

```powershell
& '.\backend\.venv\Scripts\python.exe' -m uvicorn science.api:app --app-dir backend --host 127.0.0.1 --port 8000
```

No test fixture or mock provider is imported by the production app. NASA uses
the existing standard-library HTTPS client with its default proxy and verified
TLS settings. No static CSV is read. A request either uses an exact validated
real cache identity or fetches matching genuine observations.

## GET /health

Returns 200 with `status="ok"`, `api_profile="lunar-analyze-v1"`,
`data_status="GEOMETRIC_ONLY"`, service UTC start/stop, maximum mission hours
and maximum instants. Health means local service availability; it deliberately
does not probe NASA or claim upstream availability.

## POST /analyze — exact request

One JSON object; unknown fields are rejected. Supply `site_id` OR both explicit
coordinates. Do not supply both forms. The three named reference centers returned
by /sites have source-verified coordinates. Their ground heights, terrain and
touchdown suitability are not verified. Explicit coordinates also work without a DEM.

| Field | Type | Rule |
| --- | --- | --- |
| site_id | string or omitted/null | 1–64 characters; must identify a verified catalog point |
| latitude_deg | number or omitted/null | strictly -90 < latitude < 90, north positive; required with explicit longitude |
| longitude_deg | number or omitted/null | -180 through 180 degrees east positive; never silently normalized |
| landing_utc | string, required | exactly YYYY-MM-DDTHH:MM:SSZ; explicit UTC including time; no snapping |
| search_window_mode | CONTAINED_MISSION (updated Unity app) or ARRIVAL_SPAN (legacy API default) |
| mission_duration_h | number, required | finite, >0 and <=744 hours; duration in seconds must be a positive whole number of selected cells |
| observer_height_m | number | default 2.0; finite 0–1000 m above the nominal source sphere, not verified terrain ground |
| sampling_step_minutes | integer | default 10; 1–60; booleans/fractions rejected |

Numeric fields reject booleans, numeric strings, NaN and Infinity. Entire mission
must lie in **[2018-01-01T00:00:00Z, 2030-01-01T00:00:00Z]**. These are this
service's bounded support limits, not Horizons' full capabilities. At most
10,001 endpoint-inclusive instants per target (10,000 complete cells) are allowed.
Two requests can compute concurrently; further simultaneous requests return 429.
Known leap-insertion intervals are rejected by the existing query model; future
NASA UTC/EOP assumptions are retained in source metadata. Arbitrary supported
dates, time-of-day and coordinates affect actual query parameters and cache keys.

Valid request example (input example only; not a claimed live NASA result):

```json
{
  "latitude_deg": -89.5,
  "longitude_deg": 0.0,
  "landing_utc": "2027-02-15T06:00:00Z",
  "mission_duration_h": 120,
  "observer_height_m": 2.0,
  "sampling_step_minutes": 10
}
```

Its exact source window is February 15 at 06:00 through February 20 at 06:00 UTC:
721 instants and 720 complete cells. A cached December response cannot satisfy it.

## Successful response — exact object layout

All numbers retain binary64 round-trip precision; no presentation rounding is
applied. Arrays described below contain numbers or integer 0/1, never unknown
placeholders. All timestamps are explicit UTC strings. `n` includes the final
boundary; metrics use n-1 left-held half-open cells. Visibility is strict
`elevation > -degrees(acos(1737400/(1737400+observer_height_m)))` for apparent
target centers; zero mast height gives a 0-degree horizon. No refraction or DEM.

| Object | Exact fields and types |
| --- | --- |
| root | schema_version integer=1; api_profile string="lunar-analyze-v1"; data_status string="GEOMETRIC_ONLY"; algorithm_version string; run_id string; generated_utc UTC string; site, request, ephemeris, terrain, visibility, metrics, provenance objects; limitations string array |
| site | id string (empty for custom); lat_deg, lon_east_deg, observer_height_m numbers; coordinate_status string (custom UNVERIFIED diagnostic or verified catalog with nominal sphere observer) |
| request | landing_utc, mission_end_utc UTC strings; mission_duration_h number; sampling_step_minutes integer |
| ephemeris | source string; t0_utc UTC string; step_s integer; n integer; complete_cells integer=n-1; utc UTC string array of length n; sun_az_deg, sun_el_deg, earth_az_deg, earth_el_deg numeric arrays of length n |
| terrain | source string identifying no terrain/nominal sphere; az_step_deg number=1.0; horizon_el_deg 360-number array; incomplete_sectors empty object array |
| visibility | sun_margin_deg number=0; sun_visible, earth_visible, both_visible integer 0/1 arrays of length n; horizon_convention string="spherical_dip"; horizon_deg number |
| metrics | f_sun, f_earth, f_both, duration_h, horizon_deg, sun_margin_deg, dark_max_h, comm_gap_max_h numbers; start_utc/end_utc UTC strings; cells and step_s integers; horizon_convention string; event_scope/time_model strings; events object; sun_visible_intervals, earth_visible_intervals, both_visible_intervals object arrays; visibility_changes object array |
| each visible interval | start_utc, end_utc UTC strings, end exclusive and clipped to the requested mission |
| each visibility change | utc UTC string; sun_visible, earth_visible, both_visible integers; changes at mission end are excluded |
| metrics.events | sun, earth, both objects, each with next_loss and next_recovery event objects |
| each event | status string and utc UTC string or null; FUTURE_LOSS/FUTURE_RECOVERY also includes crossing_bracket_utc of two UTC strings; other statuses ALREADY_HIDDEN, ALREADY_VISIBLE, NONE_WITHIN_COVERAGE. Events search only acquired coverage, without extrapolation. |
| provenance | sun and earth source objects described below |
| each source object | target_id integer (10/399); cache_key/raw_sha256/raw_cache_file/retrieved_utc strings; cache_hit boolean; source_start_utc/source_end_utc UTC strings; source_n integer; request_parameters and metadata maps of strings to strings |

Original raw envelopes, source result text, exact request maps, timestamps and
checksums are retained below backend/data/api. Each API run also records request,
response or error evidence. Cache identity includes coordinates, mast altitude,
frame/model, target, start, stop, cadence, parameters and algorithm version.
Corrupt cache entries are rejected and refreshed once, never silently served.
Failure of either target prevents a successful analysis response. Only exact
aligned pairs are combined; missing rows are never intersected away.

## Error responses

All handled errors use `{"detail":{"code":"...","message":"..."}}`.
Upstream errors additionally include run_id and synthetic_fallback=false.

| HTTP | Code | Meaning |
| --- | --- | --- |
| 422 | invalid_request | malformed/extra fields, nonfinite values, bad coordinates/date, duration/cadence/row/date-range limits |
| 422 | unknown_site / unverified_site | invalid catalog ID or no verified observing point; use explicitly labeled custom coordinates |
| 422 | unsupported_request | query-model validation rejected the requested interval, e.g. unsupported leap insertion |
| 429 | busy | two analysis slots occupied; retry later |
| 503 | horizons_unavailable | network/timeout/upstream transient failure without a suitable matching real cache |
| 502 | invalid_horizons_data | bad signature/header/body/observer/coverage or an invalid source response; no synthetic fallback |
| 500 | storage_error | evidence/cache storage failed; no certified result returned |

The existing parser currently recognizes the observed NASA API 1.2 profile.
Other versions are retained and rejected pending review; no signature relabeling.

## Unity adapter mapping — coordination still required

| Live API | Existing Unity file model |
| --- | --- |
| site.lat_deg/lon_east_deg/observer_height_m | corresponding site fields; custom ID stays empty unless a diagnostic display label is explicitly created by the consumer |
| ephemeris.t0_utc/step_s/n and four angle arrays | same-named ephemeris fields; utc is live-only convenience |
| terrain.horizon_el_deg/az_step_deg/incomplete_sectors | same-named terrain fields; source explicitly says sphere/no DEM |
| visibility.sun_margin_deg/sun_visible/earth_visible | same-named visibility fields; both_visible can be retained by a live adapter |
| metrics | single requested scenario; a consumer adapter may construct its corresponding metric object, never pretend this is the nine-window static release |
| provenance/limitations/request/coordinate_status | live response metadata; must be exposed/preserved by the adapter, not interpreted as terrain certification |

No index.json or static reference_metrics set is returned or published. Do not
deserialize this envelope as if it were an unchanged legacy release. The Unity
owner must add the live adapter and reconcile the already approved binary64,
n-1 cell, complete-cohort and observer conventions. This task edits no C#.

## Mandatory Windows live proof

One command starts a real server on a free localhost port, issues December 2026
and February 2027 requests, saves both responses, independently parses retained
NASA source bytes, compares date windows/cache identities and stops its own server:

```powershell
# Run from the repository root.
& '.\backend\.venv\Scripts\python.exe' '.\backend\live_api_proof.py'
```

It prints a unique backend/data/runs/api-live-* directory containing requests,
december-response.json, february-response.json, uvicorn.log and comparison.json.
A failure preserves error bodies and returns exit 4. A cached response is accepted
only if its actual original bytes validate against that exact requested window;
cache-hit flags are reported rather than claiming a fresh HTTP request. Numeric
mission metrics can legitimately match across dates; source windows, keys and
raw hashes must differ. No mock data or December CSV is used by this script.

`--health-only` exercises real Uvicorn/local HTTP without NASA and cannot establish
the live science milestone. Run normal Windows PowerShell outside the restricted
Codex execution environment. Keep TLS and proxy security settings intact.


## GET /sites — lunar-sites-v1

No request parameters. HTTP 200 object: schema_version=1, api_profile="lunar-sites-v1",
data_status="GEOMETRIC_ONLY", sites object array, limitations string. Only source-verified
supported coordinate records are returned, sorted by id. Each site has exactly:
id, name, lat_deg, lon_east_deg, verified, point_source, coordinate_frame,
coordinate_convention, reference_description, verification_scope. Numbers retain
source precision; verified is boolean. Other fields are strings. These are
NASA-tabulated polygon reference centers, not certified touchdown spots or ground
heights. Stable IDs remain shackleton_rim, connecting_ridge, malapert_mountain.
Use GET /sites coordinates rather than the differing pre-existing Unity assets.

## POST /opportunities — lunar-opportunities-v1

Request fields (unknown fields rejected; finite numeric fields, no numeric strings
or booleans):

| Field | Type / constraints / default |
| --- | --- |
| earliest_landing_utc | required explicit UTC string including seconds/Z, service date range as /analyze |
| search_window_h | required number >0, <=744; total enclosing period in CONTAINED_MISSION; candidate-start span in ARRIVAL_SPAN |
| mission_duration_h | required number >0, <=744; independently evaluated whole mission |
| candidate_site_ids | required array of 1–3 distinct verified supported string IDs; unknown/unverified rejects 422 before NASA retrieval |
| min_sun_fraction | number in [0,1], default 0.30 |
| min_earth_fraction | number in [0,1], default 0.40 |
| max_dark_h | number in [0,744], default 36 |
| max_comm_gap_h | number in [0,744], default 48 |
| search_step_h | positive number <=744, default 6; never snapped |
| ranking_policy | BEST_OVERALL (default) or EARLIEST_QUALIFYING |
| observer_height_m | number [0,1000], default 2 |
| sampling_step_minutes | integer [1,60], default 10 |

Window, step and mission durations must each be whole source-cell multiples.
In CONTAINED_MISSION, the stay must be <= search_window_h and coverage equals
search_window_h. In legacy ARRIVAL_SPAN, coverage equals search_window_h +
mission_duration_h. Coverage must be <=744 hours, within the
service UTC bounds, and <=10001 instants per target. At most 1000 site-arrival
scenarios are accepted. All fields are actually used. Source coverage is acquired
once per site from earliest_landing_utc through earliest + coverage,
not once per arrival. Only an exactly matching validated cache may substitute for
network acquisition. At most two fresh target requests per site normally occur;
identical searches can use zero HTTP calls. Both targets must validate for every
requested site, otherwise the request fails, with no fabricated partial ranking.

In CONTAINED_MISSION, arrivals are L0+j*search_step with j>=0 and
arrival+mission_duration <= L0+search_window (equality included on the grid).
In ARRIVAL_SPAN, arrival UTCs are L0+j*search_step strictly less than L0+search_window. Each full
mission [arrival, arrival+D) uses n-1 complete cells, never an extra terminal cell.
A start at the search stop is excluded, but the last permitted mission is fully
calculated. No longitude/date/site/mission stub or fixed December CSV is used.

Valid request example (not a claimed live Finder result):

```json
{
  "earliest_landing_utc":"2027-02-15T06:00:00Z",
  "search_window_h":48,
  "mission_duration_h":24,
  "candidate_site_ids":["connecting_ridge","shackleton_rim","malapert_mountain"],
  "min_sun_fraction":0.30,
  "min_earth_fraction":0.40,
  "max_dark_h":36,
  "max_comm_gap_h":48,
  "search_step_h":6,
  "ranking_policy":"BEST_OVERALL",
  "observer_height_m":2,
  "sampling_step_minutes":10
}
```

Successful response exact fields:

| Object | Fields |
| --- | --- |
| root | schema_version=1, api_profile="lunar-opportunities-v1", data_status="GEOMETRIC_ONLY", run_id string, request object echoing all validated fields/defaults, candidate_sites string array, sources object array, arrival_count_per_site and source_pair_count integers, score_formula and constraints_note strings, limitations string array, recommendations and closest_misses object arrays, evaluated_count/rejected_count/qualifying_count integers, empty_reason string or null |
| each source | site_id string, coverage_start_utc/coverage_end_utc UTC strings, n integer, provenance object with sun/earth source objects using the exact /analyze provenance schema |
| each recommendation | rank integer 1..3, site_id/site_name strings, landing_utc/mission_end_utc UTC strings, mission_duration_h number, metrics object using the exact /analyze metrics schema, score number, explanation string |
| each closest miss | site_id/site_name, landing_utc/mission_end_utc, mission_duration_h, metrics as above; failed_constraints string array and normalized_violation number; no qualifying rank or score |

All enabled hard constraints pass before scoring: fractions >= minima, outages <=
maxima, equality passes. Presets are illustrative/user-editable, never NASA standards.
Score = .4*f_sun + .3*f_earth + .2*f_both + .1*(1-min(1,dark_max_h/duration_h)).
BEST_OVERALL sorts score descending, landing UTC ascending, ordinal site ID ascending;
repeat sites at different arrivals are allowed, but pairs are distinct.
EARLIEST_QUALIFYING finds the earliest qualifying sampled UTC and returns only sites
at that instant, ranked by score then ID. Both policies return at most three; never pad.
Counts cover ALL requested site-arrival scenarios, before top-three selection;
qualifying_count = evaluated_count - rejected_count. source_pair_count means number
of site datasets, not number of fresh HTTP calls. A known empty search returns
200, recommendations=[], and a clear empty_reason, with up to five closest misses.
Misses sort by failure count, normalized violation, UTC and ID. Violation is the sum
of fraction shortfalls plus outage excesses divided by mission duration (never by
a possibly zero outage threshold). Missing ephemerides are service errors, not misses.
The existing 422/429/502/503/500 error envelope applies; invalid scientific coverage
fails the search, and no filter is silently ignored.

## Windows Finder acceptance and Gemini fixtures

From ordinary Windows PowerShell outside Codex:

```powershell
# Run from the repository root.
& '.\backend\.venv\Scripts\python.exe' '.\backend\live_finder_proof.py' --landing-utc '2027-02-15T06:00:00Z' --search-utc '2026-12-01T00:00:00Z' --mission-hours 24 --window-hours 48
```

This starts real Uvicorn on a free localhost port, calls /sites, /analyze using the
chosen landing date, and /opportunities for the separate search range. It saves
all request/response JSON, errors, logs and proof.json under a unique finder-live-*
directory. It parses retained real source bytes for all Finder sites and replays
any returned recommendations. Empty qualifying results are valid and are reported
honestly. --health-only checks real local /sites without NASA and is NOT live Finder
acceptance. Edit the request fields in the API/Swagger for all thresholds/policies.
The service can also be started persistently with the Uvicorn command above.
Gemini should use /sites IDs and coordinates, consume recommendations as authoritative
full-precision ordering, render GEOMETRIC_ONLY and point/ground limitations, handle
empty lists/errors explicitly, and avoid treating this live response as a legacy
static opportunity file. No Unity C# or authoritative file contract was edited.


## Frozen real JSON fixtures for Unity integration

These are complete, valid JSON documents, including every time-series element and
provenance field—no ellipses, fabricated astronomical values or schema-only stubs.
GET requests have no JSON request body. POST examples were actually accepted by
the real Windows service. Empty-result fixtures were calculated from the same
genuine retained source with an exact matching cache; no HTTP or synthetic source
substitution occurred during that offline verification.

| Route / case | Exact request JSON | Exact response JSON |
| --- | --- | --- |
| GET /health | no body | [health-response.json](api-examples/health-response.json) |
| GET /sites | no body | [sites-response.json](api-examples/sites-response.json) |
| POST /analyze | [analyze-request.json](api-examples/analyze-request.json) | [analyze-response.json](api-examples/analyze-response.json) |
| POST /opportunities, qualifying | [opportunities-request.json](api-examples/opportunities-request.json) | [opportunities-response.json](api-examples/opportunities-response.json) |
| POST /opportunities, empty | [empty-opportunities-request.json](api-examples/empty-opportunities-request.json) | [empty-opportunities-response.json](api-examples/empty-opportunities-response.json) |

The exact empty example has recommendations=[], evaluated_count=16,
rejected_count=16, qualifying_count=0 and empty_reason="No scenario satisfies all
four hard constraints." Its closest_misses and real source provenance remain in
the complete JSON. Empty results are successful evaluated science, not an API
failure. Errors have non-2xx status and the documented detail envelope instead.

## Final Gemini integration requirements

**Synchronize Unity selection/markers with GET /sites before final scientific
validation. All three existing Unity coordinate pairs differ by >0.05 degrees.**
Use these exact source-cited reference centers (not certified touchdown points):
shackleton_rim=(-89.7668,-171.87), connecting_ridge=(-89.4632,-137.49),
malapert_mountain=(-85.9948,-0.23578), all latitude/east longitude in degrees.
Do not use a Unity asset's zero elevation as verified lunar ground.

| Feature | Required live response mapping |
| --- | --- |
| Dynamic Site Inspector | POST chosen site_id OR explicit coordinates plus landing_utc, mission_duration_h, observer_height_m and sampling_step_minutes; bind response.site and response.request, not a previous selection's data |
| Directional visualization | response.ephemeris.utc/t0_utc/step_s/n and sun_az_deg/sun_el_deg/earth_az_deg/earth_el_deg; azimuth clockwise north=0/east=90, elevation positive above local horizontal. Rendering vector ENU is (cos(el)*sin(az), cos(el)*cos(az), sin(el)); convert degrees to radians and adapt ENU to Unity axes. Preserve scientific doubles before rendering conversion. |
| Visibility metrics | response.metrics.f_sun/f_earth/f_both fractions 0..1; dark_max_h/comm_gap_max_h in hours; response.visibility integer arrays are authoritative model states |
| Mission timeline | utc and visibility arrays contain n instants but only n-1 complete left-held cells. Use [landing,end), require a+m<=n-1; end boundary adds no duration. Use metrics.*_visible_intervals and metrics.events statuses without extrapolating. Hourly/ten-minute cadence limits transition precision. |
| Dynamic Opportunity Finder | POST editable search fields; display response.recommendations in returned rank order. Each item has site_id/site_name, landing_utc, mission_end_utc, mission_duration_h, metrics, score and explanation. Selection should request /analyze for that returned scenario; do not fill an EARLIEST_QUALIFYING list with later arrivals. |
| Finder empty/errors | Empty recommendations: display empty_reason and optional closest_misses.failed_constraints. HTTP 422/429/502/503/500: display error, never silently restore old December data as the new result. |
| Scientific badge | Render root.data_status exactly: GEOMETRIC_ONLY. Also preserve site verification_scope/coordinate_status and source limitations; no terrain-aware upgrade or touchdown endorsement. |

No C# adapter or shared file-contract change was made by the Python agent.
Use the live profile schemas above; these envelopes are not legacy index/site/
opportunities release files. Final static-file amendment coordination remains
separate and does not block this explicit live HTTP adapter.

## Final integration smoke checklist

1. Start normal Windows Uvicorn on localhost; GET /health and GET /sites succeed.
2. Confirm Unity's three marker/selection coordinates equal GET /sites.
3. Submit December and February /analyze requests; request times and source windows
   differ, n/cell counts match, and a new UI request never shows stale data as fresh.
4. Submit /opportunities with two different durations and thresholds; verify returned
   rank order and scenario selection, and verify the provided empty-result fixture.
5. Check north/east/up orientation, UTC labels, half-open timeline coverage, scientific
   doubles and GEOMETRIC_ONLY badge. Simulate a NASA error and show a meaningful
   unavailable/error state rather than synthetic or stale results.

Deferred: DEM/terrain visibility, cloud deployment, Unity HTTP/UI implementation,
legacy static-release publication, and physical power/RF/landing-safety validation.
