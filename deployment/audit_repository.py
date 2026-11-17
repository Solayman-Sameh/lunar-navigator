"""Audit the selected Git index before a source release; never delete local files."""
import argparse
import json
from pathlib import Path
import re
import subprocess
from urllib.parse import unquote


def audit(root, git_dir=None):
    command = ['git']
    if git_dir:
        command += [f'--git-dir={git_dir}', f'--work-tree={root}']
    files = subprocess.check_output(command + ['ls-files', '-z'], cwd=root).decode().split('\0')
    files = {name for name in files if name}
    errors = []
    warnings = []
    forbidden = ('.agents/', '.specify/', '.codex/', 'specs/', 'Screenshots/',
                 'Documentation/', 'Builds/', 'Library/', 'Temp/', 'Logs/',
                 'UserSettings/', 'backend/data/api/', 'backend/data/runs/')
    secret = re.compile(r'-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----|'
                        r'gh[pousr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{50,}')
    guid_owners = {}
    for name in sorted(files):
        path = root / name
        if name.startswith(forbidden) or any(p in path.parts for p in ('.venv', '__pycache__')):
            errors.append(f'Local/generated content tracked: {name}')
        if path.suffix.lower() in ('.exe', '.zip', '.mp4', '.pyc'):
            errors.append(f'Release payload tracked: {name}')
        if not path.is_file():
            errors.append(f'Indexed file missing: {name}')
            continue
        if path.stat().st_size >= 100 * 1024 * 1024:
            errors.append(f'File exceeds source size gate: {name}')
        if path.suffix.lower() not in ('.md', '.cs', '.py', '.json', '.ps1', '.iss', '.meta', '.txt', '.yml'):
            continue
        text = path.read_text(encoding='utf-8-sig', errors='replace')
        if secret.search(text):
            errors.append(f'Credential pattern found (value omitted): {name}')
        if path.suffix == '.meta':
            match = re.search(r'^guid: ([a-f0-9]{32})$', text, re.M)
            if match:
                if match[1] in guid_owners:
                    errors.append(f'Duplicate Unity GUID: {name}, {guid_owners[match[1]]}')
                guid_owners[match[1]] = name
        if name.startswith('Assets/') and not name.endswith('.meta') and name + '.meta' not in files:
            errors.append(f'Missing Unity asset meta: {name}')
        if path.suffix != '.md':
            continue
        for destination in re.findall(r'!?\[[^\]]*\]\(([^)]+)\)', text):
            destination = destination.split(' "')[0].strip('<>')
            if re.match(r'^[a-zA-Z]+:', destination):
                if destination.startswith(('file:', 'sandbox:')):
                    errors.append(f'Nonportable document link: {name}')
                continue
            target = unquote(destination.split('#')[0].split('?')[0])
            if not target:
                continue
            resolved = (path.parent / target).resolve()
            try:
                relative = resolved.relative_to(root).as_posix()
            except ValueError:
                errors.append(f'Document link escapes repository: {name} -> {target}')
                continue
            if relative not in files and not any(f.startswith(relative + '/') for f in files):
                errors.append(f'Document link absent from source tree: {name} -> {target}')
    required = ['README.md', 'docs/PROJECT_PROPOSAL.md', 'media/ui-overview.png',
                'backend/data/sites.json', 'launcher/LunarLauncher.csproj',
                'installer/LunarNavigator.iss', 'backend/requirements-runtime.txt',
                'Assets/Scenes/LunarNavigator_Main.unity']
    for name in required:
        if name not in files:
            errors.append(f'Required source missing: {name}')
    if 'media/demo-record.mp4' in files:
        errors.append('Demo placeholder must not be tracked')
    return {'files': len(files), 'total_bytes': sum((root/f).stat().st_size for f in files if (root/f).is_file()),
            'unity_guids': len(guid_owners), 'errors': errors, 'warnings': warnings}


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--root', type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument('--git-dir', type=Path)
    args = parser.parse_args()
    result = audit(args.root.resolve(), args.git_dir.resolve() if args.git_dir else None)
    print(json.dumps(result, indent=2))
    raise SystemExit(bool(result['errors']))
