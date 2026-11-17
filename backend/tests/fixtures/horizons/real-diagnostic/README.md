# Genuine retained NASA diagnostic fixture

NASA/JPL Horizons responses for Sun (10) and Earth (399), acquired on
2026-11-15 for the unverified lunar diagnostic coordinate latitude -89.5 degrees,
longitude 0 degrees east, nominal sphere observer height 2 m. Coverage is
2026-12-01 00:00 through 2026-12-04 00:00 UTC, with 73 hourly instants per target.

Raw JSON and extracted result text are byte-preserving copies of the original
local acquisition. `source-proof/` preserves request, retrieval and HTTP metadata
and the original raw hashes. Only receipt file paths were changed to relative
fixture paths. `validation.json` retains the original derived-output hashes with
relative paths; local acquisition logs and machine paths are omitted.

The CSVs and plot are retained diagnostic outputs. Tests reparse the raw pair,
verify integrity, compare rows and compute the 24/72-hour geometric mission oracle.
This is a fixed historical regression input, not current predictions or a runtime
offline database. Synthetic parser fixtures elsewhere are explicitly labeled.

Source: https://ssd.jpl.nasa.gov/api/horizons.api
Conventions: https://ssd.jpl.nasa.gov/horizons/manual.html
