"""Dynamic Finder: one full covering ephemeris pair per site, local windows."""
from datetime import timedelta
from typing import Literal
from pydantic import BaseModel,ConfigDict,Field,model_validator
from fastapi import HTTPException
from .models import parse_utc,utc_text
from .sites import supported_catalog
from .coordinates import Horizon
from .visibility import AlignedTracks,mission_metrics
from .opportunities import rank_opportunities


class OpportunitiesRequest(BaseModel):
    model_config=ConfigDict(extra='forbid',allow_inf_nan=False)
    earliest_landing_utc:str
    search_window_h:float=Field(gt=0,le=744,strict=True)
    mission_duration_h:float=Field(gt=0,le=744,strict=True)
    candidate_site_ids:list[str]=Field(min_length=1,max_length=3)
    min_sun_fraction:float=Field(default=.3,ge=0,le=1,strict=True)
    min_earth_fraction:float=Field(default=.4,ge=0,le=1,strict=True)
    max_dark_h:float=Field(default=36,ge=0,le=744,strict=True)
    max_comm_gap_h:float=Field(default=48,ge=0,le=744,strict=True)
    search_step_h:float=Field(default=6.0,gt=0,le=744,strict=True)
    ranking_policy:Literal['BEST_OVERALL','EARLIEST_QUALIFYING']='BEST_OVERALL'
    observer_height_m:float=Field(default=2,ge=0,le=1000,strict=True)
    sampling_step_minutes:int=Field(default=10,ge=1,le=60,strict=True)

    @model_validator(mode='after')
    def validate_grid(self):
        from .api import AnalyzeRequest
        if len(set(self.candidate_site_ids))!=len(self.candidate_site_ids): raise ValueError('duplicate candidate site IDs')
        step=self.sampling_step_minutes*60
        for h in (self.search_window_h,self.search_step_h,self.mission_duration_h):
            if not (h*3600).is_integer() or h*3600%step:raise ValueError('search and mission times must be whole source cells; no rounding')
        AnalyzeRequest(latitude_deg=-89.5,longitude_deg=0.0,landing_utc=self.earliest_landing_utc,
                       mission_duration_h=self.covering_hours,
                       observer_height_m=self.observer_height_m,sampling_step_minutes=self.sampling_step_minutes)
        if len(self.arrival_offsets())*len(self.candidate_site_ids)>1000:raise ValueError('maximum 1000 site-arrival scenarios')
        return self

    def arrival_offsets(self):
        return range(0,int(self.search_window_h*3600),int(self.search_step_h*3600))

    @property
    def covering_hours(self):
        return (int(self.search_window_h*3600)+int(self.mission_duration_h*3600))/3600


def calculate_finder(body,cache_dir,run_dir):
    from .api import AnalyzeRequest,calculate_response
    catalog=supported_catalog();sites=[]
    for site_id in body.candidate_site_ids:
        if site_id not in catalog:raise HTTPException(422,detail={'code':'unknown_site','message':site_id})
        try: sites.append(catalog[site_id].require_verified())
        except ValueError:raise HTTPException(422,detail={'code':'unverified_site','message':site_id})
    start=parse_utc(body.earliest_landing_utc);candidates=[];sources=[]
    for site in sites:
        site_run=run_dir/site.id;site_run.mkdir()
        covering=AnalyzeRequest(site_id=site.id,landing_utc=body.earliest_landing_utc,
            mission_duration_h=body.covering_hours,
            observer_height_m=body.observer_height_m,sampling_step_minutes=body.sampling_step_minutes)
        # Exactly one covering pair; NEVER call NASA inside the arrival loop.
        result=calculate_response(covering,cache_dir,site_run)
        e=result['ephemeris']
        data=AlignedTracks(tuple(parse_utc(t) for t in e['utc']),e['sun_az_deg'],e['sun_el_deg'],
                           e['earth_az_deg'],e['earth_el_deg'],e['step_s'])
        sources.append({'site_id':site.id,'coverage_start_utc':e['utc'][0],
                        'coverage_end_utc':e['utc'][-1],'n':e['n'],'provenance':result['provenance']})
        for seconds in body.arrival_offsets():
            landing=start+timedelta(seconds=seconds)
            metrics=mission_metrics(data,landing,body.mission_duration_h,Horizon('spherical_dip',body.observer_height_m))
            candidates.append({'site_id':site.id,'site_name':site.name,'landing_utc':utc_text(landing),
                'mission_end_utc':metrics['end_utc'],'mission_duration_h':body.mission_duration_h,'metrics':metrics})
    ranked=rank_opportunities(candidates,min_sun_fraction=body.min_sun_fraction,min_earth_fraction=body.min_earth_fraction,
                             max_dark_h=body.max_dark_h,max_comm_gap_h=body.max_comm_gap_h,ranking_policy=body.ranking_policy)
    return {'schema_version':1,'api_profile':'lunar-opportunities-v1','data_status':'GEOMETRIC_ONLY',
            'request':body.model_dump(),'candidate_sites':body.candidate_site_ids,'sources':sources,
            'arrival_count_per_site':len(body.arrival_offsets()),'source_pair_count':len(sites),
            'score_formula':'0.4*f_sun+0.3*f_earth+0.2*f_both+0.1*(1-min(1,dark_max_h/duration_h))',
            'constraints_note':'User-editable illustrative constraints; not NASA mission standards.',
            'limitations':['Sourced reference centers, not certified touchdown points; ground heights unverified.',
                'Nominal-sphere GEOMETRIC_ONLY: no terrain obstruction, power/RF/safety certification.',
                'Arrival range start inclusive/end exclusive; entire mission covered; source states left-held at selected cadence.'],
            **ranked}
