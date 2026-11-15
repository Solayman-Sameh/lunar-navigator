"""Real Windows /sites -> /analyze -> /opportunities handoff; no mocks.

Defaults can be overridden at the command line; JSON responses are retained.
--health-only exercises real localhost /sites without any NASA request.
"""
from pathlib import Path
from datetime import datetime,timezone
import argparse,json,subprocess,sys,socket,os,time,uuid,urllib.request,urllib.error
from science.models import HorizonsRequest,Observer,parse_utc
from science.horizons import parse_response,align_tracks,sha256
from science.visibility import AlignedTracks,mission_metrics
from science.coordinates import Horizon
from science.opportunities import rank_opportunities
from science.models import utc_text
from datetime import timedelta

BACKEND=Path(__file__).resolve().parent


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--landing-utc',default='2027-02-15T06:00:00Z')
    parser.add_argument('--search-utc',default='2026-12-01T00:00:00Z')
    parser.add_argument('--mission-hours',type=float,default=24)
    parser.add_argument('--window-hours',type=float,default=48)
    parser.add_argument('--health-only',action='store_true')
    args=parser.parse_args()
    out=BACKEND/'data/runs'/('finder-live-'+datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%SZ')+'-'+uuid.uuid4().hex[:8])
    out.mkdir(parents=True)
    with socket.socket() as sock:sock.bind(('127.0.0.1',0));port=sock.getsockname()[1]
    base=f'http://127.0.0.1:{port}';summary={'status':'FAIL','NASA_Finder_live':'NOT RUN','health_only':args.health_only}
    print('Evidence directory:',out,flush=True)
    with (out/'uvicorn.log').open('w',encoding='utf-8') as log:
        proc=subprocess.Popen([sys.executable,'-m','uvicorn','science.api:app','--host','127.0.0.1','--port',str(port)],
                cwd=BACKEND,stdout=log,stderr=subprocess.STDOUT,creationflags=subprocess.CREATE_NO_WINDOW if os.name=='nt' else 0)
        try:
            for _ in range(60):
                if proc.poll() is not None:raise RuntimeError('Uvicorn exited; see uvicorn.log')
                try:
                    with urllib.request.urlopen(base+'/health',timeout=2) as r:json.loads(r.read())
                    break
                except urllib.error.URLError:time.sleep(.25)
            else:raise RuntimeError('Server health timeout')
            def call(name,path,payload=None):
                if payload is not None:(out/(name+'-request.json')).write_text(json.dumps(payload,indent=2),encoding='utf-8')
                request=urllib.request.Request(base+path,data=None if payload is None else json.dumps(payload,allow_nan=False).encode(),headers={'Content-Type':'application/json'})
                try:
                    with urllib.request.urlopen(request,timeout=240) as response:raw=response.read()
                except urllib.error.HTTPError as exc:
                    (out/(name+'-error.json')).write_bytes(exc.read());raise
                (out/(name+'-response.json')).write_bytes(raw);return json.loads(raw)
            catalog=call('sites','/sites');sites={s['id']:s for s in catalog['sites']}
            assert sites and all(s['verified'] for s in sites.values())
            if not args.health_only:
                analysis=call('analyze','/analyze',{'site_id':sorted(sites)[0],'landing_utc':args.landing_utc,
                    'mission_duration_h':args.mission_hours,'sampling_step_minutes':10,'observer_height_m':2.0})
                assert analysis['request']['landing_utc']==args.landing_utc
                request={'earliest_landing_utc':args.search_utc,'search_window_h':args.window_hours,
                    'mission_duration_h':args.mission_hours,'candidate_site_ids':sorted(sites),
                    'min_sun_fraction':.3,'min_earth_fraction':.4,'max_dark_h':36.0,'max_comm_gap_h':48.0,
                    'search_step_h':6.0,'ranking_policy':'BEST_OVERALL','sampling_step_minutes':10,'observer_height_m':2.0}
                finder=call('opportunities','/opportunities',request)
                decoded={}
                for s in finder['sources']:
                    site=sites[s['site_id']];tracks={}
                    for body,target in [('sun',10),('earth',399)]:
                        p=s['provenance'][body];raw=(BACKEND/'data/api/cache'/p['raw_cache_file']).read_bytes()
                        assert sha256(raw)==p['raw_sha256']
                        q=HorizonsRequest(target,Observer(site['lat_deg'],site['lon_east_deg'],.002),
                            parse_utc(s['coverage_start_utc']),parse_utc(s['coverage_end_utc']),600)
                        tracks[body]=parse_response(raw,q)
                    paired=align_tracks(tracks['sun'],tracks['earth'])
                    decoded[s['site_id']]=AlignedTracks(tuple(r[0] for r in paired),*(tuple(r[i] for r in paired) for i in range(1,5)),600)
                assert finder['evaluated_count']==finder['arrival_count_per_site']*len(sites)
                assert finder['qualifying_count']==finder['evaluated_count']-finder['rejected_count']
                for c in finder['recommendations']:
                    m=mission_metrics(decoded[c['site_id']],c['landing_utc'],args.mission_hours,Horizon('spherical_dip',2))
                    assert m==c['metrics']
                    assert m['f_sun']>=request['min_sun_fraction'] and m['f_earth']>=request['min_earth_fraction']
                    assert m['dark_max_h']<=request['max_dark_h'] and m['comm_gap_max_h']<=request['max_comm_gap_h']
                assert len(finder['recommendations'])<=3
                replay=[]
                for site_id,data in decoded.items():
                    for offset in range(0,int(args.window_hours*3600),21600):
                        landing=parse_utc(args.search_utc)+timedelta(seconds=offset)
                        metrics=mission_metrics(data,landing,args.mission_hours,Horizon('spherical_dip',2))
                        replay.append({'site_id':site_id,'site_name':sites[site_id]['name'],'landing_utc':utc_text(landing),
                            'mission_end_utc':metrics['end_utc'],'mission_duration_h':args.mission_hours,'metrics':metrics})
                expected=rank_opportunities(replay,**{k:request[k] for k in ['min_sun_fraction','min_earth_fraction','max_dark_h','max_comm_gap_h','ranking_policy']})
                for key,value in expected.items():assert finder[key]==value,(key,'raw-source replay mismatch')
                summary.update(NASA_Finder_live='PASS: source bytes parsed for all sites and returned recommendations replayed',
                    evaluated_count=finder['evaluated_count'],recommendation_count=len(finder['recommendations']),empty_reason=finder['empty_reason'])
            summary['status']='PASS'
        except Exception as exc:summary['error']=f'{type(exc).__name__}: {exc}';print(summary['error'],flush=True)
        finally:
            if proc.poll() is None:
                proc.terminate()
                try:proc.wait(timeout=10)
                except subprocess.TimeoutExpired:proc.kill();proc.wait(timeout=5)
    (out/'proof.json').write_text(json.dumps(summary,indent=2)+'\n',encoding='utf-8')
    print(summary['status'],out/'proof.json')
    return 0 if summary['status']=='PASS' else 4


if __name__=='__main__':raise SystemExit(main())
