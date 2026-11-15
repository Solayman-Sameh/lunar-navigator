import json
from dataclasses import replace
import pytest
from science.horizons import parse_response, SourceError


def test_named_heading_not_numeric_position(query, synthetic_envelope):
    track = parse_response(synthetic_envelope(), query)
    assert [r.az_deg for r in track.rows] == [359.9, 0.0, 0.1]
    assert [r.el_deg for r in track.rows] == [1.125, 0.0, -1.25]
    assert track.header[3] == "decoy"


@pytest.mark.parametrize("old,new,reason", [
    ('"1.2"', '"9.9"', "signature"),
    ('NASA/JPL Horizons API', 'unknown', "signature"),
    ('$$SOE', '$$BAD', "markers"),
    ('$$EOE', '$$EOE\\n$$EOE', "markers"),
    ('1.125', 'n.a.', "angle"),
    ('1.125', 'NaN', "finite"),
    ('1.125', 'Infinity', "finite"),
    ('359.9', '361.1', "azimuth"),
    ('-1.25', '-91.0', "elevation"),
    ('Moon (301)', 'Earth (399)', "center"),
    ('Sun (10)', 'Earth (399)', "target"),
    ('0.0, -89.5, .002', '0.0, -89.6, .002', "observer"),
    ('0.0, -89.5, .002', 'NaN, -89.5, .002', "observer"),
    ('NO (AIRLESS)', 'YES (REFRACTED)', "airless"),
    ('Date__(UT)__', 'Date__(TT)__', "heading"),
    ('Azimuth_(a-app)', 'Azimuth_(r-app)', "heading"),
    ('MEAN_ME', 'IAU_EARTH', "frame"),
    ('60 minutes', '10 minutes', "step"),
])
def test_bad_source_rejected(query, synthetic_envelope, old, new, reason):
    raw = synthetic_envelope().decode().replace(old, new).encode()
    with pytest.raises(SourceError, match=reason):
        parse_response(raw, query)


def test_error_envelope_rejects_even_http_200(query):
    with pytest.raises(SourceError, match="API error"):
        parse_response(b'{"error":"invalid parameter"}', query)


def test_wrong_requested_height_rejects(query, synthetic_envelope):
    wrong = replace(query, observer=replace(query.observer, altitude_km=0.005))
    with pytest.raises(SourceError, match="observer"):
        parse_response(synthetic_envelope(), wrong)


def test_ambiguous_heading_rejects(query, synthetic_envelope):
    raw = synthetic_envelope().decode().replace('decoy', 'Azimuth_(a-app)').encode()
    with pytest.raises(SourceError, match="heading"):
        parse_response(raw, query)


def test_reversed_markers_reject(query, synthetic_envelope):
    raw = synthetic_envelope().decode().replace('$$SOE', '$$TEMP').replace('$$EOE', '$$SOE').replace('$$TEMP', '$$EOE').encode()
    with pytest.raises(SourceError, match='markers'):
        parse_response(raw, query)
