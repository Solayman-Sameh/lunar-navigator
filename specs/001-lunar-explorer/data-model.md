# Data model
LunarSite (ScriptableObject): id, displayName, latitude, longitude, elevation, description, provenance. Validate unique ID, latitude [-90,90], longitude [-180,180].
NavigatorConfig (ScriptableObject): globe radius, approach/transition duration, global/local distance limits, observer height, easing curve.
Selection: LunarApplication.SelectedSite, changed through SelectSite; renderer, director and UI consume the same reference.
DemoSimulation: UTC epoch, bounded elapsed seconds [0,2592000], paused, speed. Sun/Earth directions are normalized deterministic functions of UTC.
CameraDirector: Mode enum, current pose, transition start/progress, orbit yaw/pitch/distance and surface look/height. Approach -> Local on completion/skip; any mode interrupts from current pose.
ILunarEphemeris: SunDirection(UTC), EarthDirection(UTC), Label. ISiteCatalog: Sites. ILocalTerrainProvider: SampleHeight(x,z).
