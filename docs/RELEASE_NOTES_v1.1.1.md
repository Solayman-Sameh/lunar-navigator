# LunarNavigator v1.1.1

This update makes Finder's search period enclose the entire mission, adds direct
numeric entry, and removes generated backend/player leftovers during uninstall.

## Downloads

- **LunarNavigator_Setup.exe**: Windows x64 installer. Unity and Python are included;
  no development environment is needed.
- **LunarNavigator_Setup.exe.sha256**: integrity check for this exact installer.
- **LunarNavigator_Demo.mp4**: cropped narrated walkthrough, downloadable separately.

Close LunarNavigator, run setup and select the existing installation folder to
update. v1.1.0 remains available as a previous release.

## Changed

- A 28-day total search period and four-day stay searches arrivals through day 24.
  All missions finish by the period end; arrivals are sampled every six hours.
- An exact-fit stay evaluates one arrival. A stay longer than the period is rejected.
- Type directly into year/month/day/hour/minute/second slots, Finder period/stay
  days and hours, and Finder constraints. Enter or focus loss applies; Escape cancels.
  Invalid values restore the previous value. Arrows, presets and sliders still work.
- Uninstall removes application-owned backend/player folders and generated files,
  then the empty installation folder. Unrelated root files and separately stored
  cache, logs and results under %LOCALAPPDATA%\LunarNavigator are preserved.
- API compatibility: new Unity requests CONTAINED_MISSION explicitly; omitted mode
  retains the legacy ARRIVAL_SPAN API behavior. Restart development backends after
  updating source. The packaged launcher manages the bundled backend automatically.

## Scope

NASA/JPL-derived results remain GEOMETRIC_ONLY. Visible terrain is illustrative;
no terrain-validated landing safety, solar-power budget or radio-link certification
is provided. Uncached requests require internet. The installer is unsigned.

See [v1.1.1 validation](RELEASE_VALIDATION_v1.1.1.md) for executed checks and limits.
The demonstration was recorded before the About/version label was bumped; it
illustrates the updated source interface and may show v1.1.0 in version displays.
