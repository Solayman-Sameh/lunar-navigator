# Parser fixtures and source observation

Numerical fixtures in `tests/conftest.py` are SYNTHETIC, deliberately reordered and include a numeric decoy. They are not NASA ephemerides or a real-response cache.

Before the parser was implemented, a real Sun response was observed via the in-app browser for `coord@301`, longitude 0 east, latitude -89.5, altitude 0.002 km, 2026-12-01 through 2026-12-04, hourly. Its exact visible heading was:

```text
 Date__(UT)__HR:MN:SS, , ,Azimuth_(a-app), Elevation_(a-app),
```

The response identified Sun (10), Moon (301), DE441, `MEAN_ME (high precision)`, center radii 1737.4 km, `NO (AIRLESS)` and API signature version 1.2. The current API documentation inspected on the same day lists 1.3. This implementation explicitly recognizes only the observed 1.2 profile and rejects unreviewed versions. Do not relabel another version as 1.2.

The browser displayed 73 Sun rows, but the original response bytes were not saved: browser automation stopped during the subsequent download preparation because it could not verify the Windows browser URL. This observation establishes the header spelling only; it is not a retained-real-data test fixture. No Earth response was observed. All local Python HTTP attempts failed before receiving an HTTP response. A future successful acquisition must retain both full envelopes/result text and add retained-real-data parser tests before T006 is certified.
