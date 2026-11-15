"""Pure mission filtering and deterministic ranking; no HTTP or file I/O."""


def rank_opportunities(candidates,*,min_sun_fraction,min_earth_fraction,max_dark_h,max_comm_gap_h,ranking_policy):
    if ranking_policy not in ('BEST_OVERALL','EARLIEST_QUALIFYING'):
        raise ValueError('unknown ranking policy')
    passing=[];misses=[]
    for candidate in candidates:
        c=dict(candidate);m=c['metrics'];failed=[]
        for key,ok in [('min_sun_fraction',m['f_sun']>=min_sun_fraction),
                       ('min_earth_fraction',m['f_earth']>=min_earth_fraction),
                       ('max_dark_h',m['dark_max_h']<=max_dark_h),
                       ('max_comm_gap_h',m['comm_gap_max_h']<=max_comm_gap_h)]:
            if not ok:failed.append(key)
        if failed:
            c['failed_constraints']=failed
            c['normalized_violation']=(max(0,min_sun_fraction-m['f_sun'])+max(0,min_earth_fraction-m['f_earth'])+
                max(0,m['dark_max_h']-max_dark_h)/m['duration_h']+max(0,m['comm_gap_max_h']-max_comm_gap_h)/m['duration_h'])
            misses.append(c);continue
        c['score']=.4*m['f_sun']+.3*m['f_earth']+.2*m['f_both']+.1*(1-min(1,m['dark_max_h']/m['duration_h']))
        c['explanation']='All four hard constraints pass; score uses unrounded Sun/Earth/joint fractions and darkness burden.'
        passing.append(c)
    cohort=passing
    if ranking_policy=='EARLIEST_QUALIFYING' and passing:
        earliest=min(c['landing_utc'] for c in passing)
        cohort=[c for c in passing if c['landing_utc']==earliest]
    ordered=sorted(cohort,key=lambda c:(-c['score'],c['landing_utc'],c['site_id']))
    recommendations=[];seen=set()
    for c in ordered:
        identity=(c['site_id'],c['landing_utc'])
        if identity in seen:continue
        seen.add(identity);recommendations.append({'rank':len(recommendations)+1,**c})
        if len(recommendations)==3:break
    misses.sort(key=lambda c:(len(c['failed_constraints']),c['normalized_violation'],c['landing_utc'],c['site_id']))
    return {'recommendations':recommendations,'closest_misses':misses[:5],
            'evaluated_count':len(candidates),'rejected_count':len(misses),'qualifying_count':len(passing),
            'empty_reason':None if passing else 'No scenario satisfies all four hard constraints.'}
