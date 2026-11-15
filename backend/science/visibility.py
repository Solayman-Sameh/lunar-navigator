"""Pure binary64 visibility and left-held, half-open mission-cell calculations.

No network, file I/O, CLI or server dependency. Event times are sampled boundary
times, not interpolated physical contact times. Endpoint n-1 adds no extra cell.
"""
from dataclasses import dataclass
from datetime import datetime,timedelta
import math
from .models import parse_utc,utc_text
from .coordinates import Horizon


@dataclass(frozen=True)
class AlignedTracks:
    timestamps: tuple
    sun_az_deg: tuple
    sun_el_deg: tuple
    earth_az_deg: tuple
    earth_el_deg: tuple
    step_s: int

    def __post_init__(self):
        if type(self.step_s) is not int or self.step_s<=0 or len(self.timestamps)<2:
            raise ValueError('at least two boundaries and a positive integer step required')
        for field in ('timestamps','sun_az_deg','sun_el_deg','earth_az_deg','earth_el_deg'):
            object.__setattr__(self,field,tuple(getattr(self,field)))
        n=len(self.timestamps)
        for i,t in enumerate(self.timestamps):
            if not isinstance(t,datetime) or t.utcoffset() is None or t.utcoffset().total_seconds()!=0:
                raise ValueError('explicit UTC timestamps required')
            if t!=self.timestamps[0]+timedelta(seconds=i*self.step_s):
                raise ValueError('missing, duplicate or off-grid timestamp')
        for field in ('sun_az_deg','sun_el_deg','earth_az_deg','earth_el_deg'):
            values=getattr(self,field)
            if len(values)!=n:
                raise ValueError('parallel sample lengths must match')
            for value in values:
                if not isinstance(value,(int,float)) or not math.isfinite(value):
                    raise ValueError('missing/nonfinite angle')
                if ('az' in field and not 0<=value<360) or ('el' in field and not -90<=value<=90):
                    raise ValueError('angle out of range')


def visibility_state(elevation,horizon,margin=0.0):
    if not math.isfinite(elevation) or not math.isfinite(margin):
        raise ValueError('finite angle and margin required')
    if horizon is None: return None
    if not math.isfinite(horizon): raise ValueError('finite horizon required')
    return int(elevation>horizon+margin)


def periodic_horizon(profile,azimuth):
    if len(profile)!=360 or not math.isfinite(azimuth): raise ValueError('360 bins and finite azimuth required')
    a=azimuth%360; k=math.floor(a); w=a-k
    if profile[k] is None or (w and profile[(k+1)%360] is None): return None
    if not math.isfinite(profile[k]) or (w and not math.isfinite(profile[(k+1)%360])):
        raise ValueError('nonfinite skyline support')
    return profile[k] if w==0 else (1-w)*profile[k]+w*profile[(k+1)%360]


def unknown_fraction_bounds(states):
    if not states or any(x not in (0,1,None) for x in states): raise ValueError('nonempty three-state sequence required')
    return states.count(1)/len(states),(states.count(1)+states.count(None))/len(states)


def visibility_series(tracks,horizon,sun_margin_deg=0.0):
    if not isinstance(horizon,Horizon): raise ValueError('explicit Horizon required')
    sun=tuple(visibility_state(v,horizon.threshold_deg,sun_margin_deg) for v in tracks.sun_el_deg)
    earth=tuple(visibility_state(v,horizon.threshold_deg) for v in tracks.earth_el_deg)
    return {'sun':sun,'earth':earth,'both':tuple(s&e for s,e in zip(sun,earth))}


def _longest_hidden(states):
    best=run=0
    for flag in states:
        run=run+1 if flag==0 else 0
        best=max(best,run)
    return best


def _visible_intervals(states,start,step_s):
    intervals=[]; begin=None
    for i,flag in enumerate((*states,0)):
        if flag and begin is None: begin=i
        if not flag and begin is not None:
            intervals.append({'start_utc':utc_text(start+timedelta(seconds=begin*step_s)),
                              'end_utc':utc_text(start+timedelta(seconds=i*step_s))})
            begin=None
    return intervals


def _events(states,times,index):
    result={}
    for key,target,already,future in [('next_loss',0,'ALREADY_HIDDEN','FUTURE_LOSS'),
                                      ('next_recovery',1,'ALREADY_VISIBLE','FUTURE_RECOVERY')]:
        if states[index]==target: result[key]={'status':already,'utc':utc_text(times[index])}
        else:
            found=next((i for i in range(index+1,len(states)) if states[i]==target),None)
            result[key]={'status':'NONE_WITHIN_COVERAGE','utc':None} if found is None else {
                'status':future,'utc':utc_text(times[found]),
                'crossing_bracket_utc':[utc_text(times[found-1]),utc_text(times[found])]}
    return result


def mission_metrics(tracks,landing_utc,duration_h,horizon,*,sun_margin_deg=0.0):
    if not isinstance(tracks,AlignedTracks): raise ValueError('validated AlignedTracks required')
    landing=parse_utc(landing_utc) if isinstance(landing_utc,str) else landing_utc
    if not isinstance(landing,datetime) or landing.utcoffset() is None or landing.utcoffset().total_seconds()!=0:
        raise ValueError('landing must be explicit UTC')
    if isinstance(duration_h,bool) or not isinstance(duration_h,(int,float)) or not math.isfinite(duration_h) or duration_h<=0:
        raise ValueError('duration must be finite and positive')
    offset=(landing-tracks.timestamps[0]).total_seconds()
    seconds=float(duration_h)*3600
    if offset%tracks.step_s or seconds%tracks.step_s:
        raise ValueError('landing and duration must align exactly to source cells')
    a=int(offset/tracks.step_s); m=int(seconds/tracks.step_s)
    if a<0 or m<=0 or a+m>len(tracks.timestamps)-1:
        raise ValueError('entire half-open mission must fit acquired coverage; terminal row adds no cell')
    flags=visibility_series(tracks,horizon,sun_margin_deg)
    cells={name:values[a:a+m] for name,values in flags.items()}
    result={f'f_{name}':sum(values)/m for name,values in cells.items()}
    result.update(start_utc=utc_text(landing),end_utc=utc_text(landing+timedelta(seconds=seconds)),
                  duration_h=seconds/3600,cells=m,step_s=tracks.step_s,
                  horizon_convention=horizon.convention,horizon_deg=horizon.threshold_deg,
                  sun_margin_deg=sun_margin_deg,dark_max_h=_longest_hidden(cells['sun'])*tracks.step_s/3600,
                  comm_gap_max_h=_longest_hidden(cells['earth'])*tracks.step_s/3600,
                  events={name:_events(values,tracks.timestamps,a) for name,values in flags.items()},
                  event_scope='landing through last acquired instant; no extrapolation',
                  time_model='left-held states on half-open source cells; crossings resolved only to source cadence')
    for name in ('sun','earth','both'):
        result[name+'_visible_intervals']=_visible_intervals(cells[name],landing,tracks.step_s)
    result['visibility_changes']=[{'utc':utc_text(tracks.timestamps[i]),
            'sun_visible':flags['sun'][i],'earth_visible':flags['earth'][i],'both_visible':flags['both'][i]}
        for i in range(a+1,a+m) if any(flags[k][i]!=flags[k][i-1] for k in flags)]
    return result
