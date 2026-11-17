"""Dynamic ONLINE-only API. Production has no fake providers or CSV lookups."""
from datetime import timedelta,datetime,timezone
from pathlib import Path
import json
import threading
import uuid
from fastapi import FastAPI,HTTPException
from fastapi.exceptions import RequestValidationError
from fastapi.responses import JSONResponse
from pydantic import BaseModel,ConfigDict,Field,model_validator
from .models import Observer,HorizonsRequest,parse_utc,utc_text
from .horizons import fetch_track,align_tracks,cache_key,request_identity,dump_json,SourceError
from .visibility import AlignedTracks,visibility_series,mission_metrics
from .coordinates import Horizon
from .sites import load_catalog, get_sites_json_path

BACKEND=Path(__file__).resolve().parents[1]
MIN_DATE=parse_utc('2018-01-01T00:00:00Z')
MAX_DATE=parse_utc('2030-01-01T00:00:00Z')


class AnalyzeRequest(BaseModel):
    model_config=ConfigDict(extra='forbid',allow_inf_nan=False)
    site_id: str|None=Field(default=None,min_length=1,max_length=64)
    latitude_deg: float|None=Field(default=None,gt=-90,lt=90,strict=True)
    longitude_deg: float|None=Field(default=None,ge=-180,le=180,strict=True)
    landing_utc: str
    mission_duration_h: float=Field(gt=0,le=744,strict=True)
    observer_height_m: float=Field(default=2.0,ge=0,le=1000,strict=True)
    sampling_step_minutes: int=Field(default=10,ge=1,le=60,strict=True)

    @model_validator(mode='after')
    def validate_mission(self):
        coords=self.latitude_deg is not None and self.longitude_deg is not None
        if self.site_id is not None:
            if self.latitude_deg is not None or self.longitude_deg is not None:
                raise ValueError('provide site_id OR both coordinates, not both')
        elif not coords: raise ValueError('both latitude_deg and longitude_deg required')
        start=parse_utc(self.landing_utc)
        seconds=self.mission_duration_h*3600
        step=self.sampling_step_minutes*60
        if not seconds.is_integer() or seconds%step:
            raise ValueError('duration must be an exact positive whole number of sampling cells; no rounding')
        stop=start+timedelta(seconds=seconds)
        if start<MIN_DATE or stop>MAX_DATE:
            raise ValueError('entire mission must fit service coverage 2018-01-01 through 2030-01-01 UTC')
        if int(seconds/step)+1>10001: raise ValueError('request exceeds 10001 instants per target')
        return self


def coordinates(body):
    if body.site_id is None:
        return body.latitude_deg,body.longitude_deg,'CUSTOM_UNVERIFIED_DIAGNOSTIC_POINT'
    catalog=load_catalog(json.loads(get_sites_json_path().read_text(encoding='utf-8')))
    if body.site_id not in catalog:
        raise HTTPException(422,detail={'code':'unknown_site','message':'Unknown catalog site_id'})
    try: site=catalog[body.site_id].require_verified()
    except ValueError:
        raise HTTPException(422,detail={'code':'unverified_site','message':'Catalog point is unverified; explicit diagnostic coordinates are supported'})
    return site.lat_deg,site.lon_east_deg,'VERIFIED_CATALOG_POINT_NOMINAL_SPHERE_OBSERVER'


def calculate_response(body,cache_dir,run_dir):
    lat,lon,status=coordinates(body)
    start=parse_utc(body.landing_utc);stop=start+timedelta(seconds=body.mission_duration_h*3600)
    observer=Observer(lat,lon,body.observer_height_m/1000,coordinate_status=status)
    step=body.sampling_step_minutes*60
    tracks={};provenance={}
    for name,target in [('sun',10),('earth',399)]:
        query=HorizonsRequest(target,observer,start,stop,step)
        try:
            track=fetch_track(query,cache_dir,run_dir,attempts=1)
        except SourceError:
            # A corrupt cache is not suitable data: retry once with fresh NASA
            # acquisition, retaining the old immutable raw file and new attempt.
            if not (cache_dir/cache_key(query)/'current.json').exists(): raise
            track=fetch_track(query,cache_dir,run_dir,refresh=True,attempts=1)
        tracks[name]=track
        hit=any(run_dir.glob(f'{target}-cache-hit-*.json'))
        manifest=json.loads((cache_dir/cache_key(query)/'current.json').read_text(encoding='utf-8'))
        provenance[name]={'target_id':target,'cache_key':cache_key(query),'cache_hit':hit,
             'raw_sha256':track.raw_sha256,'raw_cache_file':f'{cache_key(query)}/{track.raw_sha256}.raw.json',
             'retrieved_utc':manifest['retrieved_utc'],'source_start_utc':utc_text(start),
             'source_end_utc':utc_text(stop),'source_n':len(track.rows),
             'request_parameters':query.params(),'metadata':track.metadata}
    paired=align_tracks(tracks['sun'],tracks['earth'])
    data=AlignedTracks(tuple(r[0] for r in paired),*(tuple(r[i] for r in paired) for i in range(1,5)),step)
    horizon=Horizon('spherical_dip',body.observer_height_m)
    flags=visibility_series(data,horizon)
    metrics=mission_metrics(data,start,body.mission_duration_h,horizon)
    return {'schema_version':1,'api_profile':'lunar-analyze-v1','data_status':'GEOMETRIC_ONLY',
       'algorithm_version':'0.1.0','run_id':run_dir.name,'generated_utc':utc_text(datetime.now(timezone.utc)),
       'site':{'id':body.site_id or '', 'lat_deg':lat,'lon_east_deg':lon,
               'observer_height_m':body.observer_height_m,'coordinate_status':status},
       'request':{'landing_utc':utc_text(start),'mission_end_utc':utc_text(stop),
                  'mission_duration_h':body.mission_duration_h,'sampling_step_minutes':body.sampling_step_minutes},
       'ephemeris':{'source':'NASA/JPL Horizons API, coord@301, apparent airless target center',
           't0_utc':utc_text(start),'step_s':step,'n':len(paired),'complete_cells':len(paired)-1,
           'utc':[utc_text(t) for t in data.timestamps],
           'sun_az_deg':data.sun_az_deg,'sun_el_deg':data.sun_el_deg,
           'earth_az_deg':data.earth_az_deg,'earth_el_deg':data.earth_el_deg},
       'terrain':{'source':'none; nominal 1737400 m sphere','az_step_deg':1.0,
                  'horizon_el_deg':[horizon.threshold_deg]*360,'incomplete_sectors':[]},
       'visibility':{'sun_margin_deg':0.0,'sun_visible':flags['sun'],'earth_visible':flags['earth'],
                     'both_visible':flags['both'],'horizon_convention':'spherical_dip','horizon_deg':horizon.threshold_deg},
       'metrics':metrics,'provenance':provenance,
       'limitations':['Geometric model only; no DEM/terrain obstruction, power, RF-link or landing-safety certification.',
           'Observer is nominal source sphere plus requested mast height; actual ground elevation is unverified.',
           'Explicit custom coordinates are not verified named landing candidates.',
           'Strict target-center elevation > spherical horizon; no atmospheric refraction.',
           'Left-held states over half-open cells; no sub-cadence crossing accuracy or extrapolation.',
           'Future Horizons UTC uses latest known leap/EOP assumptions; source headers retained.',
           'Live API profile is not the authoritative legacy Unity file contract.']}


def create_app(storage_root=None, session_id=None):
    import os
    from dataclasses import asdict
    from .sites import supported_catalog
    from .finder import OpportunitiesRequest,calculate_finder
    if storage_root is not None:
        storage = Path(storage_root)
    elif os.environ.get("LUNAR_STORAGE_DIR"):
        storage = Path(os.environ["LUNAR_STORAGE_DIR"])
    else:
        storage = BACKEND/'data/api'
    storage = storage.resolve()
    (storage / 'runs').mkdir(parents=True, exist_ok=True)
    (storage / 'cache').mkdir(parents=True, exist_ok=True)
    test_probe = storage / f'.write_test_{uuid.uuid4().hex}'
    try:
        test_probe.write_text('ok', encoding='utf-8')
        test_probe.unlink()
    except OSError as exc:
        raise OSError(f"Configured scientific storage directory '{storage}' is not writable: {exc}") from exc

    effective_session_id = session_id or os.environ.get("LUNAR_SESSION_ID")
    slots=threading.BoundedSemaphore(2)
    api=FastAPI(title='LunarNavigator Science API',version='1.0.0')

    @api.exception_handler(RequestValidationError)
    async def invalid_request(request,exc):
        # Stringify errors safely: nonfinite invalid input must never leak NaN JSON.
        return JSONResponse(status_code=422,content={'detail':{'code':'invalid_request','message':str(exc)}})

    @api.get('/health')
    def health():
        return {'status':'ok','api_profile':'lunar-analyze-v1','data_status':'GEOMETRIC_ONLY',
                'service_identity':'LunarNavigator-ScienceAPI',
                'pid':os.getpid(),
                'session_id':effective_session_id,
                'service_start_utc':utc_text(MIN_DATE),'service_stop_utc':utc_text(MAX_DATE),
                'max_mission_duration_h':744,'max_instants':10001,
                'nasa_connectivity':'not probed by health endpoint'}

    @api.get('/sites')
    def sites():
        return {'schema_version':1,'api_profile':'lunar-sites-v1','data_status':'GEOMETRIC_ONLY',
                'sites':[asdict(s) for s in sorted(supported_catalog().values(),key=lambda s:s.id) if s.verified],
                'limitations':'Verified source-cited region reference centers only; no ground/terrain/touchdown certification.'}

    @api.post('/opportunities')
    def opportunities(body:OpportunitiesRequest):
        if not slots.acquire(blocking=False):
            raise HTTPException(429,detail={'code':'busy','message':'Two analyses already in progress'})
        run=storage/'runs'/uuid.uuid4().hex
        try:
            run.mkdir(parents=True);dump_json(run/'request.json',body.model_dump())
            result=calculate_finder(body,storage/'cache',run)
            result['run_id']=run.name;dump_json(run/'response.json',result)
            return result
        except SourceError as exc:
            message=str(exc);unavailable=any(x in message for x in ('URLError','TimeoutError','HTTP 429','HTTP 50'))
            error={'code':'horizons_unavailable' if unavailable else 'invalid_horizons_data',
                   'message':message,'run_id':run.name,'synthetic_fallback':False}
            dump_json(run/'error.json',error);raise HTTPException(503 if unavailable else 502,detail=error)
        except ValueError as exc:raise HTTPException(422,detail={'code':'unsupported_request','message':str(exc)})
        except OSError:raise HTTPException(500,detail={'code':'storage_error','message':'Evidence storage failed'})
        finally:slots.release()

    @api.post('/analyze')
    def analyze(body:AnalyzeRequest):
        coordinates(body) # catalog errors before resource reservation or network
        if not slots.acquire(blocking=False):
            raise HTTPException(429,detail={'code':'busy','message':'Two analyses already in progress; retry later'})
        run=storage/'runs'/uuid.uuid4().hex
        try:
            run.mkdir(parents=True)
            dump_json(run/'request.json',body.model_dump())
            response=calculate_response(body,storage/'cache',run)
            dump_json(run/'response.json',response)
            return response
        except SourceError as exc:
            message=str(exc)
            unavailable=any(s in message for s in ('URLError','TimeoutError','HTTP 429','HTTP 50'))
            error={'code':'horizons_unavailable' if unavailable else 'invalid_horizons_data',
                   'message':message,'run_id':run.name,'synthetic_fallback':False}
            dump_json(run/'error.json',error)
            raise HTTPException(503 if unavailable else 502,detail=error)
        except ValueError as exc:
            raise HTTPException(422,detail={'code':'unsupported_request','message':str(exc)})
        except OSError:
            raise HTTPException(500,detail={'code':'storage_error','message':'Scientific evidence storage failed; no result certified'})
        finally: slots.release()
    return api


app=create_app()
