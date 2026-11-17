# Horizons regression fixtures

`tests/conftest.py` generates **synthetic** parser fixtures with reordered columns
and deliberate numeric decoys. They are not NASA ephemerides or runtime fallback
data. The parser validates the supported source profile, headings, observer and
timestamps; it must not relabel an unreviewed source version as a supported one.

`real-diagnostic/` contains the separate, genuinely retained Sun/Earth pair acquired
in November 2026. Its README documents coverage, coordinate conventions, original
raw-byte hashes and portability adjustments to receipt paths. Real-data tests
reparse those originals and verify the saved derived outputs. This retained fixture
supersedes the earlier development note that described only a browser observation.
