"""Validate pinned binary wheels and install their import payload without runtime pip.

Runs with build-host Python (packaging installed), never on reviewer machines.
"""
import argparse
from email import message_from_bytes
import hashlib
import json
from pathlib import Path, PurePosixPath
import shutil
import zipfile

from packaging.requirements import Requirement
from packaging.tags import sys_tags
from packaging.utils import canonicalize_name, parse_wheel_filename


def assemble(requirements, wheelhouse, target):
    pins = {}
    for line in requirements.read_text().splitlines():
        line = line.strip()
        if not line or line.startswith("#"):
            continue
        req = Requirement(line)
        if req.marker is None or req.marker.evaluate():
            pins[canonicalize_name(req.name)] = req
    compatible = set(sys_tags())
    selected = {}
    for file in sorted(wheelhouse.glob("*.whl")):
        name, version, _, tags = parse_wheel_filename(file.name)
        if name in pins and version in pins[name].specifier and tags & compatible:
            selected.setdefault(name, (file, version))
    missing = pins.keys() - selected.keys()
    if missing:
        raise ValueError("Missing compatible pinned wheels: " + ", ".join(sorted(missing)))
    manifests = []
    for name, (file, version) in selected.items():
        with zipfile.ZipFile(file) as archive:
            metadata = [n for n in archive.namelist() if n.endswith(".dist-info/METADATA")]
            if len(metadata) != 1:
                raise ValueError("Wheel must contain one METADATA: " + file.name)
            meta = message_from_bytes(archive.read(metadata[0]))
            if canonicalize_name(meta["Name"]) != name or meta["Version"] != str(version):
                raise ValueError("Wheel filename/metadata mismatch: " + file.name)
            for dependency in meta.get_all("Requires-Dist", []):
                req = Requirement(dependency)
                if req.marker is not None and not req.marker.evaluate({"extra": ""}):
                    continue
                dep = selected.get(canonicalize_name(req.name))
                if dep is None or dep[1] not in req.specifier:
                    raise ValueError(f"Unsatisfied dependency for {name}: {req}")
            for entry in archive.infolist():
                parts = PurePosixPath(entry.filename).parts
                if ".." in parts or PurePosixPath(entry.filename).is_absolute() or ":" in entry.filename or "\\" in entry.filename:
                    raise ValueError("Unsafe wheel member: " + entry.filename)
                if parts and parts[0].endswith(".data"):
                    if len(parts) < 2 or parts[1] not in ("purelib", "platlib"):
                        if len(parts) > 1 and parts[1] == "scripts":
                            continue  # Backend uses programmatic APIs, no generated CLI tools.
                        raise ValueError("Unsupported wheel data scheme: " + entry.filename)
                    parts = parts[2:]
                destination = target.joinpath(*parts)
                if entry.is_dir():
                    destination.mkdir(parents=True, exist_ok=True)
                else:
                    destination.parent.mkdir(parents=True, exist_ok=True)
                    with archive.open(entry) as src, destination.open("wb") as out:
                        shutil.copyfileobj(src, out)
        manifests.append({"wheel": file.name, "sha256": hashlib.sha256(file.read_bytes()).hexdigest()})
    target.mkdir(parents=True, exist_ok=True)
    (target.parent / "wheel-manifest.json").write_text(json.dumps(manifests, indent=2))
    print(f"Validated and installed {len(manifests)} pinned wheels")


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("requirements", type=Path)
    parser.add_argument("wheelhouse", type=Path)
    parser.add_argument("target", type=Path)
    args = parser.parse_args()
    assemble(args.requirements, args.wheelhouse, args.target)
