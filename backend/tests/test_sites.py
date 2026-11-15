import pytest
from science.models import Observer
from science.sites import SiteRecord, load_catalog, normalize_longitude, coordinate_mismatch


def test_longitude_wrap():
    assert normalize_longitude(270) == -90
    assert normalize_longitude(360) == 0
    assert normalize_longitude(-181) == 179


@pytest.mark.parametrize("lat", [-90, 90, 91, float("nan")])
def test_poles_and_invalid_latitude_reject(lat):
    with pytest.raises(ValueError):
        Observer(lat, 0, 0.002)


def test_near_pole_preserved_with_warning():
    with pytest.warns(UserWarning, match="pole"):
        observer = Observer(-89.96, 0, 0.002)
    assert observer.lat_deg == -89.96


def test_unverified_point_cannot_be_production():
    site = SiteRecord("shackleton_rim", "Unverified", None, None, False, "", "")
    with pytest.raises(ValueError, match="unverified"):
        site.require_verified()


def test_verification_needs_source_and_frame():
    with pytest.raises(ValueError, match="source|frame"):
        SiteRecord("shackleton_rim", "Site", -89.5, 0, True, "", "").require_verified()


def test_duplicate_catalog_ids_reject():
    row = dict(id="shackleton_rim", name="S", lat_deg=None, lon_east_deg=None,
               verified=False, point_source="", coordinate_frame="")
    with pytest.raises(ValueError, match="duplicate"):
        load_catalog([row, row])


def test_coordinate_disagreement_is_reported():
    assert coordinate_mismatch((-89.5, 0), (-89.6, 0))
    assert not coordinate_mismatch((-89.5, 179.99), (-89.5, -179.99))
