# Changelog

## v1.1.0 — prepared 2026-11-17

### Fixed

- Timeline starts from device UTC at 1x rather than the previous 3600x default.
  Date controls include seconds; a separate live UTC clock continues while paused.
  Analysis preserves the selected start, playhead and playback state. Editing the
  window or selecting a site pauses; Use current UTC resumes 1x. Reaching coverage
  end pauses without wrapping.
- Opportunity Finder serializes Gregorian UTC dates independently of the Windows
  regional calendar, preserving seconds in requests and arrival displays.

- Surface clicks select the clicked coordinate instead of snapping to nearby landmarks.
- WASD movement, pointer dragging and UI interaction coexist more consistently.
- Mission results are invalidated when their location or configuration changes.
- Horizons parsing accepts the supported UTC minute/second heading formats.
- Packaged backend storage/catalog resolution supports the installed layout.

### Improved

- Refined Mission Assessment panel, layout, input feedback and scientific scope labels.
- Windows setup with installation-folder choice and optional desktop shortcut.
- Private Python runtime and backend lifecycle managed by the Windows launcher.
- Branded launcher, player, shortcuts, setup, uninstaller, splash, header and About panel.
- Current README, proposal, attribution and release instructions.
- Curated source tree; local agent tooling, plans, development captures and reports excluded.
- Portable retained NASA regression fixture, independent of developer-machine paths.

### Distribution

- Supported Windows download: `LunarNavigator_Setup.exe` from GitHub Releases.
- Publish its matching `.sha256` alongside it. Retire the old ZIP as the recommended download.
- Narrated demo video is planned; the local zero-byte placeholder is not published.

### Scientific scope

Calculations remain GEOMETRIC_ONLY. Local terrain is illustrative; no terrain-aware
landing safety, solar-power model or RF link budget is claimed.
