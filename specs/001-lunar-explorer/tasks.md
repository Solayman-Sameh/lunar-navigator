# Tasks: Lunar Navigator
## Phase 1: Setup
- [x] T001 Retain HDRP and inspect GPU/dependencies; record in specs/001-lunar-explorer/research.md (FR-019–020).
- [x] T002 Create configuration/site assets and coordinate/provider contracts in Assets/LunarNavigator/Scripts/LunarSite.cs and LunarCoordinates.cs (FR-008–009,017,021).
## Phase 2: Foundations
- [x] T003 Implement deterministic clock/provider in Assets/LunarNavigator/Scripts/DemoSimulation.cs (FR-015–017).
- [x] T004 Build reusable globe, tangent terrain, rocks/materials and scene builder in Assets/LunarNavigator/Scripts/LunarTerrain.cs and Editor/LunarSceneBuilder.cs (FR-001–002,018–021).
## Phase 3: US1 Global exploration
**Acceptance**: globe appears, drag/zoom bounded, three sites selectable, provenance readable.
- [x] T005 [US1] Implement shared selection and world markers in Assets/LunarNavigator/Scripts/LunarApplication.cs and LunarWorld.cs (FR-009–010,021).
- [x] T006 [US1] Implement Global orbit/focus/reset and Input System adapter in Assets/LunarNavigator/Scripts/CameraDirector.cs and NavigatorInput.cs (FR-003,011).
## Phase 4: US2 Descent and observation
**Acceptance**: approach completes, skip/interruption safe, local/surface controls valid over repeated switches.
- [x] T007 [US2] Implement approach/local/surface states and terrain clearance in Assets/LunarNavigator/Scripts/CameraDirector.cs (FR-004–008).
## Phase 5: US3 Time and overlays
**Acceptance**: every button/shortcut works, popovers dismiss, Tab restores, clock deterministic.
- [x] T008 [US3] Implement floating UI, help, settings and timeline in Assets/LunarNavigator/Scripts/NavigatorUI.cs and UI/Navigator.uss (FR-011–015).
- [x] T009 [US3] Integrate Sun/Earth rendering and time-driven lighting in Assets/LunarNavigator/Scripts/LunarWorld.cs (FR-016).
## Phase 6: US4 Visual quality
**Acceptance**: detailed globe/local captures, black vacuum, no pink surfaces, all presets change costs.
- [x] T010 [US4] Attempt NASA imagery, preserve provenance and improve fallback in Assets/LunarNavigator/Editor/LunarSceneBuilder.cs and Documentation/Sources (FR-002,019).
- [x] T011 [US4] Configure three presets, HDRP exposure/AA/filmic mapping in Assets/LunarNavigator/Scripts/LunarWorld.cs (FR-019–020).
## Phase 7: Verification and delivery
- [x] T012 Create and execute editor/runtime checks in Assets/LunarNavigator/Editor/LunarVerification.cs (FR-022, SC-001–004).
- [ ] T013 Inspect Play Mode captures/input and fix defects in Assets/Scenes/LunarNavigator_Main.unity (FR-022, SC-002–006).
- [x] T014 Write README.md and DEVELOPMENT_REPORT.md with measured evidence and limitations (FR-023).
## Dependencies and implementation strategy
T001 -> T002/T003 -> T004 -> T005/T006 -> T007 -> T008/T009 -> T010/T011 -> T012/T013 -> T014.
Independent data/clock and UI styling can run in parallel in separate files, but this session executes sequentially. No optional P2 work blocks these tasks. Tests are meaningful coordinate/model and runtime behavioral verification; input/captures are exercised separately.

## Phase 8: Convergence
- [x] T015 Fix zero-height overlay root and verify visible controls at 16:9 in Assets/LunarNavigator/Scripts/NavigatorUI.cs (FR-012-014 partial).
- [x] T016 Integrate retrievable NASA color, improve starting phase and separate polar labels in Assets/LunarNavigator/Editor/LunarSceneBuilder.cs and Scripts/NavigatorUI.cs (FR-002,010,019 partial).
- [x] T017 Verify actual simulated keyboard/mouse delivery, UI capture, scrubbing and approach continuity through Unity Editor (FR-004-007,011,015 partial).
- [x] T018 Measure runtime render costs and make preset differences meaningful in Assets/LunarNavigator/Scripts/LunarWorld.cs (FR-020, SC-006 partial).


## Phase 9: Convergence
- [x] T019 Persist nonempty HDRP volume overrides and verify exposure after reopening the scene in Assets/LunarNavigator/Editor/LunarPolish.cs (FR-019,021 contradicts).
- [x] T020 Correct pointer-delta delivery and complete end-to-end input verification in Assets/LunarNavigator/Scripts/NavigatorInput.cs and InteractionProbe.cs (FR-011 partial).
- [x] T021 Integrate NASA elevation, restrain soil normals, anchor marker leaders and verify surface horizon in Assets/LunarNavigator/Editor/LunarPolish.cs and Scripts/NavigatorUI.cs (FR-002,010,018-019 partial).
- [ ] T022 Validate Windows player build and final scene restart; report build-specific limitations in DEVELOPMENT_REPORT.md (FR-001,022-023 partial).

## Phase 10: Convergence
- [x] T023 Disable shadow casting on artistic Sun/Earth display meshes and verify the lit surface in Assets/LunarNavigator/Scripts/LunarWorld.cs (FR-007,016,019 contradicts).
- [x] T024 Improve representative horizon scale and close-range regolith detail, with explicit illustrative labeling, in Assets/LunarNavigator/Scripts/LunarTerrain.cs and Editor/LunarPolish.cs (FR-018 partial).

## Phase 11: Convergence
- [ ] T025 Hide the representative terrain patch during the far approach and hand it off only when it fills the view, then inspect approach captures and rebuild in Assets/LunarNavigator/Scripts/LunarWorld.cs (FR-005,019 partial).


## Phase 12: Convergence
- [ ] T026 Preserve globe visibility at the Global zoom minimum independently of the approach handoff in Assets/LunarNavigator/Scripts/LunarWorld.cs (FR-003,005 partial).

## Phase 13: Convergence
- [ ] T027 Map the UI presets to the project's existing HDRP High Fidelity/Balanced/Performant quality assets, retaining no-ray-tracing settings, and verify switching in Assets/LunarNavigator/Scripts/LunarWorld.cs (FR-020 partial).

## Phase 14: Convergence
- [ ] T028 Add combined close-range regolith fragments and boulders for a grounded Surface view in Assets/LunarNavigator/Editor/LunarSurfaceDetail.cs (FR-018 partial).
