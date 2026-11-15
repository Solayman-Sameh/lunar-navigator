"""Real localhost FastAPI -> NASA proof. No test mocks are imported.

Run from the project root with backend/.venv/Scripts/python.exe.
--health-only tests actual Uvicorn startup/local HTTP without contacting NASA.
Default saves genuine December/February responses and validates their provenance.
"""
from datetime import datetime,timezone
from pathlib import Path
import hashlib
import json
import os
import socket
import subprocess
import sys
import time
import urllib.request
import urllib.error
import uuid
from science.models import HorizonsRequest,Observer,parse_utc
from science.horizons import parse_response

BACKEND=Path(__file__).resolve().parent


def main():
    if len(sys.argv)>2 or (len(sys.argv)==2 and sys.argv[1]!='--health-only'):
        raise SystemExit('Only optional argument: --health-only')
    health_only='--health-only' in sys.argv
    out=BACKEND/'data/runs'/('api-live-'+datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%SZ')+'-'+uuid.uuid4().hex[:8])
    out.mkdir(parents=True)
    with socket.socket() as sock:
        sock.bind(('127.0.0.1',0));port=sock.getsockname()[1]
    base=f'http://127.0.0.1:{port}'
    summary={'status':'FAIL','health_only':health_only,'NASA_live_proof':'NOT RUN','base_url':base,'results':[]}
    print('Evidence directory:',out,flush=True)
    with (out/'uvicorn.log').open('w',encoding='utf-8') as log:
        proc=subprocess.Popen([sys.executable,'-m','uvicorn','science.api:app','--host','127.0.0.1','--port',str(port)],
                   cwd=BACKEND,stdout=log,stderr=subprocess.STDOUT,
                   creationflags=subprocess.CREATE_NO_WINDOW if os.name=='nt' else 0)
        try:
            for _ in range(60):
                if proc.poll() is not None: raise RuntimeError('Uvicorn exited; inspect uvicorn.log')
                try:
                    with urllib.request.urlopen(base+'/health',timeout=2) as r: health=json.loads(r.read())
                    break
                except urllib.error.URLError: time.sleep(.25)
            else: raise RuntimeError('Local health endpoint did not become ready')
            (out/'health.json').write_text(json.dumps(health,indent=2),encoding='utf-8')
            if not health_only:
                for name,start,hours in [('december','2026-12-01T00:00:00Z',24),('february','2027-02-15T06:00:00Z',120)]:
                    payload={'latitude_deg':-89.5,'longitude_deg':0.0,'landing_utc':start,
                             'mission_duration_h':hours,'observer_height_m':2.0,'sampling_step_minutes':10}
                    (out/(name+'-request.json')).write_text(json.dumps(payload,indent=2),encoding='utf-8')
                    req=urllib.request.Request(base+'/analyze',data=json.dumps(payload,allow_nan=False).encode(),
                                               headers={'Content-Type':'application/json'})
                    try:
                        with urllib.request.urlopen(req,timeout=180) as r: raw=r.read()
                    except urllib.error.HTTPError as exc:
                        (out/(name+'-error.json')).write_bytes(exc.read());raise
                    (out/(name+'-response.json')).write_bytes(raw)
                    result=json.loads(raw)
                    assert result['request']['landing_utc']==start
                    assert result['metrics']['duration_h']==hours
                    assert result['ephemeris']['n']==hours*6+1
                    assert result['ephemeris']['utc'][0]==start
                    assert result['ephemeris']['utc'][-1]==result['request']['mission_end_utc']
                    for body,target in [('sun',10),('earth',399)]:
                        p=result['provenance'][body]
                        assert p['source_start_utc']==start and p['source_end_utc']==result['request']['mission_end_utc']
                        retained=BACKEND/'data/api/cache'/p['raw_cache_file']
                        body_bytes=retained.read_bytes()
                        assert hashlib.sha256(body_bytes).hexdigest()==p['raw_sha256']
                        query=HorizonsRequest(target,Observer(-89.5,0,.002,coordinate_status=result['site']['coordinate_status']),
                            parse_utc(start),parse_utc(result['request']['mission_end_utc']),600)
                        track=parse_response(body_bytes,query)
                        assert len(track.rows)==result['ephemeris']['n']
                    summary['results'].append({'name':name,'request':result['request'],
                       'metrics':result['metrics'],'n':result['ephemeris']['n'],'provenance':result['provenance']})
                a,b=summary['results']
                assert a['provenance']['sun']['cache_key']!=b['provenance']['sun']['cache_key']
                assert a['provenance']['sun']['raw_sha256']!=b['provenance']['sun']['raw_sha256']
                summary['NASA_live_proof']='PASS: distinct December and February source windows parsed from retained original bytes'
            summary['status']='PASS'
        except Exception as exc:
            summary['error']=f'{type(exc).__name__}: {exc}'
            print(summary['error'],flush=True)
        finally:
            # Stop only the server process started by this script.
            if proc.poll() is None:
                proc.terminate()
                try: proc.wait(timeout=10)
                except subprocess.TimeoutExpired: proc.kill();proc.wait(timeout=5)
    (out/'comparison.json').write_text(json.dumps(summary,indent=2,allow_nan=False)+'\n',encoding='utf-8')
    print(summary['status'],out/'comparison.json')
    return 0 if summary['status']=='PASS' else 4


if __name__=='__main__':raise SystemExit(main())
