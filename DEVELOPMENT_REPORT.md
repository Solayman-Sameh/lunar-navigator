# Development report — Lunar Navigator

Updated 2026-11-15. This report distinguishes implemented behavior from exercised evidence.

## Implementation
- Existing Unity 6000.6.3f1 project and HDRP 17.6 retained; no CLI update or MCP changes.
- Main scene is Assets/Scenes/LunarNavigator_Main.unity and is included in build scenes.
- NASA SVS/LROC 4K color and LOLA elevation-derived global relief, with saved provenance.
- Global orbit, logarithmic cinematic approach, local orbit and ground-anchored Surface.
- Shared selection for three fictional south-polar candidates; projected marker labels and list.
- UI Toolkit floating controls, Sites/Layers/Settings/Help, site information and immersive toggle.
- Deterministic 30-day UTC clock, play/pause, speed choices, scrubbing and hour steps.
- Sun/Earth display geometry and time-dependent directional light from demo provider.
- Representative cratered terrain, combined rocks, rough regolith, filmic response and three graphics presets.

## Verification evidence
- Model checks and runtime assertions are saved in Documentation/Verification/models.txt and runtime.txt.
- The initial runtime test executed 24 camera mode changes, checking 100 intermediate poses each, shared selection, skip, interruption, clock bounds and UI/preset states: 4877 assertions passed before added persistence/layout regressions.
- Real Input System keyboard and mouse events ran through application Update and UI Toolkit: all 22 interaction checks passed on the corrected pass. See interaction.txt. This is separate from direct method tests.
- Tested inputs: H, Escape, Tab, Space, modes 1–4, F/R, world drag, scroll, Sites selection, Settings/Performance, UI drag suppression, outside click and timeline pointer scrubbing.
- Captures were taken and visually inspected; early diagnostic captures intentionally preserve defects. Use final named captures for the latest result.
- A 180-frame Editor sample at 2560x1440 on GTX 1650: mean 31.67 ms, median 33.34 ms, p95 47.77 ms. Approximately 32 fps mean, including Editor overhead. This is not a standalone GPU benchmark or a 1080p guarantee.

## Bugs found and corrected
- Zero-height overlay root placed bottom controls offscreen: explicitly sized root and applied overlay class.
- Empty saved HDRP volume profile: create/persist override sub-assets; added regression checks.
- Overlapping polar candidate labels: separate screen rows with anchored dots/leaders.
- Pointer simulation did not populate mouse delta: position-derived deltas now serve both actual mouse motion and verification devices.
- Excessive regolith normal strength produced a noisy surface: restrained normals and adjusted tiling.
- Celestial display meshes cast an artificial shadow across the observer: disabled their shadow casting, retaining terrain and rock shadows.
- Tight globe curvature limited the illustrative local horizon: gentler representative patch curvature, clearly documented as a visualization scale.
- New verification script initially had an ambiguous MouseButton namespace: qualified Input System enum and recompiled.
- Transient MCP unavailability during assembly reloads: retried after reload without modifying the connection setup.

## Scientific and visual limitations
- This is an interactive visualization demonstration, not mission-planning software.
- Sites are fictional; local terrain is not a reconstruction of those coordinates.
- Sun/Earth directions are illustrative deterministic functions, not NASA/JPL predictions.
- Global color is authentic display imagery; global relief uses artistic vertical scaling.
- Globe radius is 1000 display units. Local curvature uses eight times that radius to show a useful representative horizon; local eye-height labels and celestial sizes are illustrative.
- Earth texture is generated, not an authentic Earth mosaic. No claim of survey-grade accuracy or full photogrammetric photorealism.
- No real-time ray tracing, scientific backend, calendar text entry, camera bookmarks, screenshot UI or optional P2 presentation tools.
- No true optical UI blur; translucent panels provide a practical lightweight alternative.

## Attribution and dependencies
See Documentation/Sources/ATTRIBUTION.md. NASA reference pages are saved in Documentation/Sources. The first UnityWebRequest TLS attempts failed; the OS HTTP client inside the Editor successfully retrieved the references and assets. No new packages were installed.

## Reproducibility and next steps
Open the main scene and Play. Run the Editor verification menu to check models/references. The interaction probe is launched explicitly for testing and is never part of the saved application scene. Source and all assets are local and usable offline.
For a production scientific product, integrate a verified site catalog, measured local DEMs and JPL Horizons; expand hardware testing and replace the illustrative Earth. Improve close-range terrain using measured high-resolution imagery before claiming survey fidelity.

## Final validation status
Final scene restart, final captures and Windows build results are recorded below as those checks complete.

### Post-correction evidence
- Reopened scene: 244 model/reference/persistence checks passed; zero failed.
- Final runtime suite: 4878 assertions passed; zero failed.
- Expanded Input System suite: 29 checks passed, zero failed, including all three projected markers, double-click focus, local drag, surface look and surface height scroll.
- Final native Editor resolution during timing was 2560x1440; presentation captures are rendered at 1600x900.
- Representative terrain is now hidden from the far approach; it is enabled only in the near-site handoff. Earlier captures showing a square patch are diagnostic history, not the final implementation.
- The generic MCP build/get-settings wrappers returned ambiguous command-name errors. The existing unchanged Unity CLI successfully validated and queued the fully qualified build run command. No CLI upgrade/configuration change was used.

## Feature status boundaries
| Area | Status |
|---|---|
| Four camera modes, selection, controls, timeline, popovers | Implemented and exercised through runtime/input checks |
| Global lunar color/relief | Authentic NASA display assets integrated; vertical scale artistic |
| Local terrain | Functional representative landscape; photogrammetric fidelity remains partial |
| Earth | Functional lit directional representation; generated texture and artistic apparent scale |
| Real ephemerides and site catalog | Deliberately absent in this demo iteration, clearly labeled |
| Optional bookmarks, capture UI and presentation tools | Not implemented; P2 scope |
| Windows executable | Build validation in progress; final result follows below |

### Session blocked by optional player build
The first Windows build remained in HDRP shader compilation for more than 30 minutes. It is not recorded as successful. Editor commands that need the main thread time out during the build.
The available desktop control helper was unavailable, and Windows denied an attempt to stop/restart the exact known LunarNavigator Editor process (PID 20176). The restart script stopped on that denial; the Editor was NOT restarted and no alternate termination method was used.
The scene/assets had been saved before the build. No CLI upgrade, package upgrade or MCP configuration change occurred.

Latest source changes awaiting compilation/verification after the build releases the Editor:
- Preserve the globe at Global minimum zoom independently of terrain handoff.
- Map presets to existing HDRP quality assets.
- A near-field geology authoring helper is written but has not yet generated scene geometry.
The final uninterrupted descent capture and complete readiness sign-off remain outstanding. Existing test counts describe the previously exercised version, not these pending changes.

To resume: cancel the build from Unity's progress window or let it finish. Wait for the pending script recompile, run Lunar Navigator/Add Near Surface Geology, reopen/save the main scene, rerun model/runtime/input checks for affected changes and inspect normal-time approach captures. Keep standalone packaging separate until the Editor demonstration passes final review.
