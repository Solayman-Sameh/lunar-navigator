# Feature Specification: Lunar Navigator

**Feature Branch**: `001-lunar-explorer` (directory identifier; no branch required)
**Created**: 2026-11-15
**Status**: Ready for design
**Input**: Autonomous Lunar Navigator mission supplied in conversation on 2026-11-15.

## User Scenarios & Testing

### User Story 1 - Explore the Moon (Priority: P1 / mission P0)
As an explorer I see a detailed Moon occupying a full-window space viewport, orbit it,
zoom and select a south-polar candidate without losing context.
**Why this priority**: All other journeys begin here.
**Independent Test**: Open main scene, play, drag and scroll, select each candidate.
**Acceptance Scenarios**:
1. Given startup, when playing, then a textured, lit globe appears without errors.
2. Given Global, when dragging/scrolling, then orbit and distance change smoothly within
bounds, without roll or penetration; clicking UI does not move the camera.
3. Given any visible marker or Sites list, when selecting, then the same shared site
becomes active and its coordinates, description and provenance are accessible.

### User Story 2 - Descend and observe (Priority: P1 / mission P0)
As an explorer I approach a selected site, inspect terrain from orbit and stand above it.
**Why this priority**: The continuous journey defines the application.
**Independent Test**: Use all four modes, interrupt approaches, replay and return.
**Acceptance Scenarios**:
1. Given selection, when Approach starts, then a configurable eased journey orients to
the site, closes distance perceptually smoothly and finishes in Local Orbit.
2. Given an approach, when skipped or interrupted with 1/3/4, then the final pose is
finite, clear of geometry, and input remains responsive.
3. Given Local Orbit, when dragging/zooming, then the site stays targeted and camera
clears the terrain; overhead and oblique views remain available.
4. Given Surface, when looking or changing observer height, then the position remains
ground anchored and the airless terrain horizon remains visible.
5. Given repeated mode changes, then selection persists and orientation remains valid.

### User Story 3 - Control a demonstration (Priority: P1 / mission P0)
As a user I control time and discover all inputs using lightweight overlays.
**Independent Test**: Operate time, popovers, shortcuts and immersive toggle.
**Acceptance Scenarios**:
1. Given paused time, when playing at different speeds or scrubbing, then UTC and
celestial directions change deterministically; paused time remains stationary.
2. Given any popover, when Escape or an outside click occurs, then it closes.
3. Given visible UI, when Tab is pressed twice, then controls hide and restore.
4. Given Help, then every advertised shortcut is implemented and every working
shortcut is listed, including approach skip and observer height.

### User Story 4 - Experience convincing visuals (Priority: P2 / mission P1)
As a user I see authentic global imagery when retrievable, detailed representative terrain,
sparse stars, Sun/Earth representations and adjustable quality on desktop hardware.
**Independent Test**: Inspect captures from all modes and switch all presets.
**Acceptance Scenarios**:
1. Given NASA imagery, then source attribution accompanies the asset; otherwise a
cratered procedural fallback is used and honestly labeled.
2. Given local views, then craters, relief, rocks and rough regolith produce visible
low-angle shadows without atmospheric haze; terrain is labeled Illustrative Terrain.
3. Given any quality preset, then the viewport stays usable without ray tracing.

### Edge Cases
- Missing remote imagery: preserve procedural fallback, no startup network requirement.
- Unverified coordinates: use explicitly fictional demo candidates near the south pole.
- Approaching far-side sites: travel around the sphere before descending, never through it.
- Repeated commands and changing site mid-flight: restart from current valid pose.
- Extreme scrolling/pitch/height: clamp to safe distances and ground clearance.
- UI input capture, hidden markers, hidden UI and popover overlap: preserve world controls
and keep candidates accessible through the Sites list.
- Time range: clamp to a finite 30-day demo interval; no claim of real ephemerides.
- Scientific backend absent: application remains functional offline.

## Requirements

### Functional Requirements
- **FR-001**: Deliver a directly playable main scene and include it in build scenes.
- **FR-002**: Render a geographically oriented, detailed spherical Moon with authentic
imagery where obtainable, crater relief and documented fallback/provenance.
- **FR-003**: Global orbit supports 360-degree yaw, clamped pitch, damped drag/zoom,
momentum, safe distance limits, focus, reset and stable roll.
- **FR-004**: A centralized four-mode camera system shares selection and exposes
configurable durations, distances and easing, with safe interruption/replay/skip.
- **FR-005**: Approach orients outside the globe, descends logarithmically and finishes
in Local Orbit without an instantaneous global/local camera cut.
- **FR-006**: Local Orbit supports horizontal/vertical drag, scroll zoom, overhead and
oblique views, selected-site targeting, terrain clearance and Return to Globe.
- **FR-007**: Surface has ground-anchored position, full yaw, limited pitch, adjustable
height, Sun/Earth directions and return to Local Orbit.
- **FR-008**: Manage global/local precision using a bounded globe and a local tangent
patch with explicit visual scale; maintain finite poses during every transition.
- **FR-009**: At least three reusable south-polar candidates have unique IDs, name,
lat/lon, optional elevation, description and provenance; unverified sites are fictional.
- **FR-010**: Markers are geographically anchored, selectable and hidden on the far side,
with stable screen size, selected feedback and list access when not visible.
- **FR-011**: Use the installed Input System for left drag, scroll, double-click, F/R,
1–4, Space, Escape, H or ?, and Tab. UI must suppress pointer camera actions.
Right-drag pan is optional and must not be advertised unless implemented.
- **FR-012**: Provide full-window floating branding, top-right Sites/Layers/Settings/Help,
active bottom-center mode selector, site information and concise mode feedback.
- **FR-013**: Popovers for sites, information, simulation/graphics, layers and help close
via Escape/outside click; settings include scientific aids and observer height.
- **FR-014**: Tab hides/restores controls and no permanent sidebar or boxed viewport exists.
- **FR-015**: Deterministic UTC clock supports play/pause, speed choices, a 30-day scrubber,
step controls and reset. UTC date input is practical optional polish.
- **FR-016**: A labeled Illustrative Demo Provider supplies time-dependent Sun/Earth
directions and directional lighting. Provider interfaces support later replacement.
- **FR-017**: Geographic, ephemeris and local-terrain abstractions remain independent.
- **FR-018**: Terrain provides craters, relief, rocks, normals and rough materials; label
Illustrative Terrain, never a measured site reconstruction.
- **FR-019**: HDRP retains black vacuum, sparse stars, controlled bloom, filmic mapping,
anisotropic filtering and AA, without atmosphere, blue haze or ray tracing.
- **FR-020**: Performance/Balanced/Cinematic presets adjust concrete rendering costs.
- **FR-021**: Modular scene/bootstrap, coordinates, selection, camera, input, time,
rendering, UI, configuration, materials and reusable assets have wired references.
- **FR-022**: Verify compilation, Console, references, Play Mode, input, camera changes,
selection, timeline and captures; distinguish tested, partial and unverified work.
- **FR-023**: Provide README and development report with controls, limitations, provenance,
tests actually performed, defects, measured performance and recommended next steps.

### Key Entities
- Site: ID, display name, latitude/longitude degrees east, elevation, description, provenance.
- Selection: one active site shared across modes and UI.
- Camera configuration/state: mode, pose, distance, bounded angles, transition progress.
- Clock: UTC epoch, elapsed seconds, pause flag, speed and bounded interval.
- Provider: demo Sun/Earth directions; terrain height sampling and geographic site catalog.
- Graphics preset: rendering scale/AA, shadows and texture/LOD policy.

## Success Criteria
### Measurable Outcomes
- **SC-001**: Main scene enters Play Mode with no application compilation/runtime errors.
- **SC-002**: All four modes can be reached and returned from through 20 successive switches
without invalid poses, ground penetration or lost selection.
- **SC-003**: Each of three candidates can be selected through the list and a visible marker;
approach completes within the configured duration plus one second at normal frame rate.
- **SC-004**: Paused clock is stable, resume advances, and repeated scrub values yield identical
directions; every supported input is present in Help.
- **SC-005**: Captures show globe detail, cratered local terrain and a ground-level horizon,
with no pink materials; UI hides/restores and popovers dismiss correctly.
- **SC-006**: Report measured performance and preset behavior; target 30+ fps on detected
entry-level GPU and 60 fps at 1080p on a capable GPU, without claiming unmeasured hardware results.

## Assumptions
## Clarifications
### Session 2026-11-15
- Q: Which geographic data is authoritative? A: None installed; use fictional demo candidates and labeled illustrative terrain until verified data is integrated.
- Q: Which quality should start? A: Balanced on the detected GTX 1650; no ray tracing.
- Q: What time and scale apply? A: Paused 2026-11-15 UTC, 30-day demo range, visually compressed bounded coordinates; no scientific ephemeris claims.

## Engineering Assumptions
- This is an offline-capable desktop demonstration, not mission planning software.
- No verified site catalog is installed; start with fictional named demo candidates.
- Use a 1000-unit globe radius and bounded local tangent terrain to avoid kilometer-scale
float coordinates. Visual terrain scale and celestial sizes are illustrative.
- Demo epoch is 2026-11-15 00:00 UTC; range is 30 days, initially paused for a stable first view.
- Priorities follow the mission: P0 functionality, then P1 visuals, P2 optional polish.
- No dependency upgrades or external scientific integrations are required.
