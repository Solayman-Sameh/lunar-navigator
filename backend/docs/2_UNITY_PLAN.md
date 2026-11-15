# LunarNavigator: Track B, Unity App and UI

**Audience:** Codex thread 2 (Unity). A separate Python thread is still building the science data **in parallel and may not be finished for hours**. You must be **fully independent of it**: build and test everything against synthetic stub data that follows the shared contract, then switch to real data automatically when it appears.
**Project root:** `C:\Users\user\Documents\Unity\LunarNavigator` (Unity 6.6 `6000.6.3f1`, HDRP 17.6, Windows, Input System, UI Toolkit)
**Deadline:** Sunday 15 Nov 2026, 11:59 PM local. **Feature freeze 9:00 PM. First test build by 8:00 PM. Submission work starts 9:45 PM.**
**Read first:** `docs/DATA_CONTRACT.md`. Obey its path ownership and git rules.

---

## 0. Mission and rules

Turn the existing Moon viewer into a tool that answers: *for this site, landing time and mission duration, when are the Sun and Earth visible above the real local skyline?* The app reads precomputed JSON files from `Assets/StreamingAssets/analysis/`.

1. **Independence.** Never wait for, call, or edit the Python side. You own the stubs (`stub_*.json`). Real files (non-`stub_`) belong to the Python agent: **never edit them**. Read `docs/science_status.md` (written by the Python agent) at the checkpoints in section 12.
2. **No faked science.** Everything computed from stub data is `ILLUSTRATIVE DEMO` and the badge must say so. Show the file's `data_status` verbatim; never upgrade it.
3. **Minimal edits to existing code.** The project already has: `LunarApplication`, `CameraDirector` (4 modes), `DemoSimulation` (30-day clock, illustrative Sun/Earth), `LunarCoordinates`, `LunarSite`, `LunarTerrain`, `LunarWorld`, `NavigatorConfig`, `NavigatorInput`, `NavigatorUI`, `InteractionProbe`, and editor tools (`LunarSceneBuilder`, `LunarPolish`, `LunarSurfaceDetail`, `LunarVerification`). Extend, do not rewrite. Do not change the render pipeline, packages, folder names, or the NASA textures. **Add no Unity packages.** (Use the existing `LunarVerification` editor menu for checks, not the Test Framework, unless the package is already in `Packages/manifest.json`.)
3. **Orient before editing.** First read the existing scripts listed above and write `docs/unity_architecture_notes.md` (one page: how UI is built, how camera modes switch, how time flows, how sites and markers are placed, which scene objects exist). Use it to avoid guessing.
4. **Compile and look.** After every step: recompile with zero Console errors, enter Play mode, and check the result. Use the Unity CLI (`unity --help`; look for console-log and screenshot commands) to capture Game-view screenshots at **1920x1080 and 1366x768** and inspect them.
5. **Evidence log.** `docs/validation_report.md` belongs to the Python agent; keep your own log in `docs/unity_status.md`: `time | step | PASS/FAIL | note | screenshot path`. Never mark PASS without running it.
6. **Commit after every step** with explicit paths: `git add Assets ProjectSettings docs/unity_*.md && git commit -m "U3: skyline ring"`. Never `git add -A`.
7. **Timebox.** If a step runs over 1.5x its budget, apply its cut or fallback and move on.
8. **Truthful UI text.** No claim of power, communications, or safety. The Help popover carries: *"Line-of-sight geometry only. Not a power, link-budget or landing-safety analysis."* The Sun and Earth discs use visual scale; the Help popover says so.

---

## 1. Step list

| Step | Goal | Budget | Priority |
|---|---|---|---|
| U0 | Orientation, cleanup, baseline checks | 25 min | P0 |
| U1 | Data classes, repository, stub generator | 40 min | P0 |
| U2 | VisibilityMath (C# metrics) | 30 min | P0 |
| U3 | Drive the clock, Sun and Earth from data + frame checks | 50 min | P0 |
| U4 | Skyline ring in Surface Look | 40 min | P0 |
| U5 | UI foundation: tokens, layout, status badge, mode switch | 30 min | P0 |
| U6 | Inspect panel + metrics cards | 40 min | P0 |
| U7 | Visibility timeline | 45 min | P0 |
| U8 | Skyline chart | 40 min | P0 |
| U9 | Find Opportunities panel | 40 min | P1 |
| U10 | Surface overlay, Compare, polish, Help text | 45 min | P1 |
| U11 | QA matrix and interaction probe | 30 min | P0 |
| U12 | Build and exe test | 45 min | P0 |
| U13 | Real-data integration (whenever real files appear) | 30 min | P0 |

Do U0 to U8 in order. U13 can happen at any time real files land; the code path is built in U1 so it is a no-op change.

---

## U0. Orientation and baseline (P0)

1. Read the scripts; write `docs/unity_architecture_notes.md`.
2. Fix the compiler warning `CS0618` at `Assets/LunarNavigator/Editor/LunarSceneBuilder.cs(120,67)`: `HDAdditionalLightData.SetIntensity(float, LightUnit)` is obsolete; use the replacement named in the warning text.
3. Enter Play mode. In the first screenshot (editor, not Play mode) the Game view was cropped and no HUD was visible. Determine whether the HUD renders. Screenshot at 1920x1080 and 1366x768. Fix anything off-screen or clipped. Record.
4. Open the HDRP Wizard (Window > Rendering > HDRP Wizard). Press "Fix All" only if something is red. The "materials will be skipped in migration" notice is acceptable.
5. Dump the three candidate sites from `Site_1..3.asset` into `docs/unity_site_coords.md` (name, id, lat, lon, elevation). The Python agent reads this to cross-check.
6. Confirm the existing keys work (`1 2 3 4 F R Space H Tab Esc`) and note any that do not.
7. Commit baseline.

**Done when:** zero errors, warning fixed, screenshots show a visible HUD, notes written.

---

## U1. Data layer and stub generator (P0)

### Files (`Assets/LunarNavigator/Scripts/Data/`)
- `AnalysisData.cs`: `[Serializable]` plain classes mirroring the contract exactly: `AnalysisIndex`, `IndexSite`, `SiteAnalysis`, `SiteInfo`, `Ephemeris`, `TerrainInfo`, `SectorRange`, `VisibilityInfo`, `ReferenceMetric`, `OpportunitiesFile`, `OpportunityRequest`, `Constraints`, `OpportunityResult`, `ClosestMiss`. Use `public` fields; arrays are `float[]`, `int[]`, `string[]`. `JsonUtility` cannot read dictionaries or top-level arrays; the contract avoids them. Field names must match the JSON keys exactly (snake_case; use the same snake_case in C#).
- `AnalysisRepository.cs`: loads `Application.streamingAssetsPath/analysis/index.json` with `File.ReadAllText`, then each listed site file. Rules:
  - If `index.json` is missing or invalid: fall back to scanning the folder for `stub_*.json`; if none, show a visible full-screen error panel ("No analysis data found") rather than crashing.
  - **Prefer real over stub:** if any non-stub site file exists and parses, use real files only. Stubs only when no real file exists.
  - Validate: array lengths equal `n`, 360 horizon entries, finite values. On failure, skip that file and show the reason in a "Data problems" line in the Help popover. Never throw.
  - API: `Sites`, `GetSite(id)`, `Opportunities` (nullable), `IsDemoMode`, `TimeAt(site, i)`, `IndexAt(site, DateTime utc)` (clamped), `T0`, `StepSeconds`, `Count`.
  - Parse `t0_utc` as UTC with `DateTimeStyles.AdjustToUniversal | AssumeUniversal`. Display all times with a trailing `UTC`.
- Reload on demand via menu `Lunar Navigator > Data > Reload` (editor) and at app start.

### Stub generator (editor menu `Lunar Navigator > Data > Generate Stub Data`)
File `Assets/LunarNavigator/Editor/LunarStubData.cs`. Writes `stub_<site_id>.json` for the three site ids plus `index.json` **only if no real `index.json` exists** (if one exists, write `stub_index.json` instead and do not overwrite). All stub files use `data_status: "ILLUSTRATIVE_DEMO"`, `t0_utc 2026-12-01T00:00:00Z`, `step_s 600`, `n 4465`, and:
- Sun azimuth sweeps 360 degrees in 29.53 days (different phase per site), Sun elevation `1.4 * sin(2 pi t / 29.53 d + phase)`.
- Earth azimuth sweeps 360 degrees in 27.3 days; elevation `6.0 * sin(2 pi t / 27.3 d + phase)`.
- Skyline: `0.8 + 1.2*sin(3a) + 0.7*sin(7a + phase)` degrees (a = azimuth in radians), one incomplete sector `{200, 215}` on one site, none on the others.
- Flags and 9 reference metrics per site (24, 72, 168 h at 3 starts) **computed with your own `VisibilityMath`** so the stub is self-consistent. `provenance` says `"SYNTHETIC. Not real data."`
- Same three site ids, names and approximate coordinates as `docs/unity_site_coords.md`.

**Done when:** menu produces stubs; Play mode loads them and logs `Loaded N sites, demo mode = true`; a corrupt file (delete one bracket manually) produces a visible error line, not an exception.

---

## U2. VisibilityMath (P0)

File `Assets/LunarNavigator/Scripts/Data/VisibilityMath.cs`, pure C# (no Unity types except in tests), exactly per the contract definitions:
- `float SkylineAt(float[] h360, float az)` periodic linear interpolation.
- `bool SunVisibleAt(site, i)` uses stored flags (and `SunVisibleComputed(site, i)` recomputes from tracks + skyline + `sun_margin_deg`).
- `Metrics Compute(site, startIdx, sampleCount)`: `f_sun`, `f_earth`, `f_both`, `dark_max_h`, `comm_gap_max_h`; throws `ArgumentOutOfRangeException` if the window does not fit (callers catch and show "window exceeds data range").
- `List<(int start, int endExcl)> Windows(int[] mask)`; `int NextLoss(int[] mask, int i)`.
- `Metrics ComputeForUtc(site, DateTime landingUtc, int durationHours)`.

Add editor menu `Lunar Navigator > Verify > Data Parity` (in `LunarVerification.cs` style) that, for every site file loaded:
1. Recomputes `sun_visible`/`earth_visible` from tracks + skyline and compares with the stored flags (counts mismatches; tolerance: 0 mismatches except at samples where `|el - skyline| < 0.002`).
2. Recomputes every `reference_metrics` entry and compares (tolerance 1e-3).
3. Prints PASS/FAIL per site and writes `docs/unity_parity.txt`.
Run it on stubs now; it runs again on real data in U13 (this is the cross-engine proof that C# and Python agree).

**Done when:** parity PASS on the three stubs; a hand-made 10-sample unit case (hard-coded in the menu check) returns the expected values.

---

## U3. Drive the clock, Sun and Earth from data (P0)

Edit `DemoSimulation.cs` and `LunarWorld.cs` minimally.

1. **Clock:** when data is loaded, the simulated time range becomes `[T0, T0 + (n-1)*step]` (31 days). The existing 30-day scrubber maps to that range; the current UTC is shown as `2026-12-05 14:30 UTC`. Default start = `T0`. Keep play/pause.
2. **Selected site:** the current site (the one chosen by the user, default the first) provides `sun_az/el`, `earth_az/el` at index `i` (linear interpolation between samples for smooth scrubbing).
3. **Direction math.** Get the site's local frame from the **same functions in `LunarCoordinates.cs` that place the site markers** (east, north, up in world space), so handedness and longitude conventions stay consistent. Direction toward a body:
   `dir = cos(el) * (cos(az) * north + sin(az) * east) + sin(el) * up`
   (azimuth is clockwise from north). Use `dir` to rotate the Sun directional light (`light.forward = -dir`) and to position the Earth and Sun discs at their existing visual distances. Keep the visual-scale labels honest.
4. Because the Sun/Earth are far away, using the **selected site's** direction for the whole globe is correct to well under a degree. When no site is selected, use the first site.
5. When data is absent, the old illustrative behavior remains and the badge says `ILLUSTRATIVE DEMO`.

### Frame checks (all must PASS; log them)
- **F1 Earth near the center of the near side:** over the whole range, the Earth direction expressed as lunar latitude/longitude (convert `dir` back using `LunarCoordinates`) stays within about 9 degrees of (lat 0, lon 0). If not, longitude sign or handedness is wrong. Fix in the conversion layer, not in the data. (With stub data this check is meaningless; it becomes meaningful at U13. Implement the check now as menu `Lunar Navigator > Verify > Frame`, run it at U13.)
- **F2 Landmarks:** place debug markers (menu-toggled, off by default) at Tycho (about 43.3 S, 11.2 W) and Copernicus (about 9.6 N, 20.1 W). They must sit on those craters in the NASA texture. Record screenshots.
- **F3 Compass:** at a site in Surface Look mode, facing north with up above, east must be on the **right**. Test by placing a temporary marker at azimuth 90 (east).
- **F4 Self-consistency:** the Sun elevation computed from the scene vectors (`asin(dot(dir, up))`) equals `sun_el_deg` at that sample within 0.1 degree, for 5 random indices.

**Done when:** scrubbing time visibly moves the terminator/Sun; F2, F3, F4 PASS now; F1 scheduled for U13.

---

## U4. Skyline ring in Surface Look (P0)

Create `Assets/LunarNavigator/Scripts/Visual/SkylineRing.cs`.
- Builds a procedural mesh around the observer: 360 columns (1 degree each), top vertex at elevation `horizon_el_deg[k]` and bottom vertex at `-30` degrees, at a radius `Rring` chosen so the ring is **in front of** the Sun and Earth discs and inside the camera far plane. Vertices placed with the same direction formula as U3 (`az = k`). Duplicate the seam so it closes cleanly. Normals face the observer.
- Material: matte dark rock (reuse the existing `Basalt rocks.mat` or `Rough lunar soil.mat`; no emission), lit only by the Sun directional light, backface culling set so it is visible from inside.
- Visible only in Surface Look mode, attached to the selected site's frame, rebuilt when the site changes (cache one mesh per site).
- Sectors listed in `incomplete_sectors` get a visible hatch tint (slightly lighter, semi-transparent top band) so users see "no DEM coverage here". In stubs this tests the feature.
- In Surface Look, hide or fade the procedural boulder patch so it cannot contradict the skyline. Show a small label: `Skyline from site data. Foreground terrain: illustrative.` (If data is stub: `ILLUSTRATIVE DEMO`.)
- Sun and Earth discs sit at their real `(az, el)`. When a body is below the skyline, it is occluded by the ring (the depth test does this naturally; verify by scrubbing).

**Done when:** in Surface Look, scrubbing time shows the Sun/Earth passing behind the skyline; compare 3 flip points to the `sun_visible` array (index, expected flip, visual flip) in `docs/unity_status.md`. Screenshots saved.

---

## U5. UI foundation (P0)

Follow the existing pattern in `NavigatorUI.cs` and `Navigator.uss`. Read how the UI is created (UXML vs code) in U0 and keep that approach. All new styles go in `Navigator.uss`. Use only UI Toolkit; no new packages.

### Design tokens (USS variables on the root, reuse existing ones if present)
- Background glass: `rgba(14, 18, 26, 0.72)`; border `rgba(255,255,255,0.10)` 1 px; radius 14 px; inner padding 14 px; element gap 8 px; panel gap 12 px.
- Text: primary `#EAF0F7`, secondary `#9AA7B8`, disabled `#5C6675`. Min size 12 px; body 13 px; section label 11 px uppercase with letter spacing; metric number 22 px bold.
- Accents: Sun `#F5B642`, Earth `#5AA9F5`, Both `#6FE3B0`, Hidden `#3A4150`, Fail `#FF6B6B`, Pass `#6FE3B0`, Warning `#F5B642`.
- Status badge colors: Illustrative `#F5B642` text on `rgba(245,182,66,0.15)`; Geometric `#5AA9F5` text on `rgba(90,169,245,0.15)`; Terrain-aware `#6FE3B0` text on `rgba(111,227,176,0.15)`.
- Transitions: `transition-duration: 0.16s` on hover/press. Hover raises background alpha by 0.08; pressed lowers by 0.04. Focus outline `#EAF0F7` 1 px.
- Contrast: text on glass must be at least 4.5:1; do not rely on color alone (lanes carry labels, pass/fail carry text and icons as characters).

### Layout (reference 1920x1080; must also work at 1366x768)
Use absolute/anchored positioning with the safe margin 24 px.
- Top-left: app title "Lunar Navigator" + subtitle "South-pole landing opportunities", and under it the **status badge**.
- Top-center: the **mode switch** (segmented, 2 options: "Inspect a Site", "Find Opportunities"), width 320 px.
- Top-right: existing Sites / Layers / Settings / Help buttons (keep).
- Left column, below the title block: the **mode panel** (Inspect or Find), width 340 px (300 px at width < 1500).
- Right column, below the buttons: **metrics cards** (2x2 grid, 2 x 150 px wide) then the **skyline chart card** (width 340 px, height 220 px). At width < 1500 the skyline card becomes collapsible (header click) and starts collapsed.
- Bottom: the **timeline** card, anchored bottom center, margin 24 px, width `min(1100 px, screen - 48 px - side-column widths)`; at low width it spans the full width above the safe margin.
- Panels must never overlap each other at 1366x768; verify with screenshots.
- **Tab** hides all panels except the status badge. **Esc** closes popovers and returns up one camera level (existing behavior; keep).
- Panels must not block orbit drags on the 3D view: set `picking-mode: ignore` on empty containers and pass pointer events only on panel surfaces (existing HUD already does this; match it).

### Status badge and mode switch
- Badge text from `data_status` (contract section 3). A tooltip shows `source`, resolution, algorithm version, and generated time. Always visible, even when UI is hidden with Tab.
- Mode switch state `AppMode { Inspect, Find }` kept in the UI controller, separate from the camera modes. Disable "Find Opportunities" with the tooltip "Not available in this build" if `opportunities` is missing.

**Done when:** layout screenshots at both resolutions show no overlaps, tokens applied, badge reads `ILLUSTRATIVE DEMO` with stubs.

---

## U6. Inspect panel and metrics cards (P0)

### Inspect panel (left)
Controls, top to bottom:
1. **Site** dropdown (`DropdownField`), one entry per loaded site; changing it moves the selected site, refreshes everything, and triggers the existing Cinematic Approach on a second click of "Fly to site" (a small secondary button).
2. **Landing time (UTC)**: a `TextField` with format `yyyy-MM-dd HH:mm`, plus buttons `-1 d`, `-6 h`, `+6 h`, `+1 d`. Validate on Enter/blur: parse; clamp into `[T0, T_end - duration]`; if invalid show a red inline message. No native date picker exists in UI Toolkit, so do not attempt one.
3. **Duration chips:** `24 h`, `72 h`, `168 h` (toggle group; default 72 h). If the window does not fit the data, clamp the landing time and show a one-line note.
4. Optional foldout **Mission requirements** (collapsed): four numeric fields `min sunlit %`, `min Earth-visible %`, `max darkness h`, `max comms gap h`, prefilled from the contract example (30, 40, 36, 48). Label: "Illustrative. Not NASA standards."
5. **Analyze** button (primary). It computes `VisibilityMath.ComputeForUtc(...)` and updates the metric cards, timeline window highlight, skyline tracks, and sets the clock to the landing time.
6. Secondary button **Add comparison** (U10).

### Metrics cards (right, 2x2)
Cards: `Sunlit` (percent), `Earth visible` (percent), `Longest darkness` (hours, 1 decimal), `Longest comms gap` (hours). Each shows an accent bar (Sun/Earth/Fail color), the value, and when requirements are set, a pass/fail line such as `PASS (min 30%)` or `FAIL (max 36 h)`. A fifth line below the grid: `Sun and Earth both visible: xx%`. Add a tiny caption under the cards: `Geometric line of sight only.`

**Done when:** changing site, time, duration and pressing Analyze updates all numbers; values match `reference_metrics` for the matching inputs (check in the parity menu); invalid input never throws.

---

## U7. Visibility timeline (P0)

File `Assets/LunarNavigator/Scripts/UI/TimelineElement.cs`: a custom `VisualElement` that draws with `generateVisualContent` and `MeshGenerationContext.painter2D` (available in Unity 6; no package needed).

- Height 124 px. Three lanes (Sun, Earth, Both), each 22 px tall with 8 px gaps and left labels (`Sun`, `Earth`, `Both`, 12 px). Axis row below with day ticks every 5 days (`Dec 01`, ...), UTC.
- Horizontal scale: the full data range (31 days). Draw each visible run as a filled rounded rect in the lane's accent color; hidden background in `#3A4150`. Compute runs from the flags with `VisibilityMath.Windows` (merge runs shorter than 1 px). At most about 200 rects total.
- Mission window: translucent bracket spanning `[landing, landing + duration]` across all lanes with small end caps and a label of start/end UTC.
- Playhead: a 2 px vertical line with a handle; dragging it sets the simulated time (replaces the old scrubber visual; keep its behavior and keyboard stepping `Left/Right` by 1 h, `Shift` by 6 h).
- Hover tooltip: the window under the pointer (`Sun visible 2026-12-03 04:10 to 2026-12-03 19:40 UTC (15.5 h)`) or `Hidden`.
- Click on the track sets the playhead; double-click on the track sets the landing time (and updates the Inspect field).
- Accessibility: hidden runs carry a diagonal hatch pattern (draw as a few thin lines) in addition to color.
- Marks transitions with small ticks where the playhead is within a mission window; show `Next Sun loss in 6.2 h` or `Sun visible until end of window` text at the right end of the header row.

**Done when:** timeline matches the flags (spot-check 5 transitions against the JSON), dragging is smooth, no allocations per frame beyond the playhead (cache the meshes; repaint only on data or playhead change via `MarkDirtyRepaint`).

---

## U8. Skyline chart (P0)

File `Assets/LunarNavigator/Scripts/UI/SkylineChartElement.cs` (same technique).

- Plot area x: azimuth 0 to 360 with ticks at 0 (N), 90 (E), 180 (S), 270 (W), 360; y: elevation from -2 to +10 degrees (auto-expand upward if the skyline or tracks exceed 10, cap at 20). Gridlines at 0 and every 2 degrees, faint.
- **Skyline:** filled area from y = -2 up to `horizon_el_deg[k]`, fill `rgba(150,160,175,0.45)`, outline 1.5 px `#9AA7B8`.
- **Incomplete sectors:** hatched band across the full height with tooltip `No terrain data in this direction`.
- **Sun track and Earth track** over the mission window, thin lines in their colors; break the line wherever consecutive samples jump by more than 180 degrees in azimuth (wrap). Decimate to at most about 1,200 points.
- **Current Sun and Earth** at the playhead: filled dot if above the skyline, hollow dot if below; a short label with elevation and `Above skyline by x.x deg` / `Below skyline by x.x deg` in a caption row under the chart.
- Legend row: Sun, Earth, Skyline. Title: `Local skyline (azimuth vs elevation)`. A footnote shows `terrain.source` and resolution (or "flat horizon" for geometric data).

**Done when:** values match the data (spot-check 3 indices), wrap lines are correct, hollow/filled state matches `sun_visible`/`earth_visible` at the playhead.

---

## U9. Find Opportunities panel (P1)

Only active if `opportunities.json` loaded; with stubs, generate a **stub opportunities file** (`stub_opportunities.json`, `ILLUSTRATIVE_DEMO`) from your own `VisibilityMath` in the stub generator using the contract's request defaults, so the UI is testable now. (The Python agent's real file replaces it automatically.)

Panel contents (replaces the Inspect panel when the mode switch is on Find):
1. Read-only summary of the request (earliest landing UTC, search days, duration, constraints), with the line `Constraints are illustrative and user-editable. Not NASA standards.`
2. Policy toggle: `Best overall` | `Earliest qualifying` (switches the result list).
3. Up to **3 result cards**: rank, site name, landing time (UTC), score (0..100), and four mini metrics. Clicking a card: select the site, set landing time and duration, run Analyze, and start the Cinematic Approach camera.
4. Empty state: if the list is empty show `No qualifying opportunities found` and the `closest_misses` as smaller cards listing failed constraint names (e.g. `Failed: min_sun_fraction`). Never pad to three.
5. Footer: `Evaluated N scenarios; M rejected. Algorithm v0.1.0.`

**Done when:** both policies switch the list; clicking a card changes site/time/metrics; empty state tested by temporarily loading an empty list.

---

## U10. Surface overlay, Compare, polish (P1)

- **Surface overlay (Surface Look only):** compass ticks N/E/S/W along the bottom edge following camera heading; text readouts `Sun 0.8 deg above skyline` or `Sun hidden by terrain (1.4 deg below skyline)`, same for Earth; the label from U4. Hide with Tab.
- **Compare:** "Add comparison" duplicates the current inputs into a second set (landing time editable). Shows a two-column card pair under the metrics with a delta line (`Sunlit +12 pts`, `Longest darkness -8.5 h`). Timeline shows both mission windows (second in a dashed outline). Supports the "schedule slip" scenario of the NASA challenge. One comparison at most.
- **Help popover:** list only keys that work; add the limitation line and the visual-scale note (section 0 rule 8); add "Data problems" lines if any.
- **Polish:** consistent spacing, hover/press states, tooltips, no text clipped, no horizontal scrollbars, window resize handled (re-layout), first-launch framing shows the whole Moon with margin (not cropped), smooth fade (0.2 s) when panels switch.
- **Cuts if late:** drop Compare first, then the surface overlay text.

---

## U11. QA matrix and interaction probe (P0)

Extend `InteractionProbe.cs` (or add a menu check) to automate what you can, and run the manual matrix below at both resolutions. Record each row in `docs/unity_status.md`.

| # | Check | Expected |
|---|---|---|
| 1 | Launch | Whole Moon visible, badge present, no errors |
| 2 | Camera 1,2,3,4 then 1 (keys and buttons) | No corruption; Approach ends in Local Orbit; Surface shows skyline |
| 3 | Select each of 3 sites | Markers and panels update |
| 4 | Inspect: change time, duration, site, Analyze | Cards, timeline, chart consistent |
| 5 | Invalid time text, out-of-range, window past end | Message, no exception |
| 6 | Scrub timeline by drag and keys | Sun/Earth/terminator move; chart dots flip filled/hollow correctly |
| 7 | Find mode, both policies, click a card | List switches; approach starts |
| 8 | Tab, Esc, H, Space, F, R | Behave as documented |
| 9 | Drag on empty UI area | Orbits the Moon (UI does not block) |
| 10 | Resize window 1366x768 to 2560x1440 | No overlap, no clipping |
| 11 | Missing data folder (rename temporarily) | Error panel, no crash |
| 12 | Console | Zero errors; warnings explained |
| 13 | Performance | At least 60 FPS at 1080p in the editor Game view on this machine (30 minimum); no per-frame GC spikes from UI |

---

## U12. Build and exe test (P0, start by 8:00 PM)

1. File > Build Profiles > Windows: Scripting backend **Mono** (faster than IL2CPP), Development Build off, scene list contains only `Assets/Scenes/LunarNavigator_Main.unity`. Player Settings: product name `Lunar Navigator`, windowed 1920x1080 default, resizable window on, fullscreen mode `Fullscreen Window` allowed (Alt+Enter).
2. The first HDRP build is slow (shader compilation). Start it early. Output `Builds/Win64/LunarNavigator.exe`.
3. Run the exe **outside the editor**. Verify: StreamingAssets JSON loads (badge correct), all 4 camera modes, UI text and fonts, no pink or black materials, keys work, window resize, Alt+Enter, no crash on quit. Check `Player.log` (`%USERPROFILE%\AppData\LocalLow\<company>\<product>\Player.log`) for errors.
4. Fix, rebuild, and repeat until clean.
5. Zip `Builds/Win64` to `LunarNavigator_Win64.zip`. Keep large source TIFFs out of the zip if they are not needed at runtime (they are baked into assets already). Tell the user the zip path and size. Uploading the zip and testing the public link in a private window is the user's job.

---

## U13. Real-data integration (P0, whenever real files appear)

Trigger: `Assets/StreamingAssets/analysis/` contains a non-stub site file, or `docs/science_status.md` mentions a delivery. At each checkpoint (see section 12) read `docs/science_status.md` and list the folder. When real files exist:
1. Reload data (`Lunar Navigator > Data > Reload`). Confirm the repository now uses real files only (`IsDemoMode = false`) and the badge shows the real `data_status`.
2. Run `Verify > Data Parity`. All sites must PASS. A FAIL means the two engines disagree: **do not fix by editing real files**. Write the details (site, index, expected vs got) in `docs/unity_status.md` and tell the user to relay to the Python thread.
3. Run `Verify > Frame` (F1): the Earth direction stays within about 9 degrees of lat 0, lon 0 over the range. If it fails, fix the longitude/handedness conversion in your layer, re-run F2 to F4.
4. Re-do the U4 flip-point comparison with real data (3 transitions) and screenshot.
5. Confirm every UI element still lays out with real numbers (longer values, many sectors).
6. If `GEOMETRIC_ONLY` is delivered first, then upgraded to `TERRAIN_AWARE_ESTIMATE`, repeat 1 to 4 on the upgraded files.
7. Remove nothing from stubs; they stay as the fallback.

---

## 12. Checkpoints with the (separate) backend

Check at about 6:30 PM, 7:30 PM, 8:30 PM, and before each build: read `docs/science_status.md` (read-only) and `Assets/StreamingAssets/analysis/`. Do not message or wait for the Python thread. If nothing real exists by **8:30 PM**, ship with stubs: the badge says `ILLUSTRATIVE DEMO` and the submission must describe the app honestly as a prototype with a documented science pipeline in progress. Tell the user immediately so they can decide.

---

## Definition of done (Track B)

- [ ] Zero Console errors; the warning fixed.
- [ ] Data layer works with stubs and with real files without code changes.
- [ ] Parity check PASS on whatever data is loaded; frame checks F2 to F4 PASS (F1 once real data exists).
- [ ] Badge, mode switch, Inspect panel, metrics cards, timeline, skyline chart, Surface skyline ring all working at 1920x1080 and 1366x768.
- [ ] Find Opportunities works (P1) or is disabled honestly.
- [ ] Windows build runs outside the editor; zip created.
- [ ] `docs/unity_status.md` and `docs/unity_architecture_notes.md` written; everything committed with path-scoped commits.
