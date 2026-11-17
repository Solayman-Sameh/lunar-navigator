"""Named-column, strict UTC Horizons parser and retained request-identity cache.

Only diagnostic retrieval is exposed by the current CLI. Source precision is
retained in raw bytes and binary64 parsed angles; no visibility is calculated.
"""
from dataclasses import asdict
from datetime import datetime, timezone, timedelta
from pathlib import Path
import csv
import hashlib
import json
import math
import re
import time
import urllib.error
import urllib.parse
import urllib.request
import uuid
from .models import EphemerisTrack, TrackRow, utc_text

SUPPORTED_SIGNATURES = {("NASA/JPL Horizons API", "1.2")}
CACHE_FORMAT = 1


class SourceError(ValueError):
    pass


def dump_json(path, value):
    Path(path).write_text(json.dumps(value, indent=2, allow_nan=False) + "\n", encoding="utf-8")


def sha256(raw):
    return hashlib.sha256(raw).hexdigest()


def request_identity(query):
    identity = asdict(query)
    identity.update(start=utc_text(query.start), stop=utc_text(query.stop),
                    cache_format=CACHE_FORMAT, params=query.params())
    return identity


def cache_key(query):
    return sha256(json.dumps(request_identity(query), sort_keys=True, separators=(',', ':'), allow_nan=False).encode())


def _source_date(text):
    # Avoid locale-dependent %b: Horizons uses fixed English month names.
    months = dict(zip('Jan Feb Mar Apr May Jun Jul Aug Sep Oct Nov Dec'.split(), range(1, 13)))
    match = re.fullmatch(r'(\d{4})-([A-Z][a-z]{2})-(\d{2}) (\d{2}):(\d{2})(?::(\d{2})(\.\d+)?)?', text)
    if not match:
        raise SourceError(f"unsupported source timestamp: {text!r}")
    year, month, day, hour, minute, second, fraction = match.groups()
    if fraction and float(fraction) != 0:
        raise SourceError("fractional source timestamp unsupported")
    try:
        return datetime(int(year), months[month], int(day), int(hour), int(minute), int(second or 0), tzinfo=timezone.utc)
    except (ValueError, KeyError) as exc:
        raise SourceError(f"invalid source timestamp: {text!r}") from exc


def validate_rows(rows, query):
    if len(rows) != query.expected_count:
        raise SourceError(f"timestamp count {len(rows)} != {query.expected_count}")
    for i, row in enumerate(rows):
        if row.utc != query.start + timedelta(seconds=i*query.step_s):
            raise SourceError(f"timestamp/cadence mismatch at row {i}")


def parse_response(raw, query):
    try:
        envelope = json.loads(raw)
    except (ValueError, UnicodeError) as exc:
        raise SourceError("invalid JSON source envelope") from exc
    if not isinstance(envelope, dict):
        raise SourceError("source envelope must be an object")
    if 'error' in envelope:
        raise SourceError(f"API error: {envelope['error']}")
    signature = envelope.get('signature', {})
    if not isinstance(signature, dict) or (signature.get('source'), signature.get('version')) not in SUPPORTED_SIGNATURES:
        raise SourceError(f"unrecognized signature: {signature}")
    result = envelope.get('result')
    if (not isinstance(result, str) or result.count('$$SOE') != 1 or result.count('$$EOE') != 1
            or result.find('$$SOE') >= result.find('$$EOE')):
        raise SourceError("expected exactly one SOE/EOE markers pair")
    before, rest = result.split('$$SOE')
    block, after = rest.split('$$EOE')
    metadata = {}
    for line in before.splitlines():
        match = re.match(r'^([A-Za-z][A-Za-z .\-/]*?)\s*:\s*(.*)$', line)
        if match:
            metadata[match[1].strip()] = match[2].strip()
    def require(key):
        if key not in metadata:
            raise SourceError(f"missing source metadata: {key}")
        return metadata[key]
    target = 'Sun' if query.target == 10 else 'Earth'
    if not re.match(rf'{target}\s+\({query.target}\)(?:\s|$)', require('Target body name')):
        raise SourceError("wrong target metadata")
    if not re.match(r'Moon\s+\(301\)(?:\s|$)', require('Center body name')):
        raise SourceError("wrong center metadata")
    if require('Center-site name') != '(user defined site below)':
        raise SourceError("wrong custom observer site")
    if require('Atmos refraction') != 'NO (AIRLESS)':
        raise SourceError("expected airless source")
    if query.observer.frame not in require('Center pole/equ'):
        raise SourceError("unrecognized center frame")
    coords = require('Center geodetic').split('{')[0].strip().split(',')
    expected = (query.observer.lon_east_deg, query.observer.lat_deg, query.observer.altitude_km)
    try:
        actual = tuple(float(a) for a in coords)
        differences = (((actual[0]-expected[0]+180)%360-180),
                       actual[1]-expected[1],actual[2]-expected[2]) if len(actual)==3 else ()
        if len(actual) != 3 or any(not math.isfinite(a) for a in actual) or any(abs(d)>5e-7 for d in differences):
            raise SourceError("source observer coordinates/altitude differ")
    except ValueError as exc:
        raise SourceError("invalid source observer coordinates") from exc
    radii = require('Center radii').split('km')[0].split(',')
    if len(radii) != 3 or any(not math.isfinite(float(r)) or abs(float(r)-1737.4) > 1e-7 for r in radii):
        raise SourceError("source radius differs from nominal observer model")
    for key, value in [('Start time', query.start), ('Stop  time', query.stop)]:
        text = require(key)
        match = re.fullmatch(r'A\.D\.\s+(.+?)\s+UT', text)
        if not match or _source_date(match[1]) != value:
            raise SourceError(f"wrong source timestamp metadata: {key}")
    if require('Step-size') != f'{query.step_s//60} minutes':
        raise SourceError("wrong source step metadata")
    headings = []
    for line in before.splitlines():
        if 'Date__(' in line and ',' in line:
            headings.append(tuple(c.strip() for c in next(csv.reader([line]))))
    if len(headings) != 1:
        raise SourceError("missing/ambiguous CSV heading")
    header = headings[0]
    date_col = next((c for c in header if c in ('Date__(UT)__HR:MN:SS', 'Date__(UT)__HR:MN')), None)
    if not date_col or header.count(date_col) != 1 or header.count('Azimuth_(a-app)') != 1 or header.count('Elevation_(a-app)') != 1:
        raise SourceError(f"unsupported or ambiguous airless/UTC heading: {header}")
    ti = header.index(date_col)
    ai = header.index('Azimuth_(a-app)')
    ei = header.index('Elevation_(a-app)')
    rows = []
    for cells in csv.reader(block.strip().splitlines()):
        if not cells or not any(c.strip() for c in cells):
            continue
        if len(cells) != len(header):
            raise SourceError("CSV row/heading length differs (blank flags must be retained)")
        stamp = _source_date(cells[ti].strip())
        try:
            az, el = float(cells[ai]), float(cells[ei])
        except ValueError as exc:
            raise SourceError("unavailable/invalid angle") from exc
        if not math.isfinite(az) or not math.isfinite(el):
            raise SourceError("nonfinite required angle")
        if not 0 <= az <= 360:
            raise SourceError("azimuth outside 0..360 degrees")
        if not -90 <= el <= 90:
            raise SourceError("elevation outside -90..90 degrees")
        rows.append(TrackRow(stamp, az % 360, el))
    validate_rows(rows, query)
    return EphemerisTrack(query, tuple(rows), header, metadata, sha256(raw))


def merge_rows(chunks):
    rows = []
    for chunk in chunks:
        for row in chunk:
            if rows and row.utc == rows[-1].utc:
                if row != rows[-1]:
                    raise SourceError("conflicting duplicate boundary timestamp")
                continue
            if rows and row.utc < rows[-1].utc:
                raise SourceError("unordered chunk timestamp")
            rows.append(row)
    return tuple(rows)


def align_tracks(sun, earth):
    if sun.request.target != 10 or earth.request.target != 399:
        raise SourceError("wrong paired targets")
    a, b = request_identity(sun.request), request_identity(earth.request)
    a.pop('target'); b.pop('target'); a['params'].pop('COMMAND'); b['params'].pop('COMMAND')
    if a != b or len(sun.rows) != len(earth.rows) or any(s.utc != e.utc for s, e in zip(sun.rows, earth.rows)):
        raise SourceError("pair timestamp/observer identity mismatch; refusing intersection")
    return tuple((s.utc, s.az_deg, s.el_deg, e.az_deg, e.el_deg) for s, e in zip(sun.rows, earth.rows))


def fetch_track(query, cache_dir, run_dir, *, refresh=False, offline=False, opener=None, attempts=2):
    """Retain every HTTP attempt; only parsed/validated responses enter cache.

    opener is dependency injection for offline tests, never a proxy bypass.
    Existing proxy/TLS policy is preserved by urllib's default opener.
    """
    cache_dir, run_dir = Path(cache_dir), Path(run_dir)
    cache_dir.mkdir(parents=True, exist_ok=True)
    run_dir.mkdir(parents=True, exist_ok=True)
    key = cache_key(query)
    slot = cache_dir / key
    manifest_path = slot / 'current.json'
    if offline and refresh:
        raise SourceError("offline and refresh are mutually exclusive")
    if manifest_path.exists() and not refresh:
        try:
            manifest = json.loads(manifest_path.read_text(encoding='utf-8'))
            if not isinstance(manifest, dict):
                raise SourceError('cache manifest must be an object')
        except (OSError, ValueError) as exc:
            raise SourceError(f'invalid cache manifest: {exc}') from exc
        raw_name = manifest.get('raw_file', '')
        if Path(raw_name).name != raw_name or not re.fullmatch(r'[0-9a-f]{64}\.raw\.json', raw_name):
            raise SourceError("unsafe cache raw filename")
        if manifest.get('request') != request_identity(query):
            raise SourceError("cache identity mismatch")
        try:
            raw = (slot/raw_name).read_bytes()
        except OSError as exc:
            raise SourceError(f'cached raw response missing/unreadable: {exc}') from exc
        if sha256(raw) != manifest.get('raw_sha256'):
            raise SourceError("cache checksum mismatch")
        track = parse_response(raw, query)
        dump_json(run_dir/f'{query.target}-cache-hit-{uuid.uuid4().hex}.json',
                  {'status': 'PASS', 'network_request': False, 'cache_key': key, 'raw_file': str(slot/raw_name)})
        return track
    if offline:
        raise SourceError("offline cache missing")
    opener = opener or urllib.request.urlopen
    url = query.endpoint + '?' + urllib.parse.urlencode(query.params())
    last_error = None
    for attempt in range(1, attempts+1):
        stem = f'{query.target}-attempt-{uuid.uuid4().hex}'
        receipt = {'request': request_identity(query), 'request_url': url, 'attempt': attempt,
                   'requested_utc': datetime.now(timezone.utc).isoformat(), 'transport': 'urllib-default-proxy-and-TLS',
                   'http_status': None, 'status': 'FAIL'}
        raw = None
        try:
            req = urllib.request.Request(url, headers={'User-Agent': 'LunarNavigator-science/0.1'})
            with opener(req, timeout=30) as response:
                raw = response.read()
                receipt.update(http_status=response.status, headers=dict(response.headers))
            raw_path = run_dir/(stem+'.raw.json')
            raw_path.write_bytes(raw)
            receipt.update(raw_file=str(raw_path), raw_sha256=sha256(raw))
            if receipt['http_status'] != 200:
                raise SourceError(f"HTTP {receipt['http_status']}")
            track = parse_response(raw, query)
            result_path = run_dir/(stem+'.result.txt')
            result_path.write_text(json.loads(raw)['result'], encoding='utf-8')
            receipt['result_file'] = str(result_path)
            slot.mkdir(parents=True, exist_ok=True)
            name = sha256(raw)+'.raw.json'
            if (slot/name).exists() and sha256((slot/name).read_bytes()) != sha256(raw):
                # Preserve corrupted bytes as evidence, then restore this exact
                # content address from the newly validated genuine response.
                (slot/name).replace(slot/('corrupt-'+uuid.uuid4().hex[:16]+'.bin'))
            if not (slot/name).exists():
                (slot/name).write_bytes(raw)
            entry = {'request': request_identity(query), 'raw_sha256': sha256(raw),
                     'raw_file': name, 'retrieved_utc': receipt['requested_utc']}
            pending = slot/(uuid.uuid4().hex+'.tmp')
            dump_json(pending, entry)
            pending.replace(manifest_path)
            receipt['status'] = 'PASS'
            dump_json(run_dir/(stem+'.json'), receipt)
            return track
        except urllib.error.HTTPError as exc:
            raw = exc.read()
            raw_path = run_dir/(stem+'.raw.json')
            raw_path.write_bytes(raw)
            receipt.update(http_status=exc.code, raw_file=str(raw_path), raw_sha256=sha256(raw), error=str(exc))
            last_error = f'HTTP {exc.code}: {exc}'
            retry = exc.code in (429, 500, 502, 503, 504)
        except (OSError, SourceError) as exc:
            last_error = f'{type(exc).__name__}: {exc}'
            receipt['error'] = last_error
            retry = isinstance(exc, OSError)
        dump_json(run_dir/(stem+'.json'), receipt)
        if not retry:
            break
        if attempt < attempts:
            time.sleep(.25 * attempt)
    raise SourceError(f'NASA acquisition failed: {last_error}')
