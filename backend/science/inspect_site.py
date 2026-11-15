"""Offline diagnostic analysis adapter. Calculation functions contain no I/O."""
import argparse
from dataclasses import asdict
from pathlib import Path
import csv
import json
import hashlib
from .acquisition import diagnostic_queries
from .horizons import parse_response,align_tracks,dump_json
from .coordinates import Horizon
from .visibility import AlignedTracks,mission_metrics,visibility_series
from .models import utc_text


def analyze_retained(source,out):
    source,out=Path(source).resolve(),Path(out).resolve()
    backend=Path(__file__).resolve().parents[1]
    if not out.is_relative_to(backend): raise ValueError('internal outputs must stay under backend')
    out.mkdir(parents=True,exist_ok=False)
    queries=diagnostic_queries()
    tracks={name:parse_response((source/'raw'/(name+'.json')).read_bytes(),query) for name,query in queries.items()}
    paired=align_tracks(tracks['sun'],tracks['earth'])
    data=AlignedTracks(tuple(row[0] for row in paired),*(tuple(row[i] for row in paired) for i in range(1,5)),3600)
    # Primary approved geometric model; tangent plane is separately identified.
    horizon=Horizon('spherical_dip',2)
    flags=visibility_series(data,horizon)
    missions=[mission_metrics(data,data.timestamps[0],h,horizon) for h in (24,72)]
    comparison=[mission_metrics(data,data.timestamps[0],h,Horizon('tangent_plane',2)) for h in (24,72)]
    second_date=mission_metrics(data,data.timestamps[24],24,horizon)
    with (out/'visibility.csv').open('w',newline='',encoding='utf-8') as f:
        writer=csv.writer(f)
        writer.writerow(['utc','sun_az_deg','sun_el_deg','earth_az_deg','earth_el_deg','horizon_deg','sun_visible','earth_visible','both_visible','has_following_complete_cell'])
        for i,row in enumerate(paired):
            writer.writerow([utc_text(row[0]),*row[1:],horizon.threshold_deg,flags['sun'][i],flags['earth'][i],flags['both'][i],int(i<len(paired)-1)])
    import matplotlib
    matplotlib.use('Agg')
    import matplotlib.pyplot as plt
    import matplotlib.dates as dates
    fig,ax=plt.subplots(figsize=(11,5),constrained_layout=True)
    ax.plot(data.timestamps,data.sun_el_deg,label='Sun (airless center)',color='#ca8400')
    ax.plot(data.timestamps,data.earth_el_deg,label='Earth (airless center)',color='#177ab5')
    ax.axhline(horizon.threshold_deg,color='#a32038',linestyle='--',label=f'2 m sphere horizon: {horizon.threshold_deg:.6f} deg')
    ax.axhline(0,color='gray',linewidth=.7,linestyle=':',label='Tangent-plane reference: 0 deg')
    ax.set(title='GEOMETRIC_ONLY — diagnostic point (-89.5°, 0° east)',xlabel='UTC',ylabel='Elevation (degrees)')
    ax.xaxis.set_major_formatter(dates.DateFormatter('%b %d\n%H:%M',tz=data.timestamps[0].tzinfo))
    ax.grid(alpha=.2); ax.legend(); fig.savefig(out/'visibility.png',dpi=160); plt.close(fig)
    result={'analysis_format':'INTERNAL_SCIENCE_ANALYSIS_NOT_UNITY_CONTRACT', 'data_status':'GEOMETRIC_ONLY',
            'coordinate_status':'USER_SUPPLIED_TEST_POINT_NOT_VERIFIED_LANDING_SITE',
            'observer':asdict(queries['sun'].observer),'horizon':{**asdict(horizon),'threshold_deg':horizon.threshold_deg},
            'instants':len(paired),'complete_cells':len(paired)-1,'step_s':3600,
            'source_provenance':{name:{'raw_file':str(source/'raw'/(name+'.json')),'sha256':track.raw_sha256,'metadata':track.metadata} for name,track in tracks.items()},
            'missions':missions,'tangent_plane_comparison':comparison,'second_date_comparison':second_date,
            'limitations':['No terrain obstruction, no atmospheric refraction; apparent target centers, not limb visibility.',
                          'Nominal ground on 1737400 m source sphere plus 2 m mast; actual ground is unverified.',
                          'Hourly states held over each following cell; no sub-hour crossing accuracy or extrapolation.',
                          'Visibility is not a power, RF-link, safety or landing-site certification.',
                          'Internal analysis only; production site and coordinated Unity contract gates remain.']}
    dump_json(out/'analysis.json',result)
    dump_json(out/'output-hashes.json',{p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in out.iterdir() if p.is_file()})
    return result


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source',type=Path,required=True)
    parser.add_argument('--out',type=Path,required=True)
    args=parser.parse_args()
    result=analyze_retained(args.source,args.out)
    for mission in result['missions']:
        print(json.dumps({k:mission[k] for k in ['duration_h','f_sun','f_earth','f_both','dark_max_h','comm_gap_max_h']}))


if __name__=='__main__': main()
