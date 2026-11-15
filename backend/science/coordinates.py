"""Explicit reference-sphere geometry; no terrain or atmosphere."""
from dataclasses import dataclass
import math


def spherical_horizon_deg(radius_m, height_m):
    if not all(math.isfinite(x) for x in (radius_m,height_m)) or radius_m<=0 or height_m<0:
        raise ValueError('positive finite radius and nonnegative finite mast height required')
    return -math.degrees(math.acos(radius_m/(radius_m+height_m)))


@dataclass(frozen=True)
class Horizon:
    convention: str
    observer_height_m: float
    radius_m: float = 1737400.0

    def __post_init__(self):
        if self.convention not in ('tangent_plane','spherical_dip'):
            raise ValueError('choose tangent_plane or spherical_dip explicitly')
        spherical_horizon_deg(self.radius_m,self.observer_height_m)

    @property
    def threshold_deg(self):
        return 0.0 if self.convention=='tangent_plane' else spherical_horizon_deg(self.radius_m,self.observer_height_m)
