"""Local Windows acquisition/import handoff using the existing Horizons client.

No proxy or TLS settings are changed. Imported original bytes are preserved
before validation, including rejected responses. No Unity exchange is written.
"""
from datetime import datetime, timezone
from pathlib import Path
import html
import json
import math
import urllib.parse
import uuid

from .models import HorizonsRequest, Observer, parse_utc, utc_text
from .horizons import (fetch_track, parse_response, align_tracks, request_identity,
                       sha256, dump_json, SourceError)
from .evidence import write_pair

BACKEND = Path(__file__).resolve().parents[1]
PROJECT = BACKEND.parent
INBOX = BACKEND / 'data' / 'horizons_inbox'


def diagnostic_queries():
    observer = Observer(-89.5, 0.0, .002)
    return {name: HorizonsRequest(target, observer,
                parse_utc('2026-12-01T00:00:00Z'), parse_utc('2026-12-04T00:00:00Z'), 3600)
            for name, target in [('sun', 10), ('earth', 399)]}


def request_url(query):
    return query.endpoint + '?' + urllib.parse.urlencode(query.params())


def new_run(parent=None):
    parent = Path(parent) if parent is not None else BACKEND / 'data' / 'runs'
    path = parent / ('local-horizons-' + datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%SZ') + '-' + uuid.uuid4().hex[:8])
    path.mkdir(parents=True, exist_ok=False)
    return path


def prepare_handoff(out, queries=None, inbox=None):
    out = Path(out)
    queries = queries or diagnostic_queries()
    inbox = Path(inbox) if inbox is not None else INBOX
    inbox.mkdir(parents=True, exist_ok=True)
    urls = {name: request_url(query) for name, query in queries.items()}
    for name, query in queries.items():
        dump_json(out / (name+'-request.json'), {'request': request_identity(query), 'url': urls[name]})
        # Never alter browser-saved sun.json/earth.json or other source files.
        (inbox / (name+'-url.txt')).write_text(urls[name]+'\n', encoding='utf-8')
    dump_json(out/'request-urls.json', urls)
    links = ''.join(f'<li><a href="{html.escape(url, quote=True)}">Open {name.title()} NASA JSON</a> — save the complete response as <code>{name}.json</code></li>' for name, url in urls.items())
    page = ('<!doctype html><meta charset="utf-8"><title>NASA Horizons response downloads</title>'
            '<h1>NASA Horizons: retain both original JSON responses</h1><p>Diagnostic point only: '
            'latitude -89.5, longitude 0 east, nominal altitude 2 m above source sphere. '
            'December 1–4, 2026 UTC, hourly; 73 expected instants per body.</p><ol>'+links+'</ol>'
            '<p>Open each link in your normal browser. Save the entire original JSON document '
            '(Ctrl+S), not an HTML viewer, screenshot or selected rows. Save into <code>'
            +html.escape(str(inbox))+'</code>. The document must include signature and the full '
            'result string with $$SOE and $$EOE. No field edits or signature changes.</p>'
            '<p>Then run the import command in MANUAL_ACQUISITION.md. The importer preserves '
            'your original bytes, validates both responses and generates CSV/plot outputs.</p>')
    (inbox/'open-nasa-urls.html').write_text(page, encoding='utf-8')
    return urls


def _copy_import(name, source, out, query):
    """Copy exact bytes before parsing; unknown signatures/HTML remain evidence."""
    source = Path(source).resolve()
    raw = source.read_bytes()
    destination = out/'raw'/(name+'.json')
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_bytes(raw)
    receipt = {'mode':'saved-response-import', 'source_path':str(source),
               'retained_path':str(destination.resolve()), 'sha256':sha256(raw),
               'bytes':len(raw), 'imported_utc':datetime.now(timezone.utc).isoformat(),
               'http_status':None, 'network_request_performed':False,
               'expected_request':request_identity(query), 'expected_url':request_url(query),
               'provenance_limit':'User-saved response; source metadata is validated, not independently refetched.'}
    dump_json(out/(name+'-import-receipt.json'), receipt)
    return raw


def _retain_download(name, query, out):
    paths = list((out/'source-proof').glob(f'{query.target}-attempt-*.raw.json'))
    if paths:
        # Downloader calls the tested client with exactly one attempt per body.
        if len(paths) != 1:
            raise SourceError('unexpected multiple raw attempts in a new acquisition run')
        raw = paths[0].read_bytes()
        destination = out/'raw'/(name+'.json')
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_bytes(raw)
        return raw
    return None


def _target_details(track):
    return {'status':'PASS', 'samples':len(track.rows), 'raw_sha256':track.raw_sha256,
            'start_utc':utc_text(track.rows[0].utc), 'stop_utc':utc_text(track.rows[-1].utc),
            'step_s':track.request.step_s, 'observed_header':list(track.header),
            'metadata':track.metadata,
            'azimuth_min_deg':min(r.az_deg for r in track.rows),
            'azimuth_max_deg':max(r.az_deg for r in track.rows),
            'elevation_min_deg':min(r.el_deg for r in track.rows),
            'elevation_max_deg':max(r.el_deg for r in track.rows)}


def process_pair(out, *, saved_files=None, queries=None, cache_dir=None):
    """Download OR import; imports never issue a network request.

    Only a fully parsed, paired, finite dataset is sent to the existing writer.
    A PASS here is automated acquisition validation; visual inspection and task
    completion remain separate review actions, never auto-asserted by this script.
    """
    out = Path(out).resolve()
    queries = queries or diagnostic_queries()
    if (out/'validation.json').exists():
        raise ValueError('Refusing to overwrite an earlier validation run')
    result = {'status':'FAIL', 'mode':'saved-response-import' if saved_files is not None else 'live-download',
              'coordinate_status':'USER_SUPPLIED_TEST_POINT_NOT_VERIFIED_LANDING_SITE',
              'targets':{}, 'outputs':[], 'plot_visual_inspection':'NOT RUN; inspect the generated image',
              'scientific_exchange_published':False,
              'time_assumption':'Modern Horizons UT is UTC; nominal reference-sphere observer, no verified DEM ground. Source future leap/EOP assumptions retained in headers.'}
    tracks = {}
    for name, query in queries.items():
        try:
            if saved_files is not None:
                raw = _copy_import(name, saved_files[name], out, query)
                track = parse_response(raw, query)
            else:
                try:
                    track = fetch_track(query, cache_dir or BACKEND/'data/cache/horizons',
                                        out/'source-proof', refresh=True, attempts=1)
                finally:
                    _retain_download(name, query, out)
                raw = (out/'raw'/(name+'.json')).read_bytes()
            # Extra handoff checks operate on the actual parsed source values.
            for row in track.rows:
                if not all(math.isfinite(v) for v in (row.az_deg, row.el_deg)):
                    raise SourceError('nonfinite parsed value')
                if not 0 <= row.az_deg < 360 or not -90 <= row.el_deg <= 90:
                    raise SourceError('parsed angle out of range')
            (out/'raw'/(name+'-result.txt')).write_text(json.loads(raw)['result'], encoding='utf-8')
            tracks[name] = track
            result['targets'][name] = _target_details(track)
        except (SourceError, OSError, KeyError, ValueError) as exc:
            result['targets'][name] = {'status':'FAIL','samples':0,'error':f'{type(exc).__name__}: {exc}'}
    if len(tracks) == 2:
        try:
            paired = align_tracks(tracks['sun'], tracks['earth'])
            result['paired_samples'] = len(paired)
            # Approved physical bounds are a sanity check, not a measured error budget.
            for name, extra in [('sun',1.7),('earth',7.2)]:
                bound = 90-abs(queries[name].observer.lat_deg)+extra
                if any(abs(row.el_deg) > bound for row in tracks[name].rows):
                    raise SourceError(f'{name} elevation exceeds the approved polar sanity bound {bound} deg')
            write_pair(tracks['sun'], tracks['earth'], out)
            for name in ('sun.csv','earth.csv','combined.csv','elevations.png'):
                path=out/name
                if not path.is_file() or not path.stat().st_size:
                    raise SourceError(f'missing/empty output: {path}')
                result['outputs'].append({'path':str(path),'sha256':sha256(path.read_bytes())})
            for name, track in tracks.items():
                if sha256((out/'raw'/(name+'.json')).read_bytes()) != track.raw_sha256:
                    raise SourceError('retained original response checksum mismatch')
            result['checks'] = {'signature_and_source_metadata':'PASS', 'observer_and_request_match':'PASS',
                                'utc_grid_endpoints_count':'PASS', 'sun_earth_pairing':'PASS',
                                'finite_angles_and_ranges':'PASS', 'polar_physical_bounds':'PASS',
                                'raw_byte_hashes':'PASS', 'csv_and_plot_created':'PASS'}
            result['status']='PASS'
        except (SourceError, OSError, ValueError, ImportError) as exc:
            result['validation_error']=f'{type(exc).__name__}: {exc}'
    dump_json(out/'validation.json',result)
    return result
