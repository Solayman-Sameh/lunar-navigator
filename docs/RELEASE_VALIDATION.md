# v1.1.0 release verification

**Prepared 17 November 2026. This record does not itself claim GitHub publication.**

## Executed checks

| Check | Result |
| --- | --- |
| Icon refresh Unity player build, 6000.6.3f1 | Succeeded in 103.1 s; zero errors, zero warnings |
| Backend plus deployment test suite | 158 passed; existing AnyIO deprecation warning |
| C# launcher regression harness | 13 passed |
| Additional lifecycle tests against assembled private Python | 2 passed |
| Private runtime dependencies | Import/catalog/application construction smoke check passed |
| Production Inno Setup compilation | Succeeded |
| Launcher, setup and generated uninstaller icon extraction | Largest embedded icons match the supplied ICO exactly |
| Unity EXE icon extraction | Supplied artwork present at nine sizes; preview visually inspected |
| In-app header and About | Visually checked in Editor Play Mode |
| Startup splash configuration | Enabled; one valid custom sprite, two-second duration |
| Isolated test install and uninstall | Exit 0 with test-only registration disabled; files removed on uninstall |
| Test desktop/Start menu links | Created; both target branded launcher |

The portable NASA fixture is copied without changing original response bytes.
Its receipts and derived-output paths are relative, so retained-data tests can run
in a clean checkout rather than referring to the developer's data/runs directory.
After this portability change, the full backend/deployment suite was rerun:
**158 passed**, with the same single third-party deprecation warning.
An export of only the release-index files was then tested from a short Windows
path with no developer cache or environment copied into the source snapshot:
**158 passed** using the existing development interpreter. An earlier deeply nested
staging export hit Windows path-length limits in hashed cache filenames; use a
short checkout path on Windows. This was not an additional scientific model change.

## Exact installer identity

- File: `LunarNavigator_Setup.exe`
- Bytes: **68,213,422** (65.05 MiB)
- SHA-256: **6E7950B693EF508C6BEAC5FD400BEBD36D98E8A15CBE89FB42C0ACE8E0EB5A8E**
- Private runtime: CPython 3.11.9 x64; launcher targets .NET Framework 4.8 x64.
- API/OpenAPI compatibility version remains 1.0.0; app version is 1.1.0.

Source cleanup and test/documentation portability changes do not alter the
already-built Unity application or scientific runtime implementation. The release
asset checksum identifies the previously verified icon-refresh installer.

## Verification limits

The first isolated installer test created files/shortcuts but was denied access
when creating its separate HKCU uninstall key (Windows code 5). It rolled back
cleanly. A subsequent test disabled only registration and redirected shortcuts
inside its test folder; production setup retains normal registration and real
shortcut destinations. Its uninstaller was inspected and then removed the test
installation successfully. The existing production installation was preserved.

The build environment could not capture targetable newly launched setup/player
windows. The live Windows installed-app listing, taskbar appearance and installer
wizard are configured but not claimed as visually verified here. Editor interface
captures are real application captures, not generated design references.

No clean Windows machine, fresh live NASA acquisition, exhaustive input proof,
terrain-model validation or operational mission certification is established by
these checks. Historical aggregate Unity test counts from prior reports are not
repeated as newly executed v1.1.0 results.
