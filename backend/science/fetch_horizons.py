"""Execute the explicitly authorized diagnostic source proof; no Unity output.

From backend:
python -m science.fetch_horizons --test-coordinate --lat -89.5 --lon 0 \
  --start 2026-12-01T00:00:00Z --stop 2026-12-04T00:00:00Z --step-minutes 60
"""
import argparse
from dataclasses import asdict
from datetime import datetime, timezone
from pathlib import Path
import sys
from .models import Observer, HorizonsRequest, parse_utc
from .horizons import fetch_track, SourceError, dump_json
from .evidence import write_pair


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--test-coordinate', action='store_true', required=True,
                        help='acknowledge this point is unverified and not a landing-site recommendation')
    parser.add_argument('--lat', type=float, required=True)
    parser.add_argument('--lon', type=float, required=True, help='east longitude, -180..180 degrees')
    parser.add_argument('--altitude-km', type=float, default=.002,
                        help='nominal altitude above Horizons 1737.4 km sphere, NOT actual DEM ground')
    parser.add_argument('--start', required=True)
    parser.add_argument('--stop', required=True)
    parser.add_argument('--step-minutes', type=int, default=10)
    parser.add_argument('--out', type=Path)
    parser.add_argument('--cache-dir', type=Path, default=Path('data/cache/horizons'))
    parser.add_argument('--refresh', action='store_true')
    parser.add_argument('--offline', action='store_true')
    args = parser.parse_args(argv)
    backend = Path(__file__).resolve().parents[1]
    out = (args.out or backend/'data/runs'/('horizons-'+datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%SZ'))).resolve()
    cache = args.cache_dir.resolve()
    if not out.is_relative_to(backend) or not cache.is_relative_to(backend):
        parser.error('all diagnostic output/cache paths must stay under backend')
    try:
        observer = Observer(args.lat, args.lon, args.altitude_km)
        queries = {name: HorizonsRequest(target, observer, parse_utc(args.start), parse_utc(args.stop), args.step_minutes*60)
                   for name, target in [('sun',10),('earth',399)]}
    except ValueError as exc:
        parser.error(str(exc))
    out.mkdir(parents=True, exist_ok=True)
    if (out/'summary.json').exists():
        parser.error('output run already has summary.json; choose a new run to preserve prior evidence')
    summary = {'status': 'FAIL', 'coordinate_status': observer.coordinate_status,
               'observer': asdict(observer), 'requested_samples_per_target': queries['sun'].expected_count,
               'source': 'NASA/JPL Horizons; apparent airless target-center directions',
               'time_assumptions': 'UT is UTC for these modern dates; source uses latest known leap correction for future dates; no leap insertion inside this proof interval',
               'transport': 'Python standard-library urllib; default proxy and TLS policy unchanged',
               'targets': {}, 'scientific_exchange_published': False}
    tracks = {}
    for name, query in queries.items():
        try:
            track = fetch_track(query, cache, out/'source-proof', refresh=args.refresh, offline=args.offline)
            tracks[name] = track
            summary['targets'][name] = {'status':'PASS', 'samples':len(track.rows), 'raw_sha256':track.raw_sha256,
                                        'observed_header': track.header, 'metadata':track.metadata}
            print(f'{name}: validated {len(track.rows)} samples')
        except SourceError as exc:
            summary['targets'][name] = {'status':'FAIL', 'samples':0, 'error':str(exc)}
            print(f'{name}: {exc}', file=sys.stderr)
    if len(tracks) == 2:
        try:
            summary['outputs'] = write_pair(tracks['sun'], tracks['earth'], out)
            summary['status'] = 'PASS'
        except (SourceError, OSError, ImportError) as exc:
            summary['output_error'] = f'{type(exc).__name__}: {exc}'
    dump_json(out/'summary.json', summary)
    print(f"{summary['status']}: {out/'summary.json'}")
    return 0 if summary['status']=='PASS' else 4


if __name__ == '__main__':
    raise SystemExit(main())
