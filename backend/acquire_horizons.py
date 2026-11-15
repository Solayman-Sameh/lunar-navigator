"""Standalone Windows downloader / browser-response importer.

Run from LunarNavigator root with backend/.venv/Scripts/python.exe.
Default: one genuine GET attempt per target, a fresh retained run on every call.
--prepare-only: URL/browser handoff files only, no network.
--import-dir DIR: import DIR/sun.json and DIR/earth.json, no network.
"""
import argparse
from pathlib import Path
import sys
from science.acquisition import (BACKEND, PROJECT, INBOX, diagnostic_queries,
                                 new_run, prepare_handoff, process_pair)


def main(argv=None):
    parser=argparse.ArgumentParser(description=__doc__)
    mode=parser.add_mutually_exclusive_group()
    mode.add_argument('--prepare-only',action='store_true')
    mode.add_argument('--import-dir',type=Path)
    args=parser.parse_args(argv)
    if Path.cwd().resolve() != PROJECT:
        parser.error(f'Run from the project root: {PROJECT}')
    if Path(sys.executable).resolve() != BACKEND/'.venv/Scripts/python.exe':
        parser.error('Use the installed backend/.venv/Scripts/python.exe; no installation is performed')
    out=new_run()
    queries=diagnostic_queries()
    urls=prepare_handoff(out,queries)
    print(f'Run directory: {out}')
    for name,url in urls.items(): print(f'{name.title()} URL: {url}')
    if args.prepare_only:
        print(f'PREPARED (no network): {INBOX/"open-nasa-urls.html"}')
        return 0
    saved={name:args.import_dir/(name+'.json') for name in queries} if args.import_dir else None
    result=process_pair(out,saved_files=saved,queries=queries)
    for name,target in result['targets'].items():
        print(f'{name}: {target["status"]}; samples={target["samples"]}; '+target.get('error',''))
    print(f'{result["status"]}: {out/"validation.json"}')
    if result['status']=='PASS':
        print('Raw responses, CSVs and elevations.png retained. Inspect the plot before T006 review.')
        return 0
    print(f'If your normal browser works, save complete JSON responses into {INBOX} and use --import-dir.')
    return 4


if __name__=='__main__':
    raise SystemExit(main())
