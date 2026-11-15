from datetime import timedelta
import pytest
from science.models import parse_utc
from science.coordinates import Horizon
from science.visibility import (AlignedTracks,mission_metrics,visibility_series,
                                periodic_horizon,visibility_state,unknown_fraction_bounds)

T0=parse_utc('2026-12-01T00:00:00Z')


def tracks(s,e,step=600):
    return AlignedTracks(tuple(T0+timedelta(seconds=i*step) for i in range(len(s))),
                         (0,)*len(s),tuple(1 if x else -1 for x in s),
                         (0,)*len(e),tuple(1 if x else -1 for x in e),step)


def calc(s,e,hours,step=600,start=T0):
    return mission_metrics(tracks(s,e,step),start,hours,Horizon('tangent_plane',2))


def test_hand_worked_ten_cells():
    s=[int(x) for x in '1100111011']+[0]
    e=[int(x) for x in '1010110011']+[0]
    r=calc(s,e,100/60)
    assert [r[k] for k in ('f_sun','f_earth','f_both')]==pytest.approx([.7,.6,.5],abs=1e-12)
    assert r['dark_max_h']==pytest.approx(1/3,abs=1e-9)
    assert r['comm_gap_max_h']==pytest.approx(1/3,abs=1e-9)
    assert len(r['sun_visible_intervals'])==3


@pytest.mark.parametrize('s,e,expected',[(1,1,(1,1,1,0,0)),(0,1,(0,1,0,1,0)),(1,0,(1,0,0,0,1)),(0,0,(0,0,0,1,1))])
def test_always_states(s,e,expected):
    r=calc([s]*7,[e]*7,1)
    assert tuple(r[k] for k in ('f_sun','f_earth','f_both','dark_max_h','comm_gap_max_h'))==expected


def test_alternating_and_terminal_not_counted():
    r=calc([1,0,1,0,1,0,1],[0,1,0,1,0,1,1],1)
    assert r['f_sun']==r['f_earth']==.5 and r['f_both']==0
    assert r['dark_max_h']==pytest.approx(1/6)


def test_exact_window_slice_and_blackouts():
    r=calc([0,0,1,1,0,1,1],[1]*7,4/6,start=T0+timedelta(minutes=20))
    assert r['f_sun']==.75
    assert r['dark_max_h']==pytest.approx(1/6)
    assert r['sun_visible_intervals'][0]=={'start_utc':'2026-12-01T00:20:00Z','end_utc':'2026-12-01T00:40:00Z'}


@pytest.mark.parametrize('start,duration',[(T0,7/6),(T0-timedelta(seconds=600),1),(T0+timedelta(seconds=1),1),(T0,0),(T0,-1),(T0,float('nan')),(T0,.123)])
def test_invalid_or_outside_window(start,duration):
    with pytest.raises(ValueError): calc([1]*7,[1]*7,duration,start=start)


def test_missing_malformed_samples():
    with pytest.raises(ValueError): AlignedTracks((T0,T0+timedelta(hours=1)),(0,0),(1,), (0,0),(1,1),3600)
    for times,values in [((T0,T0),(1,1)),((T0,T0+timedelta(hours=2)),(1,1)),((T0,T0+timedelta(hours=1)),(1,float('nan')))]:
        with pytest.raises(ValueError): AlignedTracks(times,(0,0),values,(0,0),(1,1),3600)


def test_equality_margin_and_full_precision():
    assert visibility_state(0,0)==0
    assert visibility_state(1e-13,0)==1
    assert visibility_state(.1,0,.1)==0
    assert visibility_state(1,None) is None
    assert unknown_fraction_bounds([1,None,0])==(1/3,2/3)


def test_periodic_wrap_and_unknown_support():
    h=[0.0]*360; h[359]=2
    assert periodic_horizon(h,359.5)==1
    assert periodic_horizon(h,-.5)==1
    assert periodic_horizon(h,360)==0
    h[1]=None
    assert periodic_horizon(h,0)==0
    assert periodic_horizon(h,.5) is None


def test_events_loss_recovery_and_coverage():
    r=calc([1,1,0,0,1,1,1],[0,0,1,1,1,1,1],1)
    assert r['events']['sun']['next_loss']['status']=='FUTURE_LOSS'
    assert r['events']['sun']['next_loss']['utc']=='2026-12-01T00:20:00Z'
    assert r['events']['earth']['next_loss']['status']=='ALREADY_HIDDEN'
    assert r['events']['earth']['next_recovery']['utc']=='2026-12-01T00:20:00Z'
    r=calc([1]*7,[0]*7,1)
    assert r['events']['sun']['next_loss']['status']=='NONE_WITHIN_COVERAGE'
    assert r['events']['earth']['next_recovery']['status']=='NONE_WITHIN_COVERAGE'


def test_dip_is_explicit_and_changes_near_horizon_state():
    t=AlignedTracks((T0,T0+timedelta(hours=1)),(0,0),(-.05,-.05),(0,0),(-.05,-.05),3600)
    assert mission_metrics(t,T0,1,Horizon('tangent_plane',2))['f_sun']==0
    assert mission_metrics(t,T0,1,Horizon('spherical_dip',2))['f_sun']==1


def test_end_boundary_event_does_not_add_mission_cell_or_transition():
    r=calc([1,1,1,1,1,1,0],[1]*7,1)
    assert r['f_sun']==1 and r['dark_max_h']==0
    assert r['visibility_changes']==[]
    assert r['events']['sun']['next_loss']['utc']=='2026-12-01T01:00:00Z'


def test_nonfinite_skyline_support_rejects():
    h=[0.0]*360; h[1]=float('nan')
    with pytest.raises(ValueError): periodic_horizon(h,.5)
