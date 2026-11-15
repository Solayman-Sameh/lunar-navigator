"""Executed real-data mission oracle for the retained diagnostic point."""
from pathlib import Path
import pytest
from science.acquisition import diagnostic_queries
from science.horizons import parse_response,align_tracks
from science.visibility import AlignedTracks,mission_metrics
from science.coordinates import Horizon

pytestmark=pytest.mark.real_data


@pytest.mark.parametrize('hours',[24,72])
def test_real_geometric_mission(hours):
    source=Path(__file__).resolve().parents[1]/'data/runs/local-horizons-20261115T182625Z-4a08a2a5/raw'
    if not (source/'sun.json').is_file(): pytest.skip('real source absent; NOT a scientific PASS')
    tracks={name:parse_response((source/(name+'.json')).read_bytes(),q) for name,q in diagnostic_queries().items()}
    paired=align_tracks(tracks['sun'],tracks['earth'])
    data=AlignedTracks(tuple(r[0] for r in paired),*(tuple(r[i] for r in paired) for i in range(1,5)),3600)
    assert len(data.timestamps)==73
    for convention in ('tangent_plane','spherical_dip'):
        result=mission_metrics(data,data.timestamps[0],hours,Horizon(convention,2))
        assert result['f_sun']==1 and result['f_earth']==result['f_both']==0
        assert result['dark_max_h']==0 and result['comm_gap_max_h']==hours
        assert len(result['sun_visible_intervals'])==1 and not result['earth_visible_intervals']
    with pytest.raises(ValueError): mission_metrics(data,data.timestamps[0],73,Horizon('spherical_dip',2))
