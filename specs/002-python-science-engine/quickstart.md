# Future implementation validation guide

These are **planned commands**, not runnable functionality delivered by Plan. No environment is installed, dependency lock generated, Python module implemented, science acquired or runtime check executed now. Run only after the reviewed tasks/implementation stage is authorized.

## Prerequisites and safe targeting

1. Constitution v1.1.0 is already applied; verify its version and ownership rules.
2. Before implementation, have the owner apply/record the approved [contract wording](data-contract-amendments.md), including full precision, endpoint/cell and complete-subset semantics. Record applied hash/profile and Unity-owner acknowledgement. Do not edit C# or contracts from this guide without that stage's authorization.
3. Install/locate 64-bit CPython 3.12 and the Windows Python launcher (3.11 is the validated fallback). If `py` is absent or selected interpreter unavailable, resolve that prerequisite; never silently run the machine's newer default interpreter. A launcher command below is valid only once this prerequisite is met.

Future Spec Kit commands must explicitly target the science feature and avoid persisting the Unity pointer. For read-only verification:

```powershell
Set-Location 'C:\Users\user\Documents\Unity\LunarNavigator'
$env:SPECIFY_FEATURE_DIRECTORY = 'specs/002-python-science-engine'
& '.\.specify\scripts\powershell\check-prerequisites.ps1' -Json -PathsOnly
```

Expected FEATURE_DIR is the science feature, not 001-lunar-explorer. A later stage whose helper persists feature.json needs the same process-local NoPersist handling documented in workflow-record.md; do not run it blindly against shared selection.

## Environment and offline checks (future S0)

```powershell
Set-Location 'C:\Users\user\Documents\Unity\LunarNavigator\backend'
py --list
py -3.12 -m venv .venv
& '.\.venv\Scripts\python.exe' -m pip install --upgrade pip
& '.\.venv\Scripts\python.exe' -m pip install --only-binary=:all: numpy pandas requests matplotlib pytest
& '.\.venv\Scripts\python.exe' -m pytest -m 'not real_data and not network'
```

If 3.12 is unavailable but the reviewed fallback is supported, replace only the venv creation selector with `-3.11`. Do not treat an empty future test suite as functional validation. After successful import/binary64 smoke checks and core tests, capture the actual tested dependency versions into backend requirements.txt and a run/dependency manifest. Install rasterio/pyproj wheels at terrain entry, not as a prerequisite to delivering geometry; do not compile GDAL on failure.

## First useful P0 result

Curate and independently verify one exact sourced catalog point first. The example below uses shackleton_rim **only if that point is actually verified**; it does not adopt asset coordinates as verified values. If another catalog site is the first verified one, use its ID consistently.

```powershell
& '.\.venv\Scripts\python.exe' -m science.fetch_horizons --site shackleton_rim --observer-model geometric
& '.\.venv\Scripts\python.exe' -m science.inspect_site --site shackleton_rim --landing 2026-12-01T00:00:00Z --duration-h 72 --mode geometric
& '.\.venv\Scripts\python.exe' -m science.export_analysis --site shackleton_rim --geometric-only
```

Expected: retained real Sun/Earth responses, observed headers/settings, exact paired 4465-instant baseline, 4464 usable cells, inspected physical/source checks, nominal-sphere observer/dip provenance, known flags and nine reference windows in a newly printed backend/data/staged/run_id directory. No Unity output is overwritten by default. Validate the actual printed index path with the standalone module command; never substitute a guessed successful run path or call it published.

```powershell
# Replace the following argument with the actual index path printed by export.
& '.\.venv\Scripts\python.exe' -m science.validate_contract 'data/staged/ACTUAL_RUN_ID/index.json'
```

This is the one deliberate run-ID placeholder; it is not an invented generated file. Validation must fail if nonexistent.

## Add real terrain for that same site

```powershell
& '.\.venv\Scripts\python.exe' -m pip install --only-binary=:all: rasterio pyproj
& '.\.venv\Scripts\python.exe' -m science.acquire_dem --site shackleton_rim
& '.\.venv\Scripts\python.exe' -m science.build_horizon --site shackleton_rim --observer-height-m 2 --max-range-km 150 --sensitivity
& '.\.venv\Scripts\python.exe' -m science.fetch_horizons --site shackleton_rim --observer-model terrain
& '.\.venv\Scripts\python.exe' -m science.inspect_site --site shackleton_rim --landing 2026-12-01T00:00:00Z --duration-h 72 --mode terrain
& '.\.venv\Scripts\python.exe' -m science.export_analysis --site shackleton_rim --terrain-aware
```

Expected: actual acquired product/crop with verified datum/frame/registration/control evidence; passed analytic/synthetic tests; inspected profile and coverage/sensitivity; tracks matching the real observer. If terrain is unavailable, incomplete or fails checks, the terrain command reports failure/incomplete evidence and preserves the earlier geometric result. A separately requested geometric model remains the useful fallback. No silent terrain-aware promotion.

## Expand and evaluate P1 opportunities

Independently verify and process remaining site/observer/terrain inputs, then:

```powershell
& '.\.venv\Scripts\python.exe' -m science.export_analysis --all --terrain-aware --allow-exclusions
& '.\.venv\Scripts\python.exe' -m science.opportunities --mode terrain --site all --earliest 2026-12-01T00:00:00Z --search-days 14 --landing-step-h 6 --duration-h 72 --allow-exclusions
& '.\.venv\Scripts\python.exe' -m pytest -m 'not network'
```

Expected: 168 scenarios only when all three are eligible; otherwise explicit original/accepted sets and exclusion reasons, no fabricated winners. Temporal insufficient coverage is rejected, not silently dropped. Zero qualifiers among complete candidates is successful with empty lists; zero eligible complete sites produces local empty/exclusion evidence only. Default constraints remain illustrative. Remove --allow-exclusions for a strict full-request check.

## Release and clean-shell reproduction

Validate the actual staged set/index/opportunities, confirm applied contract/consumer acknowledgement and exclusive destinations, arrange the reader reload/startup barrier, then use the planned `export_analysis --publish ACTUAL_RUN_DIR` interface. Without these gates, keep artifacts staged. Unity owner runs parity vectors/double/frame checks after one deliberate reload; this agent edits no C#/.meta.

From a clean shell, repeat scientific calculations from retained validated source/cache/DEM inputs and pinned working dependencies, compare numeric/quality/order outputs, and audit [test-matrix.md](test-matrix.md). Generation time may differ; source/algorithm identities and numerical results must agree. Record every actual check PASS/FAIL/NOT RUN with evidence, including failures and omitted later milestones. Scientific methodology, sources/credits, status and backend regeneration README must match what was actually achieved.
