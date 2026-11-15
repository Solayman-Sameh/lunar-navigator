"""New handoff integration checks only; fixtures are SYNTHETIC, not NASA data."""
from dataclasses import replace
import json
from pathlib import Path
from urllib.parse import urlsplit,parse_qs
from science import acquisition
from science.acquisition import diagnostic_queries,request_url,new_run,prepare_handoff,process_pair


def test_exact_url_encoding_without_network():
    queries=diagnostic_queries()
    for name,target in [('sun',10),('earth',399)]:
        params=parse_qs(urlsplit(request_url(queries[name])).query)
        assert params['COMMAND']==[f"'{target}'"]
        assert params['SITE_COORD']==["'0.0,-89.5,0.002'"]
        assert params['TIME_ZONE']==["'+00:00'"]
        assert params['CENTER']==["'coord@301'"]
        assert params['APPARENT']==["'AIRLESS'"]
        assert params['STEP_SIZE']==["'60 m'"]
        assert queries[name].expected_count==73


def test_prepare_and_rerun_preserve_saved_originals(workdir):
    inbox=workdir/'inbox'; inbox.mkdir()
    source=inbox/'sun.json'; source.write_bytes(b'SYNTHETIC pre-existing original')
    first=new_run(workdir/'runs'); second=new_run(workdir/'runs')
    prepare_handoff(first,inbox=inbox); prepare_handoff(second,inbox=inbox)
    assert first != second
    assert source.read_bytes()==b'SYNTHETIC pre-existing original'
    assert (inbox/'open-nasa-urls.html').is_file()


def test_import_rejects_html_but_retains_both_originals(workdir,monkeypatch):
    def no_network(*a,**k): raise AssertionError('import must never use network')
    monkeypatch.setattr(acquisition,'fetch_track',no_network)
    inbox=workdir/'inbox'; inbox.mkdir()
    data={'sun':b'<html>SYNTHETIC wrong save format</html>','earth':b'{"error":"SYNTHETIC error"}'}
    paths={}
    for name,raw in data.items(): paths[name]=inbox/(name+'.json'); paths[name].write_bytes(raw)
    out=new_run(workdir/'runs')
    result=process_pair(out,saved_files=paths)
    assert result['status']=='FAIL'
    for name,raw in data.items():
        assert (out/'raw'/(name+'.json')).read_bytes()==raw
        receipt=json.loads((out/(name+'-import-receipt.json')).read_text())
        assert receipt['network_request_performed'] is False and receipt['http_status'] is None
    assert not (out/'combined.csv').exists()


def test_missing_import_is_failure_without_network(workdir,monkeypatch):
    monkeypatch.setattr(acquisition,'fetch_track',lambda *a,**k: (_ for _ in ()).throw(AssertionError('network')))
    out=new_run(workdir/'runs')
    result=process_pair(out,saved_files={'sun':workdir/'missing-sun.json','earth':workdir/'missing-earth.json'})
    assert result['status']=='FAIL' and all(t['samples']==0 for t in result['targets'].values())


def test_import_validates_pair_and_preserves_exact_bytes(workdir,query,synthetic_envelope,monkeypatch):
    # Test only importer/writer coordination, never create a fake scientific plot.
    def fixture_writer(sun,earth,out):
        for name in ('sun.csv','earth.csv','combined.csv','elevations.png'):
            (out/name).write_bytes(b'SYNTHETIC HANDOFF TEST ARTIFACT -- NOT NASA OUTPUT')
    monkeypatch.setattr(acquisition,'write_pair',fixture_writer)
    monkeypatch.setattr(acquisition,'fetch_track',lambda *a,**k: (_ for _ in ()).throw(AssertionError('network')))
    paths={}
    for name,target in [('sun',10),('earth',399)]:
        paths[name]=workdir/(name+'.json'); paths[name].write_bytes(synthetic_envelope(target))
    queries={'sun':query,'earth':replace(query,target=399)}
    out=new_run(workdir/'runs')
    result=process_pair(out,saved_files=paths,queries=queries)
    assert result['status']=='PASS' and result['paired_samples']==3
    assert result['plot_visual_inspection'].startswith('NOT RUN')
    for name in paths: assert (out/'raw'/(name+'.json')).read_bytes()==paths[name].read_bytes()
