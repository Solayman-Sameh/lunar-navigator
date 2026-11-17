# LunarNavigator Windows deployment

The release payload uses Unity 6000.6.3f1, a private CPython 3.11.9 x64
runtime, a Windows GUI launcher, and Inno Setup. Python is not installed
globally. Uncached NASA/JPL requests require internet.

## Build

1. In the existing Unity Editor run Lunar Navigator > Build Windows 64-bit.
   For a closed project use batchmode with -executeMethod
   LunarNavigator.Editor.LunarBuild.BuildRelease. Never launch another Editor
   against an already-open project.
2. Run .\deployment\build-release.ps1.
   Prerequisites: backend\.venv, Roslyn, Inno Setup 6, and .NET Framework 4.8
   reference assemblies.
3. Packaging verifies the exact Unity source-file set and SHA-256 values in
   Builds\Win64\source-hashes.txt. Changed sources require another player build.
4. Candidates appear under Builds\Packaging\<build-id>\installer.
   Existing Builds\Dist files are not replaced automatically.
5. Validate the installed candidate before promotion or publication.

Optional arguments: -BuildId v110-candidate -Wheelhouse Builds\Packaging\Wheels
-FrameworkReferenceDir 'C:\path\to\4.8-reference-assemblies'

The wheelhouse path enables offline build-host assembly, with dependency closure
and platform compatibility checks. Reviewer-time pip/downloads are never needed.

A tested local reference profile is Unity's
Editor\Data\MonoBleedingEdge\lib\mono\4.8-api. The selected profile is recorded
in the candidate manifest. No Mono runtime is shipped for the launcher; it runs
on Windows .NET Framework.

## Lifecycle

Children are created suspended and assigned to a non-inheritable kill-on-close
Job Object before being resumed. Only the launcher owns its handle. The backend
retains an ephemeral loopback socket. Typed startup/health JSON is verified against
the actual child PID and random session before Unity starts.

On Unity exit the backend receives a shutdown event, followed by bounded cleanup.
Error dialogs appear after cleanup. Forced launcher termination closes the job.
The installer and launcher share a session-scoped mutex. Duplicate launches show
a notice rather than manipulating windows identified only by title.

Logs and scientific cache/results live under %LOCALAPPDATA%\LunarNavigator.
Uninstall preserves them. Installation is per-user and does not require elevation.
The destination-folder page is always shown, including reinstalls. Users can
type the installation path or browse to a parent folder; browsing appends
LunarNavigator automatically. All installed application files use that path.

## Checks

Run backend\.venv\Scripts\python.exe -m pytest backend\tests
deployment\test_backend_lifecycle.py -q -p no:cacheprovider

Run .\deployment\test-session.ps1 -ApplicationRoot 'C:\installed\LunarNavigator'
and repeat with -ForceLauncherExit.

The build runs deployment\LauncherTests.cs. See ../docs/RELEASE_VALIDATION.md for actual results.
Build-host testing is not clean-machine testing. Unsigned executables can trigger
SmartScreen; EV signing does not guarantee bypass.
