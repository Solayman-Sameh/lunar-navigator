"""Catalog validation. Test coordinates cannot become verified production sites."""
from dataclasses import dataclass
import math
from .models import Observer

SITE_IDS = {"malapert_mountain", "shackleton_rim", "connecting_ridge"}


def normalize_longitude(value):
    if not math.isfinite(value):
        raise ValueError("finite longitude required")
    return (value + 180.0) % 360.0 - 180.0


@dataclass(frozen=True)
class SiteRecord:
    id: str
    name: str
    lat_deg: float | None
    lon_east_deg: float | None
    verified: bool
    point_source: str
    coordinate_frame: str
    coordinate_convention: str = 'degrees; north-positive latitude, east-positive longitude [-180,180]'
    reference_description: str = ''
    verification_scope: str = 'coordinate citation only; no touchdown, ground-height or terrain-safety certification'

    def require_verified(self):
        if self.verified is not True or self.lat_deg is None or self.lon_east_deg is None:
            raise ValueError(f"unverified production point: {self.id}")
        if self.id not in SITE_IDS or not self.point_source or not self.coordinate_frame:
            raise ValueError("verified ID, exact-point source and frame required")
        Observer(self.lat_deg, self.lon_east_deg, 0.002, frame=self.coordinate_frame)
        return self


def load_catalog(rows):
    result = {}
    for row in rows:
        site = SiteRecord(**row)
        if site.id in result:
            raise ValueError(f"duplicate site ID: {site.id}")
        if site.id not in SITE_IDS or type(site.verified) is not bool:
            raise ValueError("unknown site ID or invalid verified flag")
        if site.verified:
            site.require_verified()
        result[site.id] = site
    return result


def coordinate_mismatch(science, asset, tolerance_deg=.05):
    return abs(science[0]-asset[0]) > tolerance_deg or abs(normalize_longitude(science[1]-asset[1])) > tolerance_deg


def get_sites_json_path():
    import os, sys
    from pathlib import Path
    env_override = os.environ.get("LUNAR_SITES_JSON")
    if env_override:
        p = Path(env_override).resolve()
        if p.is_file():
            return p
    backend_root = Path(__file__).resolve().parents[1]
    candidate = backend_root / 'data' / 'sites.json'
    if candidate.is_file():
        return candidate
    exe_dir = Path(sys.executable).resolve().parent
    for sub in [exe_dir / 'data' / 'sites.json', exe_dir / 'backend' / 'data' / 'sites.json']:
        if sub.is_file():
            return sub
    raise FileNotFoundError(f"Authoritative sites.json could not be located at {candidate} or fallbacks.")


def supported_catalog():
    import json
    return load_catalog(json.loads(get_sites_json_path().read_text(encoding='utf-8')))
