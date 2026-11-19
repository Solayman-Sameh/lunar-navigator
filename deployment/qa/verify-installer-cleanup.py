"""Exercise the real Inno installer/uninstaller in an isolated workspace.
Use test-only installers compiled with registration disabled and /NOICONS.
No user installation or Windows registration is modified.
"""
import argparse
import json
from pathlib import Path
import subprocess


def run_installer(exe, target, log):
    result = subprocess.run([str(exe), '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART',
        '/NOICONS', '/TASKS=', '/DIR=' + str(target), '/LOG=' + str(log)], timeout=120)
    if result.returncode:
        raise RuntimeError('Installer failed: ' + str(result.returncode))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--installer', type=Path, required=True)
    parser.add_argument('--old-installer', type=Path)
    parser.add_argument('--workspace', type=Path, required=True)
    args = parser.parse_args()
    base = args.workspace.resolve()
    # Require a dedicated, fresh test workspace; never use an existing install.
    if base.exists():
        raise RuntimeError('Test workspace already exists')
    base.mkdir(parents=True)
    evidence = []
    cases = [('clean', False), ('preserve-unrelated', True)]
    if args.old_installer:
        cases.append(('upgrade', False))
    for name, keep_note in cases:
        target = (base / name / 'LunarNavigator').resolve()
        assert target.is_relative_to(base) and target.name == 'LunarNavigator'
        target.parent.mkdir()
        if name == 'upgrade':
            run_installer(args.old_installer.resolve(), target, base / (name + '-old-install.log'))
        run_installer(args.installer.resolve(), target, base / (name + '-install.log'))
        assert (target / 'installation-marker.txt').read_text().strip() == 'LunarNavigator-owned-installation-v1'
        external = target.parent / 'saved-user-data'
        external.mkdir()
        (external / 'preserve.txt').write_text('saved analysis evidence')
        # Import the actual installed backend, generating real nested __pycache__.
        subprocess.run([str(target / 'backend/python.exe'), '-c',
            'import science.finder,fastapi; from science.api import create_app; create_app(storage_root=r"' +
            str(external) + '")'], check=True, timeout=30)
        assert list((target / 'backend').rglob('*.pyc'))
        (target / 'backend/generated-runtime').mkdir()
        (target / 'backend/generated-runtime/leftover.tmp').write_text('runtime generated')
        (target / 'bin/runtime-leftover.tmp').write_text('runtime generated')
        if keep_note:
            (target / 'my-notes.txt').write_text('unrelated file')
        result = subprocess.run([str(target / 'unins000.exe'), '/VERYSILENT', '/SUPPRESSMSGBOXES',
            '/NORESTART', '/LOG=' + str(base / (name + '-uninstall.log'))], timeout=120)
        assert result.returncode == 0, result.returncode
        # Inno removes its temporary executable/directory just after process exit.
        import time
        for _ in range(40):
            if not (target / 'unins000.exe').exists():
                break
            time.sleep(.1)
        assert not (target / 'backend').exists()
        assert not (target / 'bin').exists()
        assert (external / 'preserve.txt').read_text() == 'saved analysis evidence'
        if keep_note:
            assert sorted(p.name for p in target.iterdir()) == ['my-notes.txt']
        else:
            for _ in range(40):
                if not target.exists():
                    break
                time.sleep(.1)
            assert not target.exists(), list(target.iterdir())
        evidence.append({'case': name, 'result': 'passed', 'backend_removed': True,
                         'player_removed': True, 'saved_data_preserved': True,
                         'unrelated_root_file_preserved': keep_note})
        print('PASS ' + name, flush=True)
    (base / 'results.json').write_text(json.dumps(evidence, indent=2))


if __name__ == '__main__':
    main()
