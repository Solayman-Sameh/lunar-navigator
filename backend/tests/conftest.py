"""Synthetic, in-memory source fixtures; none is a NASA observation."""
from datetime import datetime, timezone
from pathlib import Path
import json
import shutil
import uuid
import pytest


@pytest.fixture
def workdir():
    root = Path(__file__).resolve().parent / ".work"
    path = root / uuid.uuid4().hex
    path.mkdir(parents=True)
    yield path
    # Verify the absolute deletion target stays inside the task-owned test root.
    assert path.resolve().parent == root.resolve()
    shutil.rmtree(path)


@pytest.fixture
def query():
    from science.models import HorizonsRequest, Observer
    return HorizonsRequest(10, Observer(-89.5, 0.0, 0.002),
                           datetime(2026, 12, 1, tzinfo=timezone.utc),
                           datetime(2026, 12, 1, 2, tzinfo=timezone.utc), 3600)


@pytest.fixture
def synthetic_envelope():
    def make(target=10):
        name = "Sun" if target == 10 else "Earth"
        # Heading and metadata spellings observed in the browser; numerical rows
        # below are deliberately SYNTHETIC parser decoys, never real ephemerides.
        result = f"""SYNTHETIC TEST FIXTURE -- NOT NASA DATA
Target body name: {name} ({target}) {{source: SYNTHETIC}}
Center body name: Moon (301) {{source: SYNTHETIC}}
Center-site name: (user defined site below)
Start time      : A.D. 2026-Dec-01 00:00:00.0000 UT
Stop  time      : A.D. 2026-Dec-01 02:00:00.0000 UT
Step-size       : 60 minutes
Center geodetic : 0.0, -89.5, .002 {{E-lon(deg),Lat(deg),Alt(km)}}
Center pole/equ : MEAN_ME (high precision) {{East-longitude positive}}
Center radii    : 1737.4, 1737.4, 1737.4 km
Atmos refraction: NO (AIRLESS)
 Date__(UT)__HR:MN:SS, , ,decoy,Elevation_(a-app),Azimuth_(a-app),
***********************************
$$SOE
 2026-Dec-01 00:00:00,*, ,9999,1.125,359.9,
 2026-Dec-01 01:00:00, ,i,9999,0.0,360.0,
 2026-Dec-01 02:00:00, , ,9999,-1.25,0.1,
$$EOE
"""
        return json.dumps({"signature": {"version": "1.2", "source": "NASA/JPL Horizons API"},
                           "result": result}).encode()
    return make
