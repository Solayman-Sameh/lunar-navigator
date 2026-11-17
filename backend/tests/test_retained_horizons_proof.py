"""Retained REAL NASA proof acquired by the user; no network requests."""
import csv
import json
import math
from pathlib import Path
import pytest
from science.acquisition import diagnostic_queries
from science.horizons import parse_response, align_tracks, request_identity, sha256
from science.models import utc_text

pytestmark = pytest.mark.real_data
RUN = Path(__file__).resolve().parent / 'fixtures/horizons/real-diagnostic'


@pytest.fixture(scope='module')
def real_tracks():
    if not (RUN/'raw/sun.json').is_file() or not (RUN/'raw/earth.json').is_file():
        pytest.skip('retained real proof absent; this is NOT a real-data PASS')
    return {name:parse_response((RUN/'raw'/(name+'.json')).read_bytes(),query)
            for name,query in diagnostic_queries().items()}


def test_real_http_and_original_byte_provenance(real_tracks):
    receipts=[]
    for path in (RUN/'source-proof').glob('*.json'):
        if not path.name.endswith('.raw.json'):
            receipts.append(json.loads(path.read_text(encoding='utf-8')))
    assert len(receipts)==2
    for name,track in real_tracks.items():
        receipt=next(r for r in receipts if r['request']['target']==track.request.target)
        assert receipt['status']=='PASS' and receipt['http_status']==200
        assert receipt['request']==request_identity(track.request)
        original=(RUN/receipt['raw_file']).read_bytes()
        assert original==(RUN/'raw'/(name+'.json')).read_bytes()
        assert sha256(original)==receipt['raw_sha256']==track.raw_sha256
        assert json.loads(original)['result']==(RUN/'raw'/(name+'-result.txt')).read_text(encoding='utf-8')


def test_real_grid_pairing_and_physical_bounds(real_tracks):
    paired=align_tracks(real_tracks['sun'],real_tracks['earth'])
    assert len(paired)==73
    assert utc_text(paired[0][0])=='2026-12-01T00:00:00Z'
    assert utc_text(paired[-1][0])=='2026-12-04T00:00:00Z'
    assert all((b[0]-a[0]).total_seconds()==3600 for a,b in zip(paired,paired[1:]))
    for name,bound in [('sun',2.2),('earth',7.7)]:
        for row in real_tracks[name].rows:
            assert math.isfinite(row.az_deg) and math.isfinite(row.el_deg)
            assert 0<=row.az_deg<360 and abs(row.el_deg)<=bound


def test_real_csv_values_and_output_hashes(real_tracks):
    for name,track in real_tracks.items():
        with (RUN/(name+'.csv')).open(newline='',encoding='utf-8') as f:
            rows=list(csv.DictReader(f))
        assert len(rows)==73
        for saved,source in zip(rows,track.rows):
            assert saved['utc']==utc_text(source.utc)
            assert float(saved['az_deg'])==source.az_deg
            assert float(saved['el_deg'])==source.el_deg
    with (RUN/'combined.csv').open(newline='',encoding='utf-8') as f:
        combined=list(csv.DictReader(f))
    for saved,source in zip(combined,align_tracks(real_tracks['sun'],real_tracks['earth'])):
        assert saved['utc']==utc_text(source[0])
        assert tuple(float(saved[k]) for k in ['sun_az_deg','sun_el_deg','earth_az_deg','earth_el_deg'])==source[1:]
    assert len(combined)==73
    validation=json.loads((RUN/'validation.json').read_text(encoding='utf-8'))
    for output in validation['outputs']:
        assert sha256((RUN/output['path']).read_bytes())==output['sha256']
    assert (RUN/'elevations.png').read_bytes().startswith(b'\x89PNG\r\n\x1a\n')
