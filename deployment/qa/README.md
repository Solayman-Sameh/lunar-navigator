# Manual Unity clock regression checks

These Roslyn C# scripts are maintained test source, not generated build output.
Run against the real application in Unity Play Mode with the Unity CLI available.
They change test inputs and exercise pause, rollover and current-time controls;
use a development Play session. Results go to ignored Builds/ClockValidation/.

- verify-live-clock.cs: 29 clock/date assertions, including four regional cultures.
- verify-live-clock-analysis.cs: six integration assertions against a real backend;
  the Analyze button action is invoked and uncached NASA access may require internet.

From the project root, start a separate development backend for the integration check:

```powershell
New-Item -ItemType Directory -Force Builds/ClockValidation | Out-Null
.\backend\.venv\Scripts\python.exe backend/run_backend.py --port 8000 --storage-dir Builds/ClockValidation/data --startup-file Builds/ClockValidation/backend-startup.json
```

In another terminal, with the application running in Play Mode:

```powershell
unity command script eval --file deployment/qa/verify-live-clock.cs --timeout 35 --json
unity command script eval --file deployment/qa/verify-live-clock-analysis.cs --timeout 180 --json
```

Run the first script before the second. Inspect every boolean in the result;
a returned JSON object alone is not a passing test. These scripts are optional
manual checks, separate from the offline pytest suite. Their original recorded
runs are described in the release validation documents.


Direct numeric entry checks: run `verify-editable-slots.cs` (24 validation and
synchronization checks across both forms) and `verify-slot-keyboard.cs` (synthetic
UI Toolkit keyboard events covering delayed typing, Enter, Escape and blur).
Both scripts create and close a temporary Editor window to attach the controls.


Installer cleanup: `verify-installer-cleanup.py --installer <test-setup.exe>
--old-installer <v1.1.0-test-setup.exe> --workspace <new-workspace>` runs real
install/uninstall checks with generated Python caches, owned-folder leftovers,
unrelated root files and separately stored user data. Compile test installers
with registration disabled; v1.1.1 supports `/DIsolatedTest=1`. This disables
registration and compression only; cleanup code and shipped files are identical.
Always use `/NOICONS` as the test harness does. Never point tests at a user install.
