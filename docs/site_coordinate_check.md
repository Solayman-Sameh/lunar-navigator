# Scientific point verification — T003 remains incomplete

Read-only asset inspection, 2026-11-15. Matching labels and generic source descriptions do not verify exact observing points, lunar frame realizations or ground heights.

| Asset | ID | Candidate latitude | Candidate east longitude | Asset elevation | Scientific status |
| --- | --- | --- | --- | --- | --- |
| Assets/LunarNavigator/Generated/Site_1.asset | shackleton_rim | -89.6 | 129 | 0 | UNVERIFIED |
| Assets/LunarNavigator/Generated/Site_2.asset | malapert_mountain | -85.9 | 2.9 | 0 | UNVERIFIED |
| Assets/LunarNavigator/Generated/Site_3.asset | connecting_ridge | -89.4 | -137.5 | 0 | UNVERIFIED |

No independently sourced exact production point was adopted in this test-coordinate batch, so no authoritative-coordinate mismatch claim is made. Catalog values remain null and verified=false. The user's diagnostic coordinate (-89.5, 0 east) is separate from every catalog entry and is not a landing-site recommendation. Its query altitude is explicitly 0.002 km above the nominal Horizons 1737.4 km reference sphere, not a verified actual DEM ground elevation.

Tests cover longitude normalization, poles, near-pole warnings, unverified rejection, duplicate IDs and a SYNTHETIC >0.05-degree disagreement. A real point citation/frame/height audit and T002 production compatibility receipt remain outstanding. No asset was changed.


## Source verification completed - 2026-11-15

The earlier UNVERIFIED entries above are historical. The catalogue now uses NASA M2M-30044 (2025), Table A2.1-1 p75 reference centers. These are polygon centers, not certified touchdown locations; actual ground remains unknown. Source: https://ntrs.nasa.gov/citations/20250002845 . Associated PGDA products declare MOON_ME/DE421: https://pgda.gsfc.nasa.gov/data/LOLA_5mpp/ . No DEM processing performed.

| ID | Source latitude | Source east longitude | Existing Unity latitude/longitude | Action |
|---|---:|---:|---|---|
| malapert_mountain | -85.9948 | -0.23578 | (-85.9, 2.9) | Mismatch >0.05 degrees; Gemini must reconcile; no asset edit |
| shackleton_rim | -89.7668 | -171.87 | (-89.6, 129) | Mismatch >0.05 degrees; Gemini must reconcile; no asset edit |
| connecting_ridge | -89.4632 | -137.49 | (-89.4, -137.5) | Mismatch >0.05 degrees; Gemini must reconcile; no asset edit |

Stable malapert_mountain ID now explicitly denotes the sourced Site23 Malapert Massif reference center, not an inferred summit. All clients should load GET /sites for the scientific coordinates.
