"""HTTP integration with SYNTHETIC transport responses; never real NASA proof."""
from datetime import datetime,timedelta
import json
import urllib.error
from urllib.parse import urlsplit,parse_qs
import pytest
from fastapi.testclient import TestClient
from science.api import create_app
from science import horizons


BASE={'latitude_deg':-89.5,'longitude_deg':0.0,'landing_utc':'2026-12-01T00:00:00Z','mission_duration_h':24}


class Response:
    status=200
    headers={'Content-Type':'application/json'}
    def __init__(self,raw):self.raw=raw
    def read(self):return self.raw
    def __enter__(self):return self
    def __exit__(self,*args):return False


@pytest.fixture
def api(workdir,monkeypatch):
    calls=[]
    def transport(req,timeout=30):
        p={k:v[0].strip("'") for k,v in parse_qs(urlsplit(req.full_url).query).items()}
        calls.append(p)
        target=int(p['COMMAND']);name='Sun' if target==10 else 'Earth'
        start=datetime.strptime(p['START_TIME'],'%Y-%m-%d %H:%M:%S')
        stop=datetime.strptime(p['STOP_TIME'],'%Y-%m-%d %H:%M:%S')
        minutes=int(p['STEP_SIZE'].split()[0]);lon,lat,alt=p['SITE_COORD'].split(',')
        count=int((stop-start).total_seconds()//(minutes*60))+1
        rows=[]
        # Explicitly SYNTHETIC, date-dependent signs prove live request plumbing.
        el=1 if start.month==12 else -1
        for i in range(count):
            stamp=(start+timedelta(minutes=i*minutes)).strftime('%Y-%b-%d %H:%M:%S')
            rows.append(f' {stamp}, , ,10.123456789,{el},')
        result=f'''SYNTHETIC TRANSPORT FIXTURE -- NOT NASA OBSERVATIONS
Target body name: {name} ({target}) {{source: SYNTHETIC}}
Center body name: Moon (301) {{source: SYNTHETIC}}
Center-site name: (user defined site below)
Center geodetic : {lon}, {lat}, {alt} {{E-lon(deg),Lat(deg),Alt(km)}}
Center pole/equ : MEAN_ME (high precision)
Center radii    : 1737.4, 1737.4, 1737.4 km
Atmos refraction: NO (AIRLESS)
Start time      : A.D. {start.strftime('%Y-%b-%d %H:%M:%S')}.0000 UT
Stop  time      : A.D. {stop.strftime('%Y-%b-%d %H:%M:%S')}.0000 UT
Step-size       : {minutes} minutes
Date__(UT)__HR:MN:SS, , ,Azimuth_(a-app),Elevation_(a-app),
$$SOE
'''+ '\n'.join(rows)+'\n$$EOE\n'
        return Response(json.dumps({'signature':{'source':'NASA/JPL Horizons API','version':'1.2'},'result':result}).encode())
    monkeypatch.setattr(horizons.urllib.request,'urlopen',transport)
    return TestClient(create_app(workdir)),calls


def test_health_no_network(api):
    client,calls=api
    assert client.get('/health').status_code==200 and not calls


@pytest.mark.parametrize('hours,count',[(24,145),(120,721)])
def test_valid_missions(api,hours,count):
    client,calls=api
    r=client.post('/analyze',json={**BASE,'mission_duration_h':hours})
    assert r.status_code==200,r.text
    b=r.json();assert b['ephemeris']['n']==count and b['ephemeris']['complete_cells']==count-1
    assert b['metrics']['f_sun']==1 and b['metrics']['f_both']==1
    assert b['metrics']['duration_h']==hours
    assert b['ephemeris']['utc'][-1]==b['request']['mission_end_utc']
    assert b['ephemeris']['sun_az_deg'][0]==10.123456789
    assert len(calls)==2 and b['data_status']=='GEOMETRIC_ONLY'
    assert len(b['provenance']['sun']['raw_sha256'])==64


def test_distinct_dates_and_no_december_substitution(api):
    client,calls=api
    a=client.post('/analyze',json=BASE).json()
    b=client.post('/analyze',json={**BASE,'landing_utc':'2027-02-15T06:00:00Z'}).json()
    assert len(calls)==4 and b['request']['landing_utc']=='2027-02-15T06:00:00Z'
    assert a['provenance']['sun']['cache_key']!=b['provenance']['sun']['cache_key']
    assert b['provenance']['sun']['source_start_utc']==b['ephemeris']['utc'][0]
    assert b['metrics']['f_sun']==0 and a['metrics']['f_sun']==1


def test_identity_cache_and_changed_duration_coordinate_height_cadence(api):
    client,calls=api
    first=client.post('/analyze',json=BASE).json()
    repeat=client.post('/analyze',json=BASE).json()
    assert len(calls)==2 and repeat['provenance']['sun']['cache_hit'] is True
    assert first['provenance']['sun']['raw_sha256']==repeat['provenance']['sun']['raw_sha256']
    for change in [{'mission_duration_h':120},{'longitude_deg':12.5},{'latitude_deg':-80.0},
                   {'observer_height_m':10.0},{'sampling_step_minutes':60}]:
        r=client.post('/analyze',json={**BASE,**change})
        assert r.status_code==200,r.text
        assert r.json()['provenance']['sun']['cache_key']!=first['provenance']['sun']['cache_key']
    assert len(calls)==12


@pytest.mark.parametrize('change',[
    {'landing_utc':'bad'},{'landing_utc':'2027-02-15'},{'landing_utc':'2027-02-15T06:00:00+02:00'},
    {'landing_utc':'2030-01-01T00:00:00Z'},{'latitude_deg':-90},{'longitude_deg':181},
    {'latitude_deg':'south'},{'mission_duration_h':0},{'mission_duration_h':-1},
    {'mission_duration_h':1000},{'mission_duration_h':.1},{'sampling_step_minutes':0},
    {'sampling_step_minutes':True},{'observer_height_m':-1},{'site_id':'shackleton_rim'},
    {'extra':'not allowed'},{'mission_duration_h':744,'sampling_step_minutes':1}])
def test_invalid_inputs_never_fetch(api,change):
    client,calls=api
    assert client.post('/analyze',json={**BASE,**change}).status_code==422
    assert not calls


def test_unverified_catalog_rejected(api,monkeypatch):
    from science.sites import SiteRecord
    import science.api as api_module
    monkeypatch.setattr(api_module,'load_catalog',lambda rows:{'shackleton_rim':SiteRecord('shackleton_rim','unverified',None,None,False,'','')})
    client,calls=api
    r=client.post('/analyze',json={'site_id':'shackleton_rim','landing_utc':BASE['landing_utc'],'mission_duration_h':24})
    assert r.status_code==422 and not calls


def test_nonfinite_input_error_serializes(api):
    client,calls=api
    text=json.dumps(BASE).replace('-89.5','NaN')
    r=client.post('/analyze',content=text,headers={'Content-Type':'application/json'})
    assert r.status_code==422 and not calls


def test_network_failure_503_no_fake_result(api,monkeypatch):
    client,calls=api
    def fail(*a,**k):raise urllib.error.URLError('SYNTHETIC network refused')
    monkeypatch.setattr(horizons.urllib.request,'urlopen',fail)
    r=client.post('/analyze',json=BASE)
    assert r.status_code==503 and r.json()['detail']['synthetic_fallback'] is False


def test_incomplete_response_rejected_502(api,monkeypatch):
    client,calls=api
    original=horizons.urllib.request.urlopen
    def incomplete(*a,**k):
        response=original(*a,**k); body=json.loads(response.raw)
        body['result']=body['result'].replace('$$EOE','MISSING END')
        return Response(json.dumps(body).encode())
    monkeypatch.setattr(horizons.urllib.request,'urlopen',incomplete)
    r=client.post('/analyze',json=BASE)
    assert r.status_code==502 and 'invalid_horizons_data' in r.text


def test_time_of_day_preserved_not_globally_snapped(api):
    client,calls=api
    r=client.post('/analyze',json={**BASE,'landing_utc':'2027-02-15T06:13:25Z'})
    assert r.status_code==200,r.text
    assert r.json()['ephemeris']['utc'][0]=='2027-02-15T06:13:25Z'


def test_missing_source_sample_never_shortens_mission(api,monkeypatch):
    client,calls=api
    original=horizons.urllib.request.urlopen
    def missing(*a,**k):
        response=original(*a,**k);body=json.loads(response.raw)
        lines=body['result'].splitlines()
        index=lines.index('$$SOE')+2
        del lines[index]
        body['result']='\n'.join(lines)+'\n'
        return Response(json.dumps(body).encode())
    monkeypatch.setattr(horizons.urllib.request,'urlopen',missing)
    r=client.post('/analyze',json=BASE)
    assert r.status_code==502 and 'count' in r.text


def test_corrupt_cache_refreshed_not_served(api,workdir):
    client,calls=api
    first=client.post('/analyze',json=BASE).json()
    entry=first['provenance']['sun']
    raw=workdir/'cache'/entry['raw_cache_file']
    raw.write_bytes(b'SYNTHETIC deliberate cache corruption')
    second=client.post('/analyze',json=BASE)
    assert second.status_code==200,second.text
    import hashlib
    assert hashlib.sha256(raw.read_bytes()).hexdigest()==second.json()['provenance']['sun']['raw_sha256']
    backups=list(raw.parent.glob('corrupt-*.bin'))
    assert len(backups)==1 and backups[0].read_bytes()==b'SYNTHETIC deliberate cache corruption'
    assert len(calls)==3


def test_east_longitude_equivalent_source_header(api,monkeypatch):
    client,calls=api
    original=horizons.urllib.request.urlopen
    def wrapped(*a,**k):
        response=original(*a,**k);body=json.loads(response.raw)
        body['result']=body['result'].replace('Center geodetic : -137.5,','Center geodetic : 222.5,')
        return Response(json.dumps(body).encode())
    monkeypatch.setattr(horizons.urllib.request,'urlopen',wrapped)
    r=client.post('/analyze',json={**BASE,'longitude_deg':-137.5})
    assert r.status_code==200,r.text
    assert r.json()['site']['lon_east_deg']==-137.5
