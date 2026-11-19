# LunarNavigator v1.1.1 validation - 19 November 2026

## Artifact identity

- Installer: LunarNavigator_Setup.exe, 68,122,076 bytes (64.97 MiB).
- SHA-256: D8C490407294FB7B422087D0CE0DB9644CC4E3972E906321C7672C5EAFE9B1C3
- Matching checksum file is shipped separately; a checksum is not a signature.
- Unity application version, launcher metadata and setup version: 1.1.1.
- Immutable candidate: Builds/Packaging/v111-verified/installer (local only).

## Executed checks

| Check | Result |
| --- | --- |
| Unity 6000.6.3f1 Windows player build | Succeeded in 48.1 seconds, zero errors and warnings |
| Source provenance | All 228 Unity source files matched the fresh build's hash manifest; staged Assembly-CSharp.dll matches the fresh player |
| Backend/deployment regression suite | 166 passed in the build pipeline and again from the clean exported source; one existing AnyIO deprecation warning |
| Launcher regression harness | 13 checks passed, including quoting, session identity and owned-process cleanup |
| Editable number slots | 24 attached-control checks passed; both forms, constraints, invalid entries, arrows and presets |
| Keyboard-event checks | Typing, delayed commit, Enter, Escape and focus-loss commit passed |
| Finder UI request boundary checks | Five checks passed, including 28-day period with 3/4/7-day and exact-fit stays, plus overlong-stay rejection |
| Private bundled runtime | Import/catalog smoke check and actual run_backend.py process health passed |
| Real NASA analysis | Bundled runtime completed GEOMETRIC_ONLY analysis for Connecting Ridge, preserving requested UTC and 72-hour coverage |
| Real contained Finder | Bundled runtime and real Unity Finder button both returned nine evaluated arrivals for a 72-hour period and 24-hour stay; source coverage ends at the period end |
| Real Inspector button | Analysis ready; selected UTC input preserved |
| Uninstall | Isolated clean, unrelated-root-file preservation and v1.1.0 upgrade/uninstall cases passed with real generated Python caches and extra runtime leftovers |
| Branding | Embedded icon resources extracted from launcher, player and setup |
| Demo | 1920x1080 original cropped to 1496x840, no upscaling, original audio stream copied; complete video/audio decode produced no errors |

## Uninstall test scope

Test installers disable Windows uninstall registration and shortcut creation and use
no compression; application files and cleanup code are the release sources. They
run in fresh workspace directories, never over the user's installation. They import
the installed scientific service, generate nested .pyc files, and add untracked
backend/player leftovers. Cleanup removes those owned folders and removes the root
when empty. An unrelated root note and external saved-data sentinel are preserved.
The historical v1.1.0 test installer is built from its retained installer/source stage.

The cleanup marker is read and owned folders removed in the same uninstall callback,
before standard installed-file removal. Recursive deletion never targets the root
installation directory. Running launcher mutex blocks uninstall until the app closes.

## Video identity

- Demo size: 66461296 bytes (63.38 MiB); duration approximately 9m41s.
- Demo SHA-256: 17e378e38cd3acf9e24ecbdd33f32a058b50ccec512b85b6ab327563eaa9c669
- Original and released audio packet SHA-256:
  3aae79c5c12da1103fa3844d74bc9c838b05afa7786dfc357b471e65707cdf52
- Sample frames across the recording were visually reviewed before and after cropping.
  This is not a transcript or an independent review of every spoken claim.

## Verification limits

The standalone player was started and initialized graphics/UI, but desktop access
could not target the release window for visual interaction. The restricted launcher
attempt did not establish a verified owned session. Standalone launcher/UI end-to-end
acceptance and a clean-machine installation are therefore not claimed. Frontend
checks used the running Editor; scientific process checks used the bundled runtime.
Live Windows installed-app registration was not changed or verified by these tests.
The installer is unsigned. These limits are retained rather than treating a successful
build as proof of all deployment behavior.

The original recording and published v1.1.0 release are preserved. Runtime caches,
raw request evidence, build logs, test installations and artifact manifests stay local;
only curated source, documentation and presentation images belong in Git.
