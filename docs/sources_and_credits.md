# Scientific sources and current acquisition limits

This file records sources actually inspected during the first diagnostic implementation batch. No complete retained Sun/Earth dataset or terrain result is certified yet.

- NASA/JPL Solar System Dynamics Horizons API documentation: https://ssd-api.jpl.nasa.gov/doc/horizons.html — inspected observer GET endpoint/options, CSV, UT/time offset, airless quantity selection, envelope/error/signature rules. Current document lists version 1.3.
- JPL Horizons manual: https://ssd.jpl.nasa.gov/horizons/manual.html — inspected custom lunar observing coordinates, local apparent airless azimuth/elevation and modern UT/UTC definitions.
- Actual requested endpoint: https://ssd.jpl.nasa.gov/api/horizons.api — Requests and urllib local attempts failed before any HTTP response. One genuine Sun response was displayed in the in-app browser (DE441, MEAN_ME, API signature 1.2); original bytes were not retained, and no Earth response was acquired. This observation is not a reproducible data cache or a NASA-endorsed landing-site recommendation.

All numerical parser fixtures under backend/tests are explicitly SYNTHETIC. Metadata spellings/headings are based on the observed Sun response and are recorded separately in backend/tests/fixtures/horizons/README.md. No synthetic fixture is published as scientific exchange data. See backend/data/runs/horizons-proof-20261115/report.md for exact failed request evidence and next gates.
