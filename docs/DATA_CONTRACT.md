# LunarNavigator: Shared Data Contract (v1)

Both Codex threads (Python science, Unity) read this file. **Neither may change it alone.** If a change is needed, the agent stops, writes the proposed change at the bottom under "Change requests", and tells the user. The user relays it.

Place this file at `docs/DATA_CONTRACT.md` in the repo.

---

## 1. Ownership of paths (prevents merge conflicts)

| Path | Owner | Notes |
|---|---|---|
| `backend/**`, `docs/scientific_method.md`, `docs/validation_report.md` (science rows), `docs/sources_and_credits.md`, `docs/img/**` | **Python agent** | Unity agent never edits these. |
| `Assets/StreamingAssets/analysis/*.json` **except** `stub_*.json` | **Python agent** | Real data only. |
| `Assets/StreamingAssets/analysis/stub_*.json` | **Unity agent** | Synthetic data, `ILLUSTRATIVE_DEMO` only. |
| `Assets/**` (everything else), `ProjectSettings/**`, `Packages/**`, `Builds/**`, `docs/unity_*.md` | **Unity agent** | Python agent never edits these. |
| `README.md`, `docs/DATA_CONTRACT.md` | **User** (agents propose) | |

Git rules (both agents, same working tree, same branch):
- Commit with **explicit paths only**: `git add <your paths>` then `git commit -m "<id>: <msg>"`. Never `git add -A`, never switch branches, never `git stash`, never reset someone else's files.
- If `git` reports `index.lock`, wait 5 seconds and retry.
- When Unity is open, new JSON files get `.meta` files. The Unity agent commits those `.meta` files.

---

## 2. Conventions (non-negotiable)

- Latitude in degrees, north positive (south pole region is negative).
- Longitude in degrees, **east positive**, range -180..180 in `sites.json`. (If a source uses 0..360, convert and say so in provenance.)
- Azimuth in degrees **clockwise from north** in the local site frame, range 0..360 (360 is stored as 0).
- Elevation in degrees above the local horizontal plane, positive up.
- Times are UTC. Sample `i` is at `t0_utc + i * step_s`. Do not store a timestamp array.
- Observer height in meters above the DEM surface (default 2.0).
- Floats rounded to 3 decimals. No NaN or Infinity anywhere (use the `incomplete_sectors` list for missing terrain).
- Arrays are **parallel arrays of primitives**. No dictionaries, no top-level JSON arrays, no nested arrays (Unity `JsonUtility` cannot parse them).
- Booleans are stored as integers `0` or `1`.
- Files are UTF-8, no BOM.

## 3. data_status values (shown to the user in a badge)

| Value | Meaning | Badge text |
|---|---|---|
| `ILLUSTRATIVE_DEMO` | Synthetic. Used only by Unity stubs. | ILLUSTRATIVE DEMO |
| `GEOMETRIC_ONLY` | Real Horizons tracks, flat skyline (no terrain). | GEOMETRIC HORIZON ONLY |
| `TERRAIN_AWARE_ESTIMATE` | Real Horizons + real LOLA DEM skyline, tests passed. | TERRAIN-AWARE ESTIMATE |

Python must never emit `TERRAIN_AWARE_ESTIMATE` unless the DEM skyline tests in the Python plan passed. Unity must always display the value from the file and never upgrade it.

## 4. Files, all under `Assets/StreamingAssets/analysis/`

### 4.1 `index.json`
```json
{
  "schema_version": 1,
  "sites": [
    { "id": "shackleton_rim", "name": "Shackleton Rim", "file": "shackleton_rim.json", "data_status": "TERRAIN_AWARE_ESTIMATE" }
  ],
  "opportunities_file": "opportunities.json"
}
```
`opportunities_file` is an empty string `""` if the file does not exist. Unity reads `index.json` first. If `index.json` lists only `stub_*` entries, Unity is in demo mode.

### 4.2 `<site_id>.json`  (stub files are named `stub_<site_id>.json`)
```json
{
  "schema_version": 1,
  "data_status": "TERRAIN_AWARE_ESTIMATE",
  "algorithm_version": "0.1.0",
  "generated_utc": "2026-11-15T17:00:00Z",
  "site": {
    "id": "shackleton_rim",
    "name": "Shackleton Rim",
    "lat_deg": -89.6,
    "lon_east_deg": 129.0,
    "observer_height_m": 2.0,
    "provenance": "citation or URL"
  },
  "ephemeris": {
    "source": "JPL Horizons API, OBSERVER, CENTER=coord@301, APPARENT=AIRLESS",
    "t0_utc": "2026-12-01T00:00:00Z",
    "step_s": 600,
    "n": 4465,
    "sun_az_deg": [],
    "sun_el_deg": [],
    "earth_az_deg": [],
    "earth_el_deg": []
  },
  "terrain": {
    "source": "exact DEM product name, or 'none (flat horizon)' for GEOMETRIC_ONLY",
    "resolution_m": 100,
    "max_range_km": 150,
    "az_step_deg": 1.0,
    "horizon_el_deg": [],
    "incomplete_sectors": [ { "az_from_deg": 200.0, "az_to_deg": 215.0 } ]
  },
  "visibility": {
    "sun_margin_deg": 0.0,
    "sun_visible": [],
    "earth_visible": []
  },
  "reference_metrics": [
    {
      "start_utc": "2026-12-01T00:00:00Z",
      "duration_h": 72,
      "f_sun": 0.0, "f_earth": 0.0, "f_both": 0.0,
      "dark_max_h": 0.0, "comm_gap_max_h": 0.0
    }
  ]
}
```
Constraints:
- `sun_az_deg`, `sun_el_deg`, `earth_az_deg`, `earth_el_deg`, `sun_visible`, `earth_visible` all have exactly `n` entries.
- `horizon_el_deg` has exactly 360 entries; entry `k` is the skyline at azimuth `k` degrees (`az_step_deg` is always 1.0 in v1).
- `sun_visible[i] = 1` iff `sun_el_deg[i] > interp(horizon_el_deg, sun_az_deg[i]) + sun_margin_deg` (linear interpolation, periodic at 360). `earth_visible` uses margin 0.
- `reference_metrics` definitions (sample `i` covers `[t_i, t_i + step)`; a window starting at index `a` of `m = duration_h*3600/step_s` samples covers samples `a..a+m-1`):
  - `f_sun` = mean of `sun_visible` over the window; `f_earth`, `f_both` likewise (`both = sun & earth`).
  - `dark_max_h` = longest run of `sun_visible == 0` in the window, times `step_s/3600`.
  - `comm_gap_max_h` = longest run of `earth_visible == 0` in the window, times `step_s/3600`.
- The window must fit inside the data (`a + m <= n`). Otherwise it is an error, not truncated.

### 4.3 `opportunities.json`
```json
{
  "schema_version": 1,
  "algorithm_version": "0.1.0",
  "data_status": "TERRAIN_AWARE_ESTIMATE",
  "request": {
    "earliest_landing_utc": "2026-12-01T00:00:00Z",
    "search_days": 14,
    "landing_step_h": 6,
    "duration_h": 72,
    "constraints": {
      "min_sun_fraction": 0.30,
      "min_earth_fraction": 0.40,
      "max_dark_h": 36.0,
      "max_comm_gap_h": 48.0
    },
    "constraints_note": "Illustrative, user-editable. Not NASA standards."
  },
  "candidate_sites": ["malapert_mountain", "shackleton_rim", "connecting_ridge"],
  "scoring_note": "score = 0.4*f_sun + 0.3*f_earth + 0.2*f_both + 0.1*(1-min(1,dark_max_h/duration_h))",
  "best_overall":        [ { "rank": 1, "site_id": "", "landing_utc": "", "duration_h": 72, "score": 0.0, "f_sun": 0.0, "f_earth": 0.0, "f_both": 0.0, "dark_max_h": 0.0, "comm_gap_max_h": 0.0 } ],
  "earliest_qualifying": [ { "rank": 1, "site_id": "", "landing_utc": "", "duration_h": 72, "score": 0.0, "f_sun": 0.0, "f_earth": 0.0, "f_both": 0.0, "dark_max_h": 0.0, "comm_gap_max_h": 0.0 } ],
  "closest_misses": [ { "site_id": "", "landing_utc": "", "failed": ["min_sun_fraction"], "f_sun": 0.0, "f_earth": 0.0, "f_both": 0.0, "dark_max_h": 0.0, "comm_gap_max_h": 0.0 } ],
  "evaluated_count": 0,
  "rejected_count": 0
}
```
Arrays may be empty. An empty `best_overall` means "no qualifying opportunities". Both lists contain at most 3 entries.

## 5. Site IDs (v1)
`malapert_mountain`, `shackleton_rim`, `connecting_ridge` (match the Unity `Site_1..3.asset` entries). The Python agent owns the verified coordinates in `backend/data/sites.json` and reports any mismatch with the Unity assets to the user.

## 6. Validation
`python backend/science/validate_contract.py <file.json>` (Python agent writes it) checks every rule above. The Unity agent may run it on stubs once it exists, but does not depend on it.

## 7. Change requests
(none yet)
