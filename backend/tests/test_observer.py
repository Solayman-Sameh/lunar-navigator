import pytest
from science.coordinates import Horizon, spherical_horizon_deg


def test_exact_two_meter_sphere():
    assert spherical_horizon_deg(1737400,2)==pytest.approx(-0.08693657193373108,abs=1e-8)
    assert spherical_horizon_deg(1737400,0)==0
    assert Horizon('tangent_plane',2).threshold_deg==0
    assert Horizon('spherical_dip',2).threshold_deg<0


@pytest.mark.parametrize('radius,height',[(0,2),(-1,2),(1737400,-2),(float('nan'),2),(1737400,float('inf'))])
def test_bad_sphere_reject(radius,height):
    with pytest.raises(ValueError): spherical_horizon_deg(radius,height)


def test_model_must_be_explicit():
    with pytest.raises(ValueError): Horizon('silent_fallback',2)
