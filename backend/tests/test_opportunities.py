"""SYNTHETIC Finder tests; transport mocks never enter production imports."""
import pytest
from test_api import api
from science.opportunities import rank_opportunities

REQUEST={'earliest_landing_utc':'2026-12-01T00:00:00Z','search_window_h':24,
         'mission_duration_h':24,'candidate_site_ids':['connecting_ridge','shackleton_rim','malapert_mountain'],
         'min_sun_fraction':.3,'min_earth_fraction':.4,'max_dark_h':24,'max_comm_gap_h':24,
         'search_step_h':6,'ranking_policy':'BEST_OVERALL'}


def test_sites_coordinates_and_source_scope(api):
    client,calls=api;r=client.get('/sites');assert r.status_code==200
    sites={s['id']:s for s in r.json()['sites']}
    assert len(sites)==3 and not calls
    assert (sites['connecting_ridge']['lat_deg'],sites['connecting_ridge']['lon_east_deg'])==(-89.4632,-137.49)
    assert sites['shackleton_rim']['lon_east_deg']==-171.87
    assert sites['malapert_mountain']['lon_east_deg']==-.23578
    assert all(s['verified'] and 'ntrs.nasa.gov' in s['point_source'] and 'DE421' in s['coordinate_frame'] for s in sites.values())


def test_multiple_sites_local_windows_and_cache_bounded_calls(api):
    client,calls=api
    r=client.post('/opportunities',json=REQUEST);assert r.status_code==200,r.text
    b=r.json();assert len(calls)==6 and b['evaluated_count']==12 and b['arrival_count_per_site']==4
    assert b['qualifying_count']==12 and len(b['recommendations'])==3
    assert len({(c['site_id'],c['landing_utc']) for c in b['recommendations']})==3
    assert all(s['coverage_end_utc']=='2026-12-03T00:00:00Z' for s in b['sources'])
    again=client.post('/opportunities',json=REQUEST).json()
    assert len(calls)==6 and again['recommendations']==b['recommendations']
    assert all(p['cache_hit'] for s in again['sources'] for p in s['provenance'].values())


def test_new_dates_and_durations_used(api):
    client,calls=api
    a=client.post('/opportunities',json=REQUEST).json()
    b=client.post('/opportunities',json={**REQUEST,'earliest_landing_utc':'2027-02-15T06:00:00Z'}).json()
    assert len(calls)==12 and b['recommendations']==[] and b['rejected_count']==12
    assert b['sources'][0]['coverage_start_utc']=='2027-02-15T06:00:00Z'
    c=client.post('/opportunities',json={**REQUEST,'mission_duration_h':120}).json()
    assert len(calls)==18 and c['sources'][0]['coverage_end_utc']=='2026-12-07T00:00:00Z'
    assert all(r['metrics']['duration_h']==120 for r in c['recommendations'])


@pytest.mark.parametrize('change',[{'candidate_site_ids':[]},{'candidate_site_ids':['unknown']},
    {'candidate_site_ids':['connecting_ridge','connecting_ridge']},{'search_window_h':0},
    {'search_step_h':0},{'mission_duration_h':0},{'min_sun_fraction':1.1},{'min_earth_fraction':-.1},
    {'max_dark_h':-1},{'max_comm_gap_h':-1},{'ranking_policy':'fastest'},{'earliest_landing_utc':'2027-02-15'},
    {'search_window_h':744,'mission_duration_h':24},{'search_step_h':.123}])
def test_invalid_finder_input_never_fetches(api,change):
    client,calls=api;r=client.post('/opportunities',json={**REQUEST,**change})
    assert r.status_code==422,r.text
    assert not calls


def candidate(site,time,sun=1,earth=1,both=1,dark=0,gap=0):
    return {'site_id':site,'landing_utc':time,'metrics':{'f_sun':sun,'f_earth':earth,'f_both':both,
        'dark_max_h':dark,'comm_gap_max_h':gap,'duration_h':24}}


def rank(rows,policy='BEST_OVERALL',**changes):
    args={'min_sun_fraction':.3,'min_earth_fraction':.4,'max_dark_h':24,'max_comm_gap_h':24,'ranking_policy':policy}
    return rank_opportunities(rows,**{**args,**changes})


def test_best_and_earliest_policies_differ_and_do_not_pad():
    early=candidate('a','2026-12-01T00:00:00Z',.4,.5,.4,12,12)
    late=candidate('b','2026-12-01T06:00:00Z')
    assert rank([early,late])['recommendations'][0]['site_id']=='b'
    r=rank([late,early],'EARLIEST_QUALIFYING')
    assert len(r['recommendations'])==1 and r['recommendations'][0]['site_id']=='a'


@pytest.mark.parametrize('field,value,reason',[
    ('f_sun',.2,'min_sun_fraction'),('f_earth',.3,'min_earth_fraction'),
    ('dark_max_h',24.1,'max_dark_h'),('comm_gap_max_h',24.1,'max_comm_gap_h')])
def test_each_hard_constraint_is_applied(field,value,reason):
    c=candidate('a','2026-12-01T00:00:00Z');c['metrics'][field]=value
    r=rank([c]);assert not r['recommendations']
    assert r['closest_misses'][0]['failed_constraints']==[reason]


def test_equality_zero_outage_and_deterministic_ties():
    rows=[candidate(s,'2026-12-01T00:00:00Z') for s in ['c','a','b']]
    a=rank(rows,max_dark_h=0,max_comm_gap_h=0)
    b=rank(list(reversed(rows)),max_dark_h=0,max_comm_gap_h=0)
    assert a['recommendations']==b['recommendations']
    assert [r['site_id'] for r in a['recommendations']]==['a','b','c']


def test_last_half_open_arrival_full_mission(api):
    client,calls=api
    b=client.post('/opportunities',json={**REQUEST,'candidate_site_ids':['connecting_ridge'],
         'search_window_h':12,'search_step_h':6}).json()
    assert b['evaluated_count']==2
    late=next(r for r in b['recommendations'] if r['landing_utc']=='2026-12-01T06:00:00Z')
    assert late['mission_end_utc']=='2026-12-02T06:00:00Z'
    assert all(r['landing_utc']!='2026-12-01T12:00:00Z' for r in b['recommendations'])


def test_incomplete_finder_source_fails_whole_search(api,monkeypatch):
    from science import horizons
    from test_api import Response
    import json
    client,calls=api;original=horizons.urllib.request.urlopen
    def bad(*a,**k):
        r=original(*a,**k);b=json.loads(r.raw);b['result']=b['result'].replace('$$EOE','MISSING')
        return Response(json.dumps(b).encode())
    monkeypatch.setattr(horizons.urllib.request,'urlopen',bad)
    r=client.post('/opportunities',json=REQUEST)
    assert r.status_code==502 and 'recommendations' not in r.json()


def test_named_analyze_matches_catalog(api):
    client,calls=api
    r=client.post('/analyze',json={'site_id':'shackleton_rim','landing_utc':'2027-02-15T06:00:00Z','mission_duration_h':24})
    assert r.status_code==200,r.text
    assert r.json()['site']['lat_deg']==-89.7668


def test_fractional_hours_aligned_to_cells_no_sum_rounding(api):
    client,calls=api
    r=client.post('/opportunities',json={**REQUEST,'search_window_h':.1,'mission_duration_h':.2,
        'search_step_h':.05,'sampling_step_minutes':1})
    assert r.status_code==200,r.text
    assert r.json()['evaluated_count']==6 and len(calls)==6


def test_default_14_day_grid_and_last_arrival():
    from science.finder import OpportunitiesRequest
    from science.models import parse_utc
    from datetime import timedelta
    r=OpportunitiesRequest(**{**REQUEST,'search_window_h':336,'mission_duration_h':72})
    assert len(r.arrival_offsets())==56
    assert len(r.arrival_offsets())*len(r.candidate_site_ids)==168
    assert parse_utc(r.earliest_landing_utc)+timedelta(seconds=list(r.arrival_offsets())[-1])==parse_utc('2026-12-14T18:00:00Z')
