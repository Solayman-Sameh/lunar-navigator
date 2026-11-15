# Implementation Plan: Lunar Navigator
**Branch**: 001-lunar-explorer | **Date**: 2026-11-15 | **Spec**: spec.md
## Summary
Build a modular HDRP desktop experience with shared selection, continuous camera state interpolation, offline assets and UI Toolkit overlays.
## Technical Context
C# / Unity 6000.6.3f1, HDRP 17.6, Input System 1.20, UI Toolkit. Windows desktop. GPU inspected: GTX 1650, 3935 MB. Retain current pipeline/package configuration. No new packages. Save generated assets to Assets/LunarNavigator, scene to Assets/Scenes/LunarNavigator_Main.unity.
## Constitution Check
PASS: explicit demo labeling, existing dependencies retained, P0 first, modular responsibilities, image and behavioral verification required. Rechecked post-design: PASS. No exemptions.
## Project Structure
Assets/LunarNavigator/Scripts: LunarCoordinates, LunarSite, NavigatorConfig, DemoSimulation, LunarTerrain, CameraDirector, NavigatorInput, LunarWorld, NavigatorUI, LunarApplication.
Assets/LunarNavigator/Editor: LunarSceneBuilder and LunarVerification.
Assets/LunarNavigator/Generated: meshes, textures, materials, site/config assets and volume profile. Prefabs: observation marker/rock where practical.
specs/001-lunar-explorer contains research, data-model, contracts, quickstart, checklists and tasks.
## Architecture and Coordinates
Globe radius 1000. Direction uses x=cos(lat)cos(lon), y=sin(lat), z=cos(lat)sin(lon). North=+Y. UV east-positive, equirectangular. Tangent basis at selected direction: east, up, north. 600-unit representative patch overlaps the sphere using spherical sag; local camera distances 6..260, ground observer 0.5..12. Physics collider and analytic height sample enforce clearance. Globe and local terrain share position/scale; transition changes detail visibility, not camera coordinate systems.
## Cameras and Input
Single camera with centralized Global/Approach/Local/Surface enum. Global yaw/pitch/distance damped. Approach has orient and logarithmic descent phases, quintic easing, roll-stable look rotation. Local targets patch origin. Surface anchors above sampled ground. Every mode command interpolates from current pose, clamping clearance. Skip ends approach safely. Input System polls keyboard/mouse in one adapter; UI Toolkit picking gates pointer actions. Never advertise pan unless implemented.
## Graphics
Retain HDRP asset. Lit rough Moon/regolith, normal maps, black background, directional Sun, fixed exposure + ACES with very small bloom. No fog or ray tracing. Editor generator builds cratered sphere, tangent mesh, reusable rocks and star mesh once. Retrieve NASA equirectangular color through UnityWebRequest where network permits, max 4096 for this GPU; procedural cratered color/relief fallback remains available. Earth/Sun visual size is illustrative. TAA in Balanced/Cinematic, cheaper AA in Performance; texture limits and LOD bias change with presets.
## UI and Time
UI Toolkit document with bundled USS. Floating branding, tools, mode selector, selected-site card, collapsible bottom timeline, dismissible popover. Fullscreen noninteractive root passes world input; buttons/popovers capture. Help lists every input. Clock epoch 2026-11-15 UTC, 30-day bounded seconds, speeds 1/60/3600/86400; deterministic ephemeris interface supplies directions. All model labels remain explicit.
## Testing and Performance
Editor verification checks coordinate roundtrip/poles, unique sites, deterministic provider and clock bounds, scene references and shaders. Play Mode verification checks repeated states, transitions/skip/interruption, marker selection, UI visibility and clock. Capture global/local/surface and inspect. Record performance using available Editor stats with resolution and limitations. No hardware extrapolation.
## Implementation Strategy
P0 wiring and interaction first; compile, play, fix. Then NASA/material/terrain and preset work; capture and validate. Converge against every FR and append corrections. No P2 scope until core delivery.
