# LunarNavigator — Release Acceptance Report
**Project**: `C:\Users\user\Documents\Unity\LunarNavigator`  
**Date**: November 15, 2026  
**Target Platform**: Windows Standalone (x86_64, DirectX 12 / HDRP)  
**Verification Level**: Live FastAPI NASA Horizons Stress Test & Automated Play Mode Suite  
**Unity Editor**: 6000.6.3f1 (HDRP 17.6.0)  
**Compilation Status**: Clean (0 Errors, 0 Warnings)  
**Test Suite Status**: **5,311 / 5,311 Automated Tests Passing (0 Failures)**

---

## Executive Summary & Build Readiness Decision

- **Release Status**: **READY FOR FINAL WINDOWS STANDALONE REBUILD**
- **Blockers**: **NONE**
- **Scientific Backend**: Active on `http://127.0.0.1:8000` (FastAPI `uvicorn science.api:app`, 156 passing backend tests, validated NASA Horizons ephemerides).
- **Core Integrations**:
  - Live Site Inspector (`POST /analyze`): Fully operational, verified across all reference centers (`shackleton_rim`, `connecting_ridge`, `malapert_mountain`).
  - Live Opportunity Finder (`POST /opportunities`): Fully operational, evaluating true 24-scenario search grids ($3\text{ sites} \times 8\text{ half-open arrival steps}$), ranking qualifying windows, handling zero-qualifying searches with scientific `empty_reason` and closest misses, and synchronizing scenario state.
  - UI Clarity Corrections: Three-lane visibility timeline displays exact UTC bounds, scope declaration (`Selected mission window` vs `Wider analysis coverage`), explicit lane labels (`SUNLIGHT`, `EARTH VISIBLE`, `BOTH VISIBLE`), segment durations ("12.5 h") inside wide segments, and interactive hover tooltips.
  - Lunar Surface Contrast: Photographic regolith albedo multiplier restored ($\rho \approx 0.40$), eliminating overexposed chalky highlights and preserving crater rim and mare contrast under direct sunlight.

---

## Feature-by-Feature Acceptance Matrix

| Feature / Subsystem | Status | Verification Method | Evidence & Artifacts | Remaining P0 Defects |
|---|---|---|---|---|
| **Mission Visibility Timeline** | **PASS** | Live Play Mode UI & automated UI hierarchy inspection | [unity_release_acceptance.txt](file:///C:/Users/user/Documents/Unity/LunarNavigator/docs/unity_release_acceptance.txt)<br>[release_global_moon_timeline_1920x1080.png](file:///C:/Users/user/Documents/Unity/LunarNavigator/Screenshots/QA/release_global_moon_timeline_1920x1080.png) | None |
| **Global Moon Lighting & Material Contrast** | **PASS** | Material YAML inspection & Game View capture | [Moon Regolith.mat](file:///C:/Users/user/Documents/Unity/LunarNavigator/Assets/LunarNavigator/Generated/Moon%20Regolith.mat)<br>[Vacuum Volume.asset](file:///C:/Users/user/Documents/Unity/LunarNavigator/Assets/LunarNavigator/Generated/Vacuum%20Volume.asset)<br>[release_global_moon_timeline_1920x1080.png](file:///C:/Users/user/Documents/Unity/LunarNavigator/Screenshots/QA/release_global_moon_timeline_1920x1080.png) | None |
| **Real Reference Center Catalog (GET /sites)** | **PASS** | Live HTTP probe to `GET /sites` in Unity Editor | [unity_real_fastapi_integration.txt](file:///C:/Users/user/Documents/Unity/LunarNavigator/docs/unity_real_fastapi_integration.txt) (Lines 5–13) | None |
| **Dynamic Site Inspector (POST /analyze)** | **PASS** | Multi-site requests (2026 & 2027) with 10-minute cadence ($n=145, 289$) | [unity_real_fastapi_integration.txt](file:///C:/Users/user/Documents/Unity/LunarNavigator/docs/unity_real_fastapi_integration.txt) (Lines 14–36)<br>[unity_release_acceptance.txt](file:///C:/Users/user/Documents/Unity/LunarNavigator/docs/unity_release_acceptance.txt) (Lines 26–34) | None |
| **Dynamic Opportunity Finder (POST /opportunities)** | **PASS** | Real 48h search grid evaluation against live backend | [unity_live_opportunity_finder.txt](file:///C:/Users/user/Documents/Unity/LunarNavigator/docs/unity_live_opportunity_finder.txt)<br>[release_opportunity_finder_1920x1080.png](file:///C:/Users/user/Documents/Unity/LunarNavigator/Screenshots/QA/release_opportunity_finder_1920x1080.png) | None |
| **Zero-Qualifying Empty State Handling** | **PASS** | Impossible constraint query returning `empty_reason` and closest misses | [unity_live_opportunity_finder.txt](file:///C:/Users/user/Documents/Unity/LunarNavigator/docs/unity_live_opportunity_finder.txt) (Check B, lines 17–24) | None |
| **Recommendation Selection Synchronization** | **PASS** | Selection click triggering site, window, clock, camera, and `/analyze` sync | [unity_live_opportunity_finder.txt](file:///C:/Users/user/Documents/Unity/LunarNavigator/docs/unity_live_opportunity_finder.txt) (Check C, lines 25–26) | None |
| **Cancellable Timeouts & Offline Service Handling** | **PASS** | Evaluated 25s Inspector / 90s Finder timeouts, offline port diagnosis | [LunarApiClient.cs](file:///C:/Users/user/Documents/Unity/LunarNavigator/Assets/LunarNavigator/Scripts/Data/LunarApiClient.cs)<br>[unity_release_acceptance.txt](file:///C:/Users/user/Documents/Unity/LunarNavigator/docs/unity_release_acceptance.txt) (Lines 19–20) | None |
| **Stale Result Prevention** | **PASS** | Request generation token guard (`currentAnalyzeToken`, `currentOpportunitiesToken`) | [LunarApiClient.cs](file:///C:/Users/user/Documents/Unity/LunarNavigator/Assets/LunarNavigator/Scripts/Data/LunarApiClient.cs#L84-L260) | None |
| **Surface Observer View & Celestial Consistency** | **PASS** | Play Mode camera mode transition and celestial orientation check | [release_surface_observer_1920x1080.png](file:///C:/Users/user/Documents/Unity/LunarNavigator/Screenshots/QA/release_surface_observer_1920x1080.png)<br>[runtime.txt](file:///C:/Users/user/Documents/Unity/LunarNavigator/Documentation/Verification/runtime.txt) (Lines 49–60) | None |
| **Multi-Resolution UI Responsiveness** | **PASS** | Full HD (1920×1080) and Laptop (1366×768) Game View verification | [release_global_moon_timeline_1920x1080.png](file:///C:/Users/user/Documents/Unity/LunarNavigator/Screenshots/QA/release_global_moon_timeline_1920x1080.png)<br>[release_responsive_1366x768.png](file:///C:/Users/user/Documents/Unity/LunarNavigator/Screenshots/QA/release_responsive_1366x768.png) | None |
| **Play Mode Simulation Runtime** | **PASS** | 4,878 automated interaction steps, camera transitions, and clock tests | [runtime.txt](file:///C:/Users/user/Documents/Unity/LunarNavigator/Documentation/Verification/runtime.txt) (4,878/4,878 passed) | None |

---

## Detailed Acceptance Findings

### 1. Mission Visibility Timeline Polish (Part A.1)
- **Exact UTC Coverage**: Top header row explicitly renders `COVERAGE: 2026-12-01 00:00:00 UTC → 2026-12-02 00:00:00 UTC (24.0 h)`.
- **Scope Declaration**: Right-aligned scope badge indicates `[SCOPE: Selected mission window]` when the dataset matches the intended mission duration, or `[SCOPE: Wider analysis coverage (mission window: X.X h)]` when a larger analysis window is loaded.
- **Three Labeled Rows**: Left column features distinct uppercase labels `SUNLIGHT`, `EARTH VISIBLE`, and `BOTH VISIBLE`.
- **In-Segment Duration Text**: High-contrast, bold duration text (e.g. `24.0 h`, `12.5 h`) is rendered directly inside all segments with sufficient width ($\ge 4.5\%$ track width).
- **Interactive Tooltips & Hover Readouts**: Every segment features native tooltips detailing exact UTC start, end, and duration. A bottom readout bar displays instantaneous cursor coordinates (`[CURSOR] 2026-12-01 08:20:00 UTC (T+8.3 h)`) or segment details (`[SEGMENT] SUNLIGHT: 2026-12-01 00:00 → 2026-12-01 12:30 UTC (12.5 h)`).
- **Scrubber & Playback**: Dragging or clicking on the track immediately scrubs simulation time without GC allocation.

### 2. Global Lunar Surface Contrast (Part A.2)
- **Root Cause Identified**: `Moon Regolith.mat` had `_BaseColor` set to `{0.82, 0.82, 0.82}`, multiplying already bright photographic albedo maps and causing severe highlight clipping when combined with the directional sunlight (110,000 Lux).
- **Correction Applied**:
  - `Moon Regolith.mat`: Adjusted `_BaseColor` and `_Color` to `{0.40, 0.40, 0.40, 1.0}`, restoring authentic lunar mare and highland albedo contrast.
  - `Rough lunar soil.mat`: Adjusted `_BaseColor` to `{0.44, 0.44, 0.43, 1.0}`.
  - `Vacuum Volume.asset`: Tuned `fixedExposure` from $12.2$ to $13.0\text{ EV}$, adding nearly 1 full stop of highlight dynamic range to prevent solar limb blowouts while keeping craters sharp.
- **Scientific Non-Interference**: Scene lighting adjustments are purely visual/photometric and do not alter the analytical ephemeris vectors or visibility calculations.

### 3. Critical Integration Stress Test (Part B)
- **Scenario Count Parity**: Confirmed that a half-open 48-hour arrival window with a 6-hour step evaluates exactly 8 arrivals per site ($t = 0, 6, 12, 18, 24, 30, 36, 42\text{ h}$). For 3 candidate sites, the backend evaluates exactly **24 scenarios**. The UI and test suite reflect this verified count.
- **Backend Date Constraints**: Enforced date limits in `OpportunityFinderView`: landing dates must fall between 2018 and 2029, and mission evaluation window cannot exceed `2030-01-01T00:00:00Z`.
- **Cancellable Timeouts**:
  - Site Inspector (`/analyze`): 25 seconds.
  - Opportunity Finder (`/opportunities`): 90 seconds (accommodating uncached multi-site Horizons scans).
- **Unavailable Backend Handling**: If the backend is stopped or unreachable, `LunarApiClient` returns an explicit diagnostic message (`"Backend unreachable: Cannot connect to scientific backend at http://127.0.0.1:8000. Ensure FastAPI is running."`), preventing silent failures or fallback to synthetic stub data.
- **Stale Request Filtering**: Implemented monotonic request generation tokens (`currentAnalyzeToken`, `currentOpportunitiesToken`). Superseded or canceled responses are discarded immediately and cannot overwrite newer state.

---

## Automated Verification Suite Summary

| Test Suite | Total Checks | Passed | Failed | Source Verification File |
|---|---|---|---|---|
| `ReleaseAcceptanceChecks` | 33 | 33 | 0 | [unity_release_acceptance.txt](file:///C:/Users/user/Documents/Unity/LunarNavigator/docs/unity_release_acceptance.txt) |
| `LiveOpportunityFinderChecks` | 35 | 35 | 0 | [unity_live_opportunity_finder.txt](file:///C:/Users/user/Documents/Unity/LunarNavigator/docs/unity_live_opportunity_finder.txt) |
| `RealLiveFastApiIntegration` | 35 | 35 | 0 | [unity_real_fastapi_integration.txt](file:///C:/Users/user/Documents/Unity/LunarNavigator/docs/unity_real_fastapi_integration.txt) |
| `OnlineIntegration` | 29 | 29 | 0 | [unity_online_integration.txt](file:///C:/Users/user/Documents/Unity/LunarNavigator/docs/unity_online_integration.txt) |
| `Models and Scene` | 244 | 244 | 0 | [models.txt](file:///C:/Users/user/Documents/Unity/LunarNavigator/Documentation/Verification/models.txt) |
| `Frame and Time` | 4 | 4 | 0 | [frame.txt](file:///C:/Users/user/Documents/Unity/LunarNavigator/Documentation/Verification/frame.txt) |
| `Contract Gate` | 38 | 38 | 0 | [contract_gate.txt](file:///C:/Users/user/Documents/Unity/LunarNavigator/Documentation/Verification/contract_gate.txt) |
| `Data Parity` | 15 | 15 | 0 | [parity.txt](file:///C:/Users/user/Documents/Unity/LunarNavigator/Documentation/Verification/parity.txt) |
| `Runtime Play Mode` | 4,878 | 4,878 | 0 | [runtime.txt](file:///C:/Users/user/Documents/Unity/LunarNavigator/Documentation/Verification/runtime.txt) |
| **Grand Total** | **5,311** | **5,311** | **0** | **All automated checks passing** |

---

## Visual Verification Artifacts

1. **Global Moon & Improved Timeline (1920×1080)**:  
   [release_global_moon_timeline_1920x1080.png](file:///C:/Users/user/Documents/Unity/LunarNavigator/Screenshots/QA/release_global_moon_timeline_1920x1080.png)  
   *Demonstrates restored crater contrast, 3 labeled visibility lanes, exact UTC bounds, and in-segment duration text.*

2. **Responsive Laptop Layout (1366×768)**:  
   [release_responsive_1366x768.png](file:///C:/Users/user/Documents/Unity/LunarNavigator/Screenshots/QA/release_responsive_1366x768.png)  
   *Demonstrates clean layout scaling without element clipping or overlap.*

3. **Live Opportunity Finder (1920×1080)**:  
   [release_opportunity_finder_1920x1080.png](file:///C:/Users/user/Documents/Unity/LunarNavigator/Screenshots/QA/release_opportunity_finder_1920x1080.png)  
   *Demonstrates live 24-scenario search parameters, loading state, recommendation cards, and scientific limitations caption.*

4. **Surface Observer Celestial Orientation (1920×1080)**:  
   [release_surface_observer_1920x1080.png](file:///C:/Users/user/Documents/Unity/LunarNavigator/Screenshots/QA/release_surface_observer_1920x1080.png)  
   *Demonstrates surface perspective aligned with calculated Horizons azimuth and elevation.*

---

## Readiness Recommendation

The Unity project is in a clean, stable state with 0 compilation errors and 5,311 passing automated assertions. There are **no blockers** to proceeding with the final Windows Standalone build.
