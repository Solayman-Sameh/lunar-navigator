"""Write full-precision diagnostic CSV and plots from validated tracks only."""
from pathlib import Path
import csv
import json
from .horizons import align_tracks
from .models import utc_text


def write_csv(path, fields, rows):
    with Path(path).open('w', newline='', encoding='utf-8') as handle:
        writer = csv.writer(handle)
        writer.writerow(fields)
        writer.writerows(rows)


def write_pair(sun, earth, out):
    """This function never creates placeholder or interpolated samples."""
    out = Path(out)
    paired = align_tracks(sun, earth)
    for name, track in [('sun', sun), ('earth', earth)]:
        write_csv(out/(name+'.csv'), ['utc', 'az_deg', 'el_deg'],
                  [(utc_text(r.utc), r.az_deg, r.el_deg) for r in track.rows])
    write_csv(out/'combined.csv', ['utc', 'sun_az_deg', 'sun_el_deg', 'earth_az_deg', 'earth_el_deg'],
              [(utc_text(t), sa, se, ea, ee) for t, sa, se, ea, ee in paired])
    # Import plotting only when genuine paired data have passed validation.
    import matplotlib
    matplotlib.use('Agg')
    import matplotlib.pyplot as plt
    import matplotlib.dates as dates
    fig, ax = plt.subplots(figsize=(10, 4.8), constrained_layout=True)
    for name, track, color in [('Sun', sun, '#d58900'), ('Earth', earth, '#1679b0')]:
        ax.plot([r.utc for r in track.rows], [r.el_deg for r in track.rows], label=name, color=color)
    ax.axhline(0, color='gray', linewidth=.8, linestyle='--', label='local horizontal (no terrain)')
    ax.set(title='JPL Horizons — diagnostic south-pole test point', xlabel='UTC', ylabel='Airless apparent elevation (degrees)')
    ax.xaxis.set_major_formatter(dates.DateFormatter('%b %d\n%H:%M', tz=sun.request.start.tzinfo))
    ax.grid(alpha=.2)
    ax.legend()
    fig.savefig(out/'elevations.png', dpi=160)
    plt.close(fig)
    return {'paired_samples': len(paired), 'start_utc': utc_text(paired[0][0]), 'stop_utc': utc_text(paired[-1][0]),
            'step_s': sun.request.step_s, 'csv': ['sun.csv','earth.csv','combined.csv'], 'plot': 'elevations.png',
            'interpretation': 'test coordinate and nominal observer only; no landing recommendation or terrain visibility'}
