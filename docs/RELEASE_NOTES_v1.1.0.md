# LunarNavigator v1.1.0

This release brings the corrected navigation and Mission Assessment interface
together with a self-contained Windows installer and consistent application branding.

## Windows download

- **LunarNavigator_Setup.exe** — installs the launcher, Unity player, private Python
  runtime and scientific backend. Python and Unity are not separately required.
- **LunarNavigator_Setup.exe.sha256** — optional integrity check for this exact EXE.

Close the app, run setup, and select the installation folder. To update an existing
installation, select its current folder. Launch LunarNavigator from the installed
shortcut or root launcher. The previous standalone ZIP is superseded.

## Changes

- Precise coordinate picking without nearby-landmark snapping.
- More consistent WASD, dragging and interface input handling.
- Refined Mission Assessment layout, feedback, provenance and stale-result handling.
- Installation-folder choice and managed local backend startup/shutdown.
- New icon on launcher/player/setup/uninstaller and shortcuts, plus startup splash,
  header branding and About panel under Guide.
- Curated source repository, fresh interface capture, revised proposal and documentation.
- Small portable retained NASA regression evidence with machine-independent tests.

## Scope and verification

Results remain **GEOMETRIC_ONLY**: NASA/JPL Sun/Earth directions against a spherical
lunar horizon. Local terrain is illustrative. No terrain-validated landing safety,
power budget or RF communications certification is provided.

Uncached requests need internet; matching validated caches can be replayed offline.
The installer is unsigned. Build-host checks passed; clean-machine acceptance and
the live Windows installed-app listing are not claimed as verified in this environment.
See `docs/RELEASE_VALIDATION.md` in the tagged source for the exact scope.

A narrated walkthrough is planned; the empty local MP4 placeholder is not a release asset.
