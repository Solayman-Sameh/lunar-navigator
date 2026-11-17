# Clock-fix packaging validation - 17 November 2026

The local v1.1.0 installer now contains the verified clock and seconds controls.
No GitHub push, release upload or publication was performed.

## Delivered files

- Builds/LunarNavigator_Setup.exe
- Builds/LunarNavigator_Setup.exe.sha256
- The matching pair is also under Builds/Dist/.
- Immutable candidate: Builds/Packaging/v110-clock-runtime-fixed-final/installer/.
- Previous root pair: Builds/ClockPackaging/previous-installer/.
- Previous Dist pair: Builds/ClockPackaging/previous-dist/.

Installer size: 68,212,684 bytes (65.05 MiB).
SHA-256: 58F4CB88EF386C58059A29A81E13A0CCCA559266D6CAC773E5B39CD5DB0618C1

## Executed checks

| Check | Result |
| --- | --- |
| Unity 6000.6.3f1 Windows player | Succeeded in 191.5 seconds; zero errors and warnings |
| Unity source manifest | All 226 files matched SHA-256 and file-set checks |
| Backend/deployment regression suite | 158 passed; one existing AnyIO deprecation warning |
| Launcher regression harness | All 13 checks passed |
| Private Python runtime | Import, catalog and application smoke check passed; pinned dependencies assembled from cached wheels |
| Inno Setup | Compiled successfully in 200.969 seconds |
| Staged managed code | Assembly-CSharp.dll SHA-256 matches the fresh Windows build |
| Integrity | Candidate, manifest, checksum, root installer and Dist installer agree |
| Embedded branding | Launcher and setup contain seven icon sizes; Unity player contains nine. Moon/cyan-crosshair previews visually checked |
| Native application | Launched the exact candidate launcher and its private backend, not Editor Play Mode |
| Native clock | Started PLAYING at 1x, seconds visible, advanced through analysis without reset or pause |
| Native analysis | Real uncached Sun/Earth NASA acquisition; selected and returned start both 2026-11-17T18:28:35Z; Analysis ready and Requirements met |
| Native pause/resume | Mission held at 18:30:17 while LIVE UTC advanced to 18:30:58; playback resumed and advanced afterward |
| Native shutdown | Normal window close; launcher recorded owned-session cleanup |

The prior source-level 35 checks are recorded in CLOCK_RUNTIME_FIX.md. Native
packaging checks did not reinstall over the user installation or repeat a clean-machine
install/uninstall test. Run the new setup to update the installed copy.

## Packaging compatibility repair

Windows PowerShell 5 initially decoded a UTF-8 material filename as ANSI. The
source-manifest reader in deployment/build-release.ps1 now explicitly uses UTF-8.
The original gate failure is retained in Builds/ClockPackaging/packaging-first-attempt.log.
No source hash was ignored or bypassed. The successful pipeline verified the manifest.

## Evidence

Builds/ClockPackaging contains the build status, packaging log, artifact integrity
record, icon-resource record/previews, native-player-validation.json, and original
native request/response JSON. The native run is a619ab7f52c141e09dc70697a0e27d5f.
Session e008a957545a4cdf8b7854dd9d767188 verified backend PID 52388 and Unity PID 88696
and recorded clean shutdown. Raw NASA receipts remain with the application run.
