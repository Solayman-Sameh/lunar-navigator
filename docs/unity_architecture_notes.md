# LunarNavigator — Unity Architecture Notes

## 1. System Overview

`LunarNavigator` is an interactive 3D lunar landing-site evaluation simulator built with Unity 6 (6000.6.3f1), High Definition Render Pipeline (HDRP 17.6), Unity Input System, and UI Toolkit. It visualizes lunar south pole exploration targets with real or synthetic scientific data (ephemeris tracks, local terrain horizons, and line-of-sight visibility analysis).

---

## 2. Core Subsystems

### 2.1 Application & Lifecycle (`LunarApplication.cs`)
- Central entry point and catalog host (`ISiteCatalog`).
- Synchronizes camera director (`CameraDirector`), clock (`DemoSimulation`), world renderer (`LunarWorld`), and user interface (`NavigatorUI`).
- Manages site selection (`SelectSite`) and hierarchical "back" navigation (`Back`).

### 2.2 Navigation & Camera State Machine (`CameraDirector.cs`)
Maintains 4 camera modes with seamless transitions and collision avoidance:
1. **Mode 1 — Global Orbit:** Spherical orbit around the Moon's center with inertial damping and clamped pitch.
2. **Mode 2 — Cinematic Approach:** Multi-stage logarithmic descent towards the selected site's surface anchor, transitioning smoothly into Local Orbit.
3. **Mode 3 — Local Orbit:** Clamped spherical orbit around the site's local terrain anchor.
4. **Mode 4 — Surface Look:** Ground-anchored eye-level observation (observer height 0.5m – 12m) oriented tangent to the lunar sphere.
- **Terrain avoidance:** Ensures camera altitude never clips through the globe radius or local terrain heightmap.

### 2.3 Clock & Ephemeris Simulation (`DemoSimulation.cs`)
- Deterministic UTC clock provider (`DateTime Utc`).
- In demo/fallback mode, computes analytical ephemerides via `IllustrativeDemoProvider`.
- When scientific analysis data is loaded, time spans the full data window (31 days, 600s intervals), driving directional lighting and celestial disc positioning.

### 2.4 Coordinate Conventions & Framing (`LunarCoordinates.cs`)
- Selenographic latitude $\phi$ and east longitude $\lambda$ convert to Cartesian coordinates:
  $$P = R \cdot (\cos\phi\cos\lambda, \sin\phi, \cos\phi\sin\lambda)$$
- `Frame(up)` creates an orthonormal tangent frame (North, East, Up) for local terrain and observer orientations.
- Direction to celestial bodies is calculated in the local site frame and transformed into world space for HDRP directional lighting and visual markers.

### 2.5 World Rendering & Terrain (`LunarWorld.cs`, `LunarTerrain.cs`)
- Renders the NASA LROC 4K color mosaic on a geocentric UV sphere.
- Cross-fades dynamically between the whole-globe sphere and the high-resolution local terrain patch (`LunarTerrain`) when the camera is close to the surface (<330m) or in Local Orbit / Surface Look.
- Positions visual discs for the Sun (emissive sphere) and Earth at distance.

### 2.6 User Interface Architecture (`NavigatorUI.cs`, `Navigator.uss`)
- Built entirely with **UI Toolkit** (`UIDocument`, USS styles, responsive VisualElements).
- Uses `RuntimePanelUtils.ScreenToPanel` to project 3D lunar sites into 2D floating markers with connector lines.
- **Fullscreen-First Design Override:** Replaces permanent left/right sidebars with lightweight floating glassmorphic panels and contextual popovers:
  - Top-left: Minimal title and persistent scientific `data_status` badge.
  - Top-center: Mode switch (`Inspect` / `Find Opportunities`).
  - Top-right: Quick utility pills (`Sites`, `Layers`, `Settings`, `Help`).
  - Bottom: Collapsible 3-lane timeline (`Sun`, `Earth`, `Both`) with interactive playhead scrubbing and mission window brackets.
  - Contextual floating cards: Dismissible/collapsible Site Inspector, Opportunity Finder, Metrics grid, and Skyline elevation chart.
  - `Tab` key toggles UI visibility into pure cinematic immersion (while keeping the scientific status badge visible).
  - Empty UI containers set to `picking-mode: ignore` to ensure unobstructed 3D orbit mouse dragging.

---

## 3. Data Integration Strategy (Data Contract v1)

- **StreamingAssets Path:** `Assets/StreamingAssets/analysis/`.
- **Synthetic Stubs:** `stub_*.json` owned by Unity, generated with self-consistent mathematical models for demo operation (`ILLUSTRATIVE DEMO`).
- **Real Scientific Data:** Created by Python science engine (`index.json`, `<site_id>.json`, `opportunities.json`). Unity loads real files automatically when present.
- **Validation & Parity:** C# `VisibilityMath` re-computes line-of-sight and metrics against stored arrays to guarantee mathematical agreement across platforms.
