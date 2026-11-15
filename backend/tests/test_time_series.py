from dataclasses import replace
from datetime import timedelta
import pytest
from science.horizons import parse_response, validate_rows, align_tracks, merge_rows, SourceError
from science.models import parse_utc


def test_exact_endpoint_inclusive(query, synthetic_envelope):
    track = parse_response(synthetic_envelope(), query)
    assert len(track.rows) == 3
    assert track.rows[0].utc == query.start
    assert track.rows[-1].utc == query.stop


@pytest.mark.parametrize("kind", ["missing", "duplicate", "off_grid", "reverse"])
def test_bad_timestamps_reject(query, synthetic_envelope, kind):
    rows = list(parse_response(synthetic_envelope(), query).rows)
    if kind == "missing": rows.pop(1)
    if kind == "duplicate": rows[1] = rows[0]
    if kind == "off_grid": rows[1] = replace(rows[1], utc=rows[1].utc + timedelta(seconds=1))
    if kind == "reverse": rows.reverse()
    with pytest.raises(SourceError, match="count|timestamp|cadence"):
        validate_rows(rows, query)


def test_pairing_never_intersects_missing_rows(query, synthetic_envelope):
    sun = parse_response(synthetic_envelope(), query)
    earth = parse_response(synthetic_envelope(399), replace(query, target=399))
    assert len(align_tracks(sun, earth)) == 3
    with pytest.raises(SourceError, match="pair"):
        align_tracks(sun, replace(earth, rows=earth.rows[:-1]))


def test_chunk_equal_boundary_dedup_conflict_reject(query, synthetic_envelope):
    rows = parse_response(synthetic_envelope(), query).rows
    assert merge_rows([rows[:2], rows[1:]]) == rows
    with pytest.raises(SourceError, match="conflicting"):
        merge_rows([rows[:2], (replace(rows[1], el_deg=9), rows[2])])


@pytest.mark.parametrize("text", ['2026-12-01', '2026-12-01T00:00:00+02:00', '2026-12-01T00:00:60Z'])
def test_noncanonical_or_leap_seconds_reject(text):
    with pytest.raises(ValueError): parse_utc(text)


def test_ten_minute_and_longer_windows_supported(query):
    longer = replace(query, stop=parse_utc('2027-01-01T00:00:00Z'), step_s=600)
    assert longer.expected_count == 4465


def test_known_leap_insertion_rejected(query):
    with pytest.raises(ValueError, match="leap"):
        replace(query, start=parse_utc('2016-12-31T00:00:00Z'), stop=parse_utc('2017-01-01T00:00:00Z'))
