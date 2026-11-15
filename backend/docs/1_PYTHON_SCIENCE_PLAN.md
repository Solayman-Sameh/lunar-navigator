# LunarNavigator: Track A, Python Science Engine

**Audience:** Codex thread 1 (Python). You work in parallel with a Unity thread that you never talk to directly.
**Project root:** `C:\Users\user\Documents\Unity\LunarNavigator`  **Your folder:** `backend/`
**Deadline:** Sunday 15 Nov 2026, 11:59 PM local. **Your hard stop: all real JSON delivered by 8:30 PM. Feature freeze 9:00 PM.**
**Read first:** `docs/DATA_CONTRACT.md` (the data contract). Obey its path ownership and git rules.

---

## 0. Mission and rules

Produce real, verified Sun/Earth visibility data for 3 south-polar candidate sites and write it as JSON files the Unity app reads. You do not touch Unity code.

1. **No fabricated science.** Every number comes from a real Horizons response or a real LOLA DEM that you actually downloaded and processed. If a step fails, use the stated fallback and set the honest `data_status`. Never invent values, coordinates, file names, or URLs.
2. **Verify, do not assume.** Anything marked **VERIFY** must be checked against the official documentation or the actual file, and the result recorded in `docs/validation_report.md`.
3. **Evidence log.** Maintain `docs/validation_report.md` as a table: `ID | check | status (PASS/FAIL/NOT RUN) | evidence file`. Never write PASS for something you did not execute.
4. **Timebox.** If a step takes more than 1.5x its budget, apply its fallback and move on.
5. **Commit after each step** with explicit paths: `git add backend docs Assets/StreamingAssets/analysis && git commit -m "S3: horizons client"`.
6. **Report progress** by appending one line per finished step to `docs/science_status.md` (`time | step | PASS/FAIL | note`). The user reads this file.
7. Keep code simple, typed, and tested. No frameworks beyond the listed libraries. **No FastAPI** (not needed; Unity reads files).
8. Use `pathlib`, UTF-8, and `argparse` CLIs. All CLIs run from `backend/` with the venv active.

---

## 1. Step list (in order)

| Step | Goal | Budget | Priority |
|---|---|---|---|
| S0 | Environment | 15 min | P0 |
| S1 | Run the existing probe | 10 min | P0 |
| S2 | Site catalog | 15 min | P0 |
| S3 | Production Horizons client + sanity tests | 40 min | P0 |
| S4 | DEM acquisition | 40 min | P0 |
| S5 | Skyline computation + tests | 50 min | P0 |
| S6 | Visibility and metrics + tests | 30 min | P0 |
| S7 | Export + contract validator | 30 min | P0 |
| S8 | Validation and sensitivity | 25 min | P0 |
| S9 | Opportunity Finder | 50 min | P1 |
| S10 | Science docs for submission | 25 min | P0 |
| S11 | Handoff and integration support | ongoing | P0 |

Early delivery matters: after **S3** deliver a `GEOMETRIC_ONLY` set of JSON files (flat skyline) so Unity can switch from stubs to real Sun/Earth data early. Upgrade the same files to `TERRAIN_AWARE_ESTIMATE` after S5/S6 pass.

---

## S0. Environment (P0)

Windows PowerShell:

```powershell
cd C:\Users\user\Documents\Unity\LunarNavigator
mkdir backend, backend\science, backend\tests, backend\data, backend\data\cache, backend\data\dem, docs, docs\img -Force
cd backend
py --list
py -3.12 -m venv .venv        # use 3.11 if 3.12 is absent; avoid 3.13 for GDAL wheels
# If activation is blocked: Set-ExecutionPolicy -Scope Process Bypass
.\.venv\Scripts\Activate.ps1
python -m pip install --upgrade pip
pip install numpy pandas scipy requests rasterio pyproj matplotlib pytest
pip freeze > requirements.txt
python -c "import numpy, pandas, scipy, requests, rasterio, pyproj, matplotlib; print('imports ok', rasterio.__version__, pyproj.__version__)"
```

Add to the root `.gitignore` (create if missing, append, never overwrite): `backend/.venv/`, `backend/data/dem/`, `__pycache__/`, `*.pyc`, `.pytest_cache/`.
Create `backend/science/__init__.py` and `backend/pytest.ini` containing `[pytest]` and `pythonpath = .`.

**Done when:** the import line prints `imports ok`; `pytest` runs without error (0 tests is fine). If `rasterio` will not install, **stop and tell the user**; do not try to compile GDAL.

---

## S1. Run the existing probe (P0)

A file `horizons_probe.py` was written earlier and has **never been executed**. Copy it to `backend/horizons_probe.py` if it is not there, then run:

```powershell
python horizons_probe.py --lon 0 --lat -89.5 --start 2026-12-01 --days 3 --step-hours 1 --out data/cache/probe
```

Check: it prints Sun and Earth row counts, saves raw responses and `combined_geometric_only.csv`.
Open one raw response and read the table header; record the **exact column names** you see (azimuth and elevation) in `docs/validation_report.md` (row `S1-columns`). If the request errors, print the first 2,000 characters of the returned text, fix the parameter named in the error message using the Horizons documentation `https://ssd-api.jpl.nasa.gov/doc/horizons.html` (**VERIFY** each parameter, do not guess), and re-run.

Known weaknesses of the probe that S3 must fix:
- Integer-hour steps only (we need 10 minutes).
- No `TIME_TYPE='UT'` set explicitly (**VERIFY** the default for a lunar site; set it explicitly).
- Times are returned as strings like `2026-Dec-01 00:00`; convert to UTC ISO with `datetime.strptime(..., "%Y-%b-%d %H:%M")`.
- Parses by "all floats in the row", which is fragile; S3 parses by header name.
- No cache, no retry, no User-Agent, no physical sanity checks.
- `SITE_COORD` altitude is fixed at 0 km. **VERIFY** in the docs what reference the altitude is relative to for `coord@301`, and write the finding in `docs/scientific_method.md`.

**Done when:** one real Sun and one real Earth CSV exist and look plausible (azimuth sweeps, elevation near 0 for a polar site). Status row `S1`.

---

## S2. Site catalog (P0)

Create `backend/data/sites.json`:

```json
[ { "id": "shackleton_rim", "name": "Shackleton Rim", "lat_deg": 0.0, "lon_east_deg": 0.0,
    "provenance": "URL or citation", "notes": "" } ]
```

Three sites, ids exactly: `malapert_mountain`, `shackleton_rim`, `connecting_ridge`.
Sourcing order for coordinates: (1) the Unity assets `Assets/LunarNavigator/Generated/**/Site_1.asset`, `Site_2.asset`, `Site_3.asset` (text YAML; read lat/lon fields); (2) cross-check against an authoritative source (NASA PGDA product pages `https://pgda.gsfc.nasa.gov/products/78` and `/90`, LROC QuickMap, Moon Trek) using web access if available. Write the comparison to `docs/site_coordinate_check.md`.
- If the Unity values and the authoritative values disagree by more than 0.05 degrees, **do not pick silently**: use the authoritative value in `sites.json`, and list the mismatch at the top of `docs/science_status.md` so the user can tell the Unity side.
- Never place a site exactly at latitude -90 (azimuth is undefined). If a site is at or beyond -89.95, record that in provenance and use the stated latitude as is.
- All longitudes east positive, -180..180.
- If you cannot verify a coordinate from any source, mark `"verified": false` in the entry and say so in the status file. Do not guess.

**Done when:** `sites.json` loads; check script prints 3 sites; status row `S2`.

---

## S3. Production Horizons client (P0)

File `backend/science/horizons.py`, CLI `python -m science.fetch_horizons --site <id|all> [--refresh]`.

Requirements:
- Endpoint `https://ssd.jpl.nasa.gov/api/horizons.api`, `format=json`. Base parameters from the probe, plus `TIME_TYPE='UT'`, `STEP_SIZE='10 m'`, `EXTRA_PREC` not needed. Quote values with single quotes as the probe does.
- Window: **2026-12-01 00:00 to 2027-01-01 00:00 UTC**, 10-minute step. (If the response is rejected as too long, fetch in 7-day chunks and concatenate; drop duplicate boundary rows.) The first sample must be exactly `2026-12-01T00:00:00Z` and consecutive samples must differ by exactly 600 s; assert it.
- Targets: Sun (`COMMAND='10'`) and Earth (`COMMAND='399'`).
- Cache raw JSON to `backend/data/cache/horizons_<site>_<sun|earth>.json`. If the cache exists and `--refresh` is not given, do not call the network.
- Parse the table between `$$SOE` and `$$EOE`. **Find the azimuth and elevation columns by header name** from the header line above `$$SOE` (look for names beginning `Azi` and `Elev`); never by position. Handle blank flag columns. Fail with a clear error if the headers are not found.
- Return a DataFrame `t_utc (datetime64), az_deg, el_deg`.
- `requests` with `User-Agent: LunarNavigator-SpaceApps/1.0`, timeout 90 s, 3 retries with 2, 4, 8 s backoff. On final failure raise an error that includes the first 1,000 characters of the server response.

**Physical sanity tests** (`backend/tests/test_horizons_sanity.py`, run against the cached real data; skip with a clear message if no cache). For a site at latitude `phi`, let `k = 90 - abs(phi)`:
- Sun elevation stays within `+/-(k + 1.7)` degrees.
- Earth elevation stays within `+/-(k + 7.2)` degrees.
- Sun azimuth unwrapped covers about 360 degrees over 29.5 days: with a 31-day window assert total unwrapped change is between 340 and 400 degrees (near-pole sites only, `k < 1`).
- No NaN, azimuth in 0..360, elevation in -90..90.
If a test fails, the frame, units, or parse is wrong. Fix before continuing. Do not loosen tolerances to pass.

Plot `docs/img/tracks_<site>.png`: Sun and Earth elevation vs time, plus horizontal line at 0. Look at it and describe it in one sentence in the validation report (`S3-tracks`).

**Early delivery (do this immediately after S3 passes):** run the export (S7 code path) with a flat skyline `H(az) = -0.05` degrees, `data_status = GEOMETRIC_ONLY`, `terrain.source = "none (flat horizon)"`, so Unity can use real Sun/Earth data now. Append to `docs/science_status.md`: `GEOMETRIC_ONLY files delivered`.

**Fallback if Horizons is unreachable:** try the other documented host or retry later; message the user. Do not substitute invented data. (An offline route through SPICE kernels is possible but NOT planned; do not start it without user approval.)

---

## S4. DEM acquisition (P0)

Goal: a single regional DEM that covers all 3 sites plus about 100 km around each (rays toward the pole side will leave the DEM; that is recorded as `incomplete_sectors`).

1. Open the NASA PGDA product pages `https://pgda.gsfc.nasa.gov/products/90` (regional south-polar LOLA DEMs) and `https://pgda.gsfc.nasa.gov/products/78` (high-resolution site DEMs). Read the actual file list. **VERIFY** the real file names, resolutions, sizes, formats (GeoTIFF vs Cloud Optimized GeoTIFF), CRS and elevation reference. Do not rely on any name from memory.
2. Choose the **smallest** regional product (coarsest resolution, about 100 m class or coarser is fine to start) that covers all three sites. Prefer a Cloud Optimized GeoTIFF so you can read a window with `rasterio` over HTTP (`rasterio.open("/vsicurl/<url>")`) without downloading the whole file. If a full download is required and exceeds about 1.5 GB, pick a coarser product and note it.
3. Save (or cache a windowed crop of) the DEM at `backend/data/dem/` (git-ignored).
4. Log to `docs/validation_report.md` (`S4-dem`): file name, URL, CRS WKT, bounds, pixel size, dtype, units, NoData value, share of NoData pixels in the crop, and the elevation reference (the LOLA DEM heights are expected to be relative to a 1,737,400 m sphere: **VERIFY** from the page/metadata; the skyline formula depends on it).
5. Write exact name + URL + credit line to `docs/sources_and_credits.md`.
6. Confirm each site coordinate falls inside the raster bounds after projection (see S5). Report any site that does not.

**Done when:** the DEM opens, metadata logged, all 3 sites inside bounds.
**Fallback if no usable DEM within 70 minutes:** keep `GEOMETRIC_ONLY` files (already delivered at S3), tell the user, skip S5 and S9's terrain claims, and continue with S6/S7/S9 using the flat skyline. The status badge stays honest automatically.

---

## S5. Skyline computation (P0)

Files `backend/science/terrain.py`, `backend/science/horizon_profile.py`.

### Projection
Build a transformer from lon/lat (sphere radius `R = 1,737,400 m`) to the DEM CRS using `pyproj`. Construct the DEM CRS from the file's own CRS definition. If `pyproj` cannot parse the lunar CRS, define it explicitly from the metadata (for a polar stereographic DEM, something of the form `+proj=stere +lat_0=-90 +lon_0=<from file> +k=<from file> +R=1737400`; take every parameter from the file's metadata, **VERIFY**, do not assume).
Validate the transform with a round-trip test (lon/lat to x,y to lon/lat within 1e-6 deg) and with the **real-data check** below.

### Algorithm (pixel binning; do not ray-march)
For one site with latitude `lat0`, east longitude `lon0`, DEM ground height `z0` (bilinear sample at the site) and observer height `h` (default 2 m):
1. Crop the DEM to a square of half-width `max_range_km` (default 150, smaller if needed for memory) around the site, intersected with the raster extent. Process in row chunks of about 2 million pixels.
2. For each valid pixel (not NoData) get `lat, lon` by inverse-projecting pixel centers; height `z` from the raster. Position `P = (R + z) * (cos(lat)cos(lon), cos(lat)sin(lon), sin(lat))`.
3. Observer `O = (R + z0 + h) * up0`, with
   `up0 = (cos(lat0)cos(lon0), cos(lat0)sin(lon0), sin(lat0))`,
   `east0 = (-sin(lon0), cos(lon0), 0)`,
   `north0 = (-sin(lat0)cos(lon0), -sin(lat0)sin(lon0), cos(lat0))`.
4. `d = P - O`; `e = d.east0`, `n = d.north0`, `u = d.up0`; `az = atan2(e, n)` in degrees mod 360; `el = atan2(u, hypot(e, n))` in degrees. Exclude pixels closer than `2 * pixel_size` to the observer.
5. Bin `az` into 360 one-degree bins (bin `k` covers `[k - 0.5, k + 0.5)`). **`H[k] = max(el)` over pixels in the bin.**
6. Bins with no pixels: fill by nearest neighbor if both neighbors exist within 3 bins; otherwise add the bin to `incomplete_sectors` (merge consecutive bins into `{az_from_deg, az_to_deg}`) and fill the value with the lowest valid H nearby so downstream math runs. A bin counts as incomplete also if the nearest data in that direction ends before `0.5 * max_range_km` because the ray leaves the DEM extent.
7. Round to 3 decimals. Return `H[360]` and `incomplete_sectors`.

Make `max_range_km`, `observer_height_m` parameters. Cache results to `backend/data/cache/skyline_<site>_h<height>.json`.

### Tests (`backend/tests/test_horizon.py`; all must PASS)
Use a synthetic raster object so tests do not need real data.
1. **Flat DEM** (all zeros): `H` is uniform and equals `-degrees(sqrt(2*h/R))` within 0.02 degrees (about -0.05 degrees for h=2 m).
2. **Wall:** zero terrain plus a 1,000 m high strip 10 km due north (width 5 km). Expected `H[0]` is `atan((1000 - d^2/(2R) - h)/d)` in degrees with `d = 10,000 m`, about 5.5 degrees; assert within 0.15 degrees. `H[180]` stays at the flat value within 0.05.
3. **Raised observer:** observer on a +500 m block sees a lower skyline toward a distant 1,000 m wall than from ground level.
4. **Gap handling:** raster with NoData wedge produces `incomplete_sectors` covering that wedge; no silent zeros.
5. **Rotation invariance:** the same synthetic wall placed east of the observer gives the peak at `H[90]`.

### Real-data checks (record in the validation report as `S5-real-*`)
- **Shackleton:** sample the DEM at the Shackleton site and at a point on its rim a few kilometers away (use the site coordinates); the interior must be **much lower** (kilometers) than the rim. A projection or CRS error fails this immediately.
- **Skyline plot** `docs/img/skyline_<site>.png` (x azimuth, y elevation deg, mark incomplete sectors). Inspect it. For a crater-rim site the skyline should be near 0 over some azimuths and clearly positive over others. Write one honest sentence describing it.
- **Sensitivity:** recompute `H` for observer heights 0.5, 2, 10 m and `max_range_km` 75 vs 150; save the maximum difference per site.

**Done when:** tests pass, real-data checks recorded, skyline plots exist for 3 sites. Only then may you emit `TERRAIN_AWARE_ESTIMATE`.

---

## S6. Visibility and metrics (P0)

File `backend/science/visibility.py`.

- `skyline_at(H, az)`: linear interpolation of `H` with periodic wrap at 360.
- `sun_visible[i] = sun_el[i] > skyline_at(H, sun_az[i]) + sun_margin_deg` (default 0.0); `earth_visible[i] = earth_el[i] > skyline_at(H, earth_az[i])`.
- `run_lengths(mask, value)`; `metrics(sun_mask, earth_mask, a, m, step_s)` exactly per the contract definitions (section 4.2 of the contract). The window must satisfy `a + m <= n` or raise `ValueError`.
- `windows(mask)`: list of `(start_idx, end_idx_exclusive)` for runs of ones.
- `next_loss_index(mask, a)`.

Tests (`backend/tests/test_visibility.py`): all-visible gives fractions 1 and dark 0; all-hidden gives fractions 0 and dark equal to the window length; a single hidden gap in the middle gives the right `dark_max_h`; a gap touching the window start and another touching the end; window longer than the data raises; `step_s = 600` conversions (6 samples = 1 hour) exact; a hand-built 10-sample example computed by hand in the test.

**Sampling sensitivity (record as `S6-sens`):** recompute `f_sun` and `dark_max_h` for one 72 h window at step 10 min vs 30 min (subsample) and report the difference.

---

## S7. Export and contract validator (P0)

1. `backend/science/validate_contract.py <file.json>`: verifies every rule in the contract (array lengths, 360 horizon entries, 0/1 flags, no NaN, recomputes `sun_visible`/`earth_visible` from tracks and skyline and compares, recomputes every `reference_metrics` entry and compares with tolerance 1e-6, enum value of `data_status`). Exit code 1 on failure with readable messages.
2. `python -m science.export_analysis --all [--geometric-only]` writes:
   - `Assets/StreamingAssets/analysis/<site_id>.json` per site (contract 4.2). Include reference metrics for 24, 72 and 168 h windows starting at `2026-12-01T00:00:00Z`, `2026-12-08T00:00:00Z`, `2026-12-15T00:00:00Z` (9 entries per site).
   - `Assets/StreamingAssets/analysis/index.json` (contract 4.1).
   - `observer_height_m` 2.0; `algorithm_version` from `backend/science/__init__.py` (`__version__ = "0.1.0"`).
3. Run `validate_contract.py` on each output. All must pass before you announce delivery.
4. Deliver in two stages: **GEOMETRIC_ONLY** (after S3) and **TERRAIN_AWARE_ESTIMATE** (after S5 passes). The second overwrites the first under the same file names, and `index.json` is updated.

---

## S8. Validation summary (P0)

Update `docs/validation_report.md` with a final table. Rows (each PASS/FAIL/NOT RUN with evidence):
`S1-columns`, `S3-bounds-sun`, `S3-bounds-earth`, `S3-sun-azimuth-sweep`, `S3-tracks`, `S4-dem`, `S5-flat`, `S5-wall`, `S5-raised`, `S5-gap`, `S5-rotation`, `S5-real-shackleton`, `S5-real-plots`, `S5-sensitivity`, `S6-unit`, `S6-sens`, `S7-contract-<site>` (x3), `S9-finder`.
Add a "Known limitations" paragraph (plain language): line-of-sight geometry only; not a power, thermal, antenna, link-budget, or landing-safety analysis; DEM resolution and interpolation limits; Sun treated as a point at disc center (margin parameter available); observer height assumption; rays leaving the DEM flagged; 10-minute sampling.

---

## S9. Opportunity Finder (P1)

File `backend/science/opportunities.py`, CLI `python -m science.opportunities`.

- Candidate sites: the 3 sites. Landing times: every 6 hours from `2026-12-01T00:00:00Z` for 14 days (so the last allowed landing is `2026-12-14T18:00:00Z`); mission duration 72 h (`m = 432` samples). Data length (31 days) covers the last window; assert it.
- For every `(site, landing)` compute metrics with the S6 functions. Use cumulative sums for fractions and run-length scans for gaps; 3 sites x 56 landings is small, so simple loops are fine.
- **Choosing constraints (honest procedure):** first compute all metrics and write `backend/data/cache/finder_all_metrics.csv`. Look at the distribution (min, median, max of each metric). Set the default constraints to **round numbers that are meaningful for a polar mission** and that you can justify in one sentence each in `docs/scientific_method.md` (for example, minimum sunlit fraction and maximum continuous darkness). They are illustrative, user-editable, **not NASA standards**. Do not search for thresholds that crown a chosen winner. If zero scenarios qualify with sensible thresholds, output zero results and fill `closest_misses`; the UI handles it.
- Score (only among qualifiers): `0.4*f_sun + 0.3*f_earth + 0.2*f_both + 0.1*(1 - min(1, dark_max_h/duration_h))`.
- `best_overall`: top 3 by score; ties broken by earlier landing, then site id. `earliest_qualifying`: find the earliest landing time at which any site qualifies; list qualifying sites at that time sorted by score; up to 3.
- `closest_misses`: up to 5 rejected scenarios with the fewest failed constraints, then smallest normalized violation; include `failed` names (`min_sun_fraction`, `min_earth_fraction`, `max_dark_h`, `max_comm_gap_h`).
- Output `Assets/StreamingAssets/analysis/opportunities.json` per contract 4.3; update `index.json`'s `opportunities_file`.
- Tests (`backend/tests/test_opportunities.py`): zero qualifiers; fewer than 3; ties deterministic; last allowed landing evaluates the full 72 h; policies differ when they should; rejected reasons correct for hand-made metrics.

If the status of the DEM step is fallback, the file's `data_status` is `GEOMETRIC_ONLY` and the UI will say so.

---

## S10. Science documentation for the submission (P0)

- `docs/scientific_method.md`: data sources and exact products, coordinate conventions, the Horizons request parameters, the skyline algorithm (pixel binning, formulas), visibility and metric definitions, finder constraints and scoring (illustrative), limitations, and the confidence tier reached.
- `docs/sources_and_credits.md`: JPL Horizons, NASA PGDA / LOLA DEM (exact product), NASA SVS CGI Moon Kit / LROC (used by the Unity side), plus a line disclosing AI-assisted development (Codex, Claude). Do not use NASA logos.
- Use only claims backed by a PASS row in the validation report. Use the wording "environmentally promising candidate" and "line-of-sight geometry", never "safe landing zone" or "guaranteed power/communications".

---

## S11. Handoff and integration support (P0)

When a delivery stage completes, append to `docs/science_status.md`: file list, `data_status` of each file, step reference, and the validation rows passed. Then stop and wait for the user to relay messages from the Unity thread. If the user relays a problem (for example "Sun elevation in Unity differs from the file"), reproduce it from your own files first (recompute the value at that index), report the finding, and fix your side only if the fault is yours. Contract changes go through the "Change requests" section of the contract, never silently.

If you finish early: (1) re-run all tests from a clean shell; (2) run `validate_contract.py` on all outputs; (3) write a 10-line `backend/README.md` explaining how to regenerate everything (`fetch_horizons`, `export_analysis`, `opportunities`); (4) tell the user you are done.

---

## Definition of done (Track A)

- [ ] 3 site files + `index.json` (+ `opportunities.json` if P1 done) validated by `validate_contract.py`.
- [ ] `data_status` honest for every file.
- [ ] All tests pass from a clean shell; `docs/validation_report.md` has no unexplained NOT RUN.
- [ ] `docs/scientific_method.md`, `docs/sources_and_credits.md`, `backend/README.md` exist.
- [ ] Everything committed with path-scoped commits.
