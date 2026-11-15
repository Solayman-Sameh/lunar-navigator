# Research and decisions
- Decision: retain installed HDRP 17.6 and Input System 1.20. Rationale: Editor confirms compatible active HDRP on Unity 6000.6.3f1. Alternative package migration adds unnecessary risk.
- Decision: 1000-unit globe and tangent patch. Rationale: unified continuous poses without astronomical floats. Alternative real-radius coordinates would require larger rebasing complexity.
- Decision: UI Toolkit. Rationale: installed built-in retained mode UI, simple picking and adaptive layout. Alternative IMGUI less suitable for premium controls.
- Decision: fictional south-polar candidates and deterministic demo ephemeris. Rationale: no verified scientific catalog/backend. Alternative invented accurate values rejected.
- NASA references are requested through UnityWebRequest in the Editor; saved source responses document availability. Texture failure never blocks core functionality.
- Hardware: GTX 1650 / 3935 MB. Start Balanced, cap imported color texture at 4096, avoid ray tracing and runtime tessellation.
