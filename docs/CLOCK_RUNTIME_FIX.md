# Clock runtime repair - 17 November 2026

Packaging has since completed. See [CLOCK_PACKAGING_VALIDATION.md](CLOCK_PACKAGING_VALIDATION.md)
for the replacement installer, checksum and native EXE checks. The account below
records the earlier repair phase.

This records the source and Unity Editor repair. The existing Windows player,
installer and installer SHA-256 file have not been rebuilt or replaced by this
repair. Nothing was pushed or uploaded.

## Cause and behavior

The earlier timeline repair deliberately paused startup. Successful analysis
also reset the playhead to zero and paused it. Reapplying the same mission window
could reset the clock again. The visible date selectors omitted seconds.

- Startup now uses device UTC with second precision and runs at 1x.
- Both mission and opportunity-search selectors expose seconds.
- LIVE UTC in the header follows the device clock, including while paused.
- The landing date remains the fixed analysis start. The bottom timeline is the
  advancing mission UTC and explicitly shows PLAYING or PAUSED.
- Analysis completion preserves the playhead and playback state. Reapplying the
  same window and rebuilding the UI preserve playback too.
- Changing a mission window or selecting a site pauses playback for review.
- Use current UTC chooses now and resumes 1x. Coverage end pauses without wrapping.
- Finder dates use invariant Gregorian UTC. A reproduced ar-SA formatting bug
  displayed year 1448 for a 2026 selection; requests now retain the intended year
  and seconds independently of the regional calendar.

## Verification

The final source compiled with zero compile errors. In the actual application in
Unity Play Mode, all 29 clock/date checks passed. These include live labels,
pause/resume, fixed analysis input, UI rebuilding, coverage end, second/year
rollover, and exact request dates under en-US, en-GB, ar-EG and ar-SA.

Measured 1x playback: 5.2073522 real seconds / 5.2123361 mission seconds.
The executable check and result are `Builds/verify-live-clock.cs` and
`Builds/ClockValidation/regression.json`.

The final real scientific-analysis check passed all six assertions. The actual
Analyze button action sent `2026-11-17T18:09:35Z` for a 72-hour mission, obtained
NASA Horizons data, and displayed Analysis ready. The input and response origin
matched; playback stayed running at 1x. During the request/observation interval,
11.7261612 real seconds advanced the mission 11.7259385 seconds. See
`Builds/ClockValidation/live-analysis.json` and retained request/response/raw NASA
receipts under `Builds/ClockValidation/data/runs/`.

An earlier test-process connection failure also preserved the selected date and
continued playback. Its record is `Builds/ClockValidation/analysis-network-failure.json`.
No backend source changes were required.

Visual comparison after a successful analysis showed 18:04:59 UTC, then 18:05:32
UTC, with PLAYING and 1x visible and the analysis start unchanged at 18:04:42.
Captures: `Screenshots/QA/ClockFix/analysis-complete-1.png` and
`Screenshots/QA/ClockFix/analysis-complete-2.png`.

Unity is left in Play Mode with the development backend on loopback port 8000.
Packaging and installed-player acceptance remain pending; the old setup executable
does not contain this repair.
