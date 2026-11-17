# Diagnostic NASA acquisition

The application normally obtains NASA data through the scientific API. The
standalone acquisition utility is for source inspection and diagnostics; it is
not required for installing or launching the Windows application.

From the repository root, after creating the backend development environment:

```powershell
.\backend\.venv\Scripts\python.exe .\backend\acquire_horizons.py
```

Each execution creates a uniquely named local run under `backend/data/runs/` and
retains the full source responses and validation outcomes. The diagnostic point
is (-89.5 degrees latitude, 0 degrees east), 2 m above the nominal sphere; it is
not a verified landing candidate. Default coverage is December 1–4, 2026 UTC,
hourly, with 73 endpoint-inclusive Sun and Earth instants.

Use `--help` for supported import/acquisition options. Browser-saved imports must
retain complete original JSON envelopes; an HTML error page, edited table or
partial response is not valid NASA evidence. Keep genuine source bytes unchanged,
including source headers and their conventions. Failures remain failures and do
not justify substituting synthetic observations.

Generated run directories and handoff inbox files are local outputs and are
ignored by Git. The small retained genuine regression pair is maintained separately
in `tests/fixtures/horizons/real-diagnostic/`. See its README and the scientific
reference for what it proves.

- [Horizons API documentation](https://ssd-api.jpl.nasa.gov/doc/horizons.html)
- [Horizons conventions](https://ssd.jpl.nasa.gov/horizons/manual.html)
- [Scientific reference](../docs/SCIENCE_BACKEND_REFERENCE.md)
