# Scientific CLI contract (planned)

All commands below are future interfaces, run from `backend/` with the verified local environment active. Paths resolve with pathlib; command output gives absolute artifact paths and a run ID. No command is implemented/executed by this document. Defaults: baseline UTC December 1, 2026 through January 1, 2027, step 600 s, h=2 m, skyline range 150 km, 1-degree bins.

| Command | Required inputs / options | Outputs and failure contract |
| --- | --- | --- |
| `python -m science.fetch_horizons --site ID` | Verified catalog ID; optional --start/--stop UTC, --step-minutes 10, --observer-model geometric or terrain, --refresh. Terrain observer requires matching validated support. | Raw envelope/result/header/request manifests, parsed paired tracks, validation/plot evidence. Offline cache hit is reported. No production result for unverified site or bad source/cadence. |
| `python -m science.acquire_dem --site ID` | Verified site; --product-page defaults to official regional page; --product selects an actually listed product; optional --max-range-km and --local-file for retained acquired data. | Exact chosen URL/identity, metadata, bounded crop or small full raster, validity/ground/frame/control checks. Does not call a product validated before evidence exists. |
| `python -m science.build_horizon --site ID` | Validated product/support; --observer-height-m 2, --max-range-km 150; --refresh; optional --sensitivity. | Float64 skyline/knownness cache, sector/reason report and plot. An incomplete profile is saved locally with INCOMPLETE quality and failing certification, never a passed terrain result. |
| `python -m science.inspect_site --site ID --landing UTC --duration-h HOURS --mode geometric` | Mode explicitly geometric or terrain; optional solar margin and thresholds; --comparison-site/--comparison-landing for one secondary scenario. | Local inspection report, tracks/windows/five metrics/next losses/source/model. Unknown requests return labeled local bounds/reasons and incomplete status; no binary interchange. No off-grid clamping. |
| `python -m science.export_analysis --site ID --geometric-only` | Or --terrain-aware. Use --all for requested three IDs. Optional --allow-exclusions, --out path within backend/data/staged. No publish by default. | Full-precision complete site files and index in a run staging directory, nine reference windows/site; empty opportunity reference until valid file exists. Explicit accepted subset/exclusions report. |
| `python -m science.opportunities --mode terrain --site all` | --earliest UTC, --search-days 14, --landing-step-h 6, --duration-h 72, --min-sun-fraction .30, --min-earth-fraction .40, --max-dark-h 36, --max-comm-gap-h 48; optional --allow-exclusions and --out staged path. Mode geometric is distinct. | All metrics CSV/run report and complete-cohort v1 opportunities with both policies/misses/counts. No qualifiers is a successful empty result, not an error. No eligible complete cohort yields local empty/exclusion report and no misleading wire file. |
| `python -m science.validate_contract FILE` | Site, index or opportunities JSON; for index use --set-dir; for opportunity semantic validation require referenced accepted site datasets. | Independent structural/semantic validation, exact diagnostic paths and exit status. No side effects on source/exchange files. Same parser is exposed as `python backend/science/validate_contract.py FILE` from project root; keep this validator a standalone stdlib reference path rather than depending on exporter state. |
| `python -m science.export_analysis --publish RUN_DIR` | A previously validated staged run, approved applied profile/hash, matching consumer acknowledgement, exclusive owned destinations and reload/startup barrier. | Validate again if bytes changed, backup/rollback under barrier, per-file atomic commit/index last, hash receipt and handoff. Never synthesize data or overwrite Unity stubs/.meta. No gate bypass flags. |

## Input and error conventions

- UTC must include explicit Z; dates, step/duration/search alignment and thresholds validated before expensive work. Local timestamps, unsupported leap-crossing intervals, NaN/Infinity, bad IDs and path traversal reject. No arbitrary user URL is treated as a trusted product until its provenance/metadata are inspected.
- `--all` requests all three catalog IDs. Without `--allow-exclusions`, any unavailable requested site/scenario fails the complete request. With it, exclusions/reasons/original versus accepted cohort must be printed and persisted. Temporal out-of-coverage never silently removes the final arrival; the request fails.
- Export mode never chooses fallback silently. Terrain failure reports unavailable/incomplete and preserves previous valid results; explicitly rerun a geometric request if desired.
- Cache artifacts are immutable evidence by identity; refresh selects a new run. No silent source/header/tolerance changes to pass.
- Writes are confined to backend/science-owned docs and explicitly owned scientific outputs after gates. Validate resolved destinations stay within approved directories before replacement; never recursively delete/move a computed directory.

| Exit code | Meaning |
| --- | --- |
| 0 | Requested complete action succeeded, including a valid evaluated search with zero qualifiers. |
| 2 | Invalid request/CLI/date/grid/threshold/path. |
| 3 | Scientific source, coordinate/frame, terrain or essential completeness failure; incomplete evidence retained locally. |
| 4 | Acquisition/service/cache identity failure. |
| 5 | Contract/numerical/semantic validation failure. |
| 6 | Contract profile/consumer/ownership/reload-barrier publication gate not met; staged output retained. |

Code 0 with `--allow-exclusions` must prominently report exclusions; it means the accepted subset succeeded, not that every original candidate was verified. Logs/evidence use PASS/FAIL/NOT RUN per executed check and never present process exit alone as scientific proof.
