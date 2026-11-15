"""Explicit diagnostic observer/request identities; UTC and binary64 values."""
from dataclasses import dataclass
from datetime import datetime, timezone
import math
import re
import warnings
from . import __version__

ENDPOINT = "https://ssd.jpl.nasa.gov/api/horizons.api"


def parse_utc(text):
    if not re.fullmatch(r"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z", text):
        raise ValueError("explicit whole-second UTC YYYY-MM-DDTHH:MM:SSZ required")
    return datetime.strptime(text, "%Y-%m-%dT%H:%M:%SZ").replace(tzinfo=timezone.utc)


def utc_text(value):
    return value.astimezone(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


@dataclass(frozen=True)
class Observer:
    lat_deg: float
    lon_east_deg: float
    altitude_km: float
    frame: str = "MEAN_ME (high precision)"
    model: str = "nominal-Horizons-1737.4km-reference-sphere"
    coordinate_status: str = "USER_SUPPLIED_TEST_POINT_NOT_VERIFIED_LANDING_SITE"

    def __post_init__(self):
        if not all(math.isfinite(x) for x in (self.lat_deg, self.lon_east_deg, self.altitude_km)):
            raise ValueError("finite observer coordinates required")
        if not -90 < self.lat_deg < 90 or not -180 <= self.lon_east_deg <= 180:
            raise ValueError("invalid latitude/pole or east longitude")
        if abs(self.lat_deg) >= 89.95:
            warnings.warn("near-pole azimuth conditioning; exact stated coordinate retained", UserWarning)


@dataclass(frozen=True)
class HorizonsRequest:
    target: int
    observer: Observer
    start: datetime
    stop: datetime
    step_s: int
    algorithm_version: str = __version__
    endpoint: str = ENDPOINT

    def __post_init__(self):
        if self.target not in (10, 399) or self.endpoint != ENDPOINT:
            raise ValueError("only Sun/Earth at the documented NASA endpoint supported")
        for value in (self.start, self.stop):
            if value.utcoffset() is None or value.utcoffset().total_seconds() != 0 or value.microsecond:
                raise ValueError("explicit whole-second UTC required")
            if value.year < 1962:
                raise ValueError("pre-1962 UT1 is outside supported UTC dates")
        if type(self.step_s) is not int or self.step_s <= 0 or self.step_s % 60:
            raise ValueError("positive whole-minute step required")
        duration = (self.stop - self.start).total_seconds()
        if duration <= 0 or duration % self.step_s:
            raise ValueError("positive stop/start interval aligned to step required")
        # Known leap insertions since 1972. Future source leap assumptions are
        # retained in source headers; this does not certify future leap policy.
        leap_dates = ('1972-07-01','1973-01-01','1974-01-01','1975-01-01','1976-01-01',
                      '1977-01-01','1978-01-01','1979-01-01','1980-01-01','1981-07-01',
                      '1982-07-01','1983-07-01','1985-07-01','1988-01-01','1990-01-01',
                      '1991-01-01','1992-07-01','1993-07-01','1994-07-01','1996-01-01',
                      '1997-07-01','1999-01-01','2006-01-01','2009-01-01','2012-07-01',
                      '2015-07-01','2017-01-01')
        if any(self.start < parse_utc(day+'T00:00:00Z') <= self.stop for day in leap_dates):
            raise ValueError("unsupported leap insertion interval")

    @property
    def expected_count(self):
        return int((self.stop - self.start).total_seconds()) // self.step_s + 1

    def params(self):
        quote = lambda x: "'" + str(x) + "'"
        o = self.observer
        return {"format": "json", "COMMAND": quote(self.target), "OBJ_DATA": quote("NO"),
                "MAKE_EPHEM": quote("YES"), "EPHEM_TYPE": quote("OBSERVER"),
                "CENTER": quote("coord@301"), "COORD_TYPE": quote("GEODETIC"),
                "SITE_COORD": quote(f"{o.lon_east_deg},{o.lat_deg},{o.altitude_km}"),
                "START_TIME": quote(self.start.strftime('%Y-%m-%d %H:%M:%S')),
                "STOP_TIME": quote(self.stop.strftime('%Y-%m-%d %H:%M:%S')),
                "STEP_SIZE": quote(f"{self.step_s//60} m"), "QUANTITIES": quote("4"),
                "CSV_FORMAT": quote("YES"), "APPARENT": quote("AIRLESS"),
                "ANG_FORMAT": quote("DEG"), "TIME_DIGITS": quote("SECONDS"),
                "TIME_TYPE": quote("UT"), "TIME_ZONE": quote("+00:00"),
                "CAL_FORMAT": quote("CAL"), "ELEV_CUT": quote("-90"),
                "SKIP_DAYLT": quote("NO"), "EXTRA_PREC": quote("YES")}


@dataclass(frozen=True)
class TrackRow:
    utc: datetime
    az_deg: float
    el_deg: float


@dataclass(frozen=True)
class EphemerisTrack:
    request: HorizonsRequest
    rows: tuple
    header: tuple
    metadata: dict
    raw_sha256: str
