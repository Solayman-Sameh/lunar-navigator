from dataclasses import replace
import json
import io
import urllib.error
from datetime import timedelta
import pytest
from science.horizons import fetch_track, cache_key, SourceError


class FakeResponse:
    status = 200
    headers = {'Content-Type': 'application/json'}
    def __init__(self, body): self.body = body
    def read(self): return self.body
    def __enter__(self): return self
    def __exit__(self, *args): return False


def test_offline_cache_never_requests(query, synthetic_envelope, workdir):
    calls = []
    def opener(*args, **kwargs):
        calls.append(1)
        return FakeResponse(synthetic_envelope())
    first = fetch_track(query, workdir/'cache', workdir/'run', opener=opener)
    again = fetch_track(query, workdir/'cache', workdir/'run', offline=True, opener=opener)
    assert first.rows == again.rows
    assert calls == [1]
    fetch_track(query, workdir/'cache', workdir/'run', refresh=True, opener=opener)
    assert calls == [1, 1]


@pytest.mark.parametrize("field", ['height', 'coordinate', 'frame', 'target', 'step', 'algorithm', 'start', 'stop'])
def test_identity_changes(query, field):
    changes = {
        'height': replace(query, observer=replace(query.observer, altitude_km=.003)),
        'coordinate': replace(query, observer=replace(query.observer, lon_east_deg=1)),
        'frame': replace(query, observer=replace(query.observer, frame='DIFFERENT')),
        'target': replace(query, target=399), 'step': replace(query, step_s=600),
        'algorithm': replace(query, algorithm_version='changed'),
        'start': replace(query, start=query.start-timedelta(hours=1)),
        'stop': replace(query, stop=query.stop+timedelta(hours=1)),
    }
    assert cache_key(query) != cache_key(changes[field])


def test_corrupt_cache_rejects(query, synthetic_envelope, workdir):
    fetch_track(query, workdir/'cache', workdir/'run', opener=lambda *a, **k: FakeResponse(synthetic_envelope()))
    raw = next((workdir/'cache').rglob('*.raw.json'))
    raw.write_bytes(b'corrupt')
    with pytest.raises(SourceError, match='checksum'):
        fetch_track(query, workdir/'cache', workdir/'run', offline=True)


def test_offline_missing_cache_rejects(query, workdir):
    with pytest.raises(SourceError, match='offline'):
        fetch_track(query, workdir/'cache', workdir/'run', offline=True)


def test_http_error_body_retained_and_not_cached(query, workdir):
    def opener(*args, **kwargs):
        raise urllib.error.HTTPError('https://ssd.jpl.nasa.gov/api/horizons.api', 400, 'bad query', {}, io.BytesIO(b'API error test fixture'))
    with pytest.raises(SourceError, match='HTTP 400'):
        fetch_track(query, workdir/'cache', workdir/'run', opener=opener)
    assert next((workdir/'run').glob('*.raw.json')).read_bytes() == b'API error test fixture'
    assert not list((workdir/'cache').rglob('current.json'))


def test_network_failure_bounded_and_recorded(query, workdir):
    calls=[]
    def opener(*args, **kwargs):
        calls.append(1)
        raise urllib.error.URLError('synthetic connection failure')
    with pytest.raises(SourceError, match='synthetic connection failure'):
        fetch_track(query, workdir/'cache', workdir/'run', opener=opener, attempts=2)
    assert len(calls)==2
    receipts=[json.loads(p.read_text()) for p in (workdir/'run').glob('*.json')]
    assert len(receipts)==2 and all(r['http_status'] is None and r['status']=='FAIL' for r in receipts)


def test_bad_cache_manifest_rejected(query, synthetic_envelope, workdir):
    fetch_track(query, workdir/'cache', workdir/'run', opener=lambda *a, **k: FakeResponse(synthetic_envelope()))
    manifest=next((workdir/'cache').rglob('current.json'))
    manifest.write_text('[]')
    with pytest.raises(SourceError, match='manifest'):
        fetch_track(query, workdir/'cache', workdir/'run', offline=True)


def test_http_200_api_error_is_retained_not_cached(query, workdir):
    raw=b'{"error":"SYNTHETIC rejected query"}'
    with pytest.raises(SourceError, match='API error'):
        fetch_track(query, workdir/'cache', workdir/'run', opener=lambda *a, **k: FakeResponse(raw))
    assert next((workdir/'run').glob('*.raw.json')).read_bytes()==raw
    assert not list((workdir/'cache').rglob('current.json'))
