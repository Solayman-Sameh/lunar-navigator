# Asset and data attribution

## Scientific data and reference coordinates

Sun/Earth directions come from [NASA/JPL Horizons](https://ssd-api.jpl.nasa.gov/doc/horizons.html).
Coordinate citations and frame/convention notes are retained in
`backend/data/sites.json`. The catalog uses regional reference centers from the
[NASA Moon to Mars Lunar Surface Data Book](https://ntrs.nasa.gov/citations/20250002845),
Table A2.1-1. Citation verification does not certify a touchdown point.

## Global visual maps

Source: **NASA / GSFC Scientific Visualization Studio / LRO / LROC / LOLA**, Ernie
Wright's [CGI Moon Kit](https://svs.gsfc.nasa.gov/4720/).

- [LROC global color map](https://svs.gsfc.nasa.gov/vis/a000000/a004700/a004720/lroc_color_16bit_srgb_4k.tif):
  `Assets/LunarNavigator/Generated/NASA_LROC_4K.tif`.
- [LOLA elevation display map](https://svs.gsfc.nasa.gov/vis/a000000/a004700/a004720/ldem_16_uint.tif):
  `Assets/LunarNavigator/Generated/NASA_LOLA_16.tif`.

The color map is derived from the LROC Hapke-normalized WAC mosaic with polar gap
fill. Display maps and global mesh relief use visual/artistic adjustments. They
are not a validated local terrain skyline and do not drive terrain-obstruction
calculations. Local terrain, rocks and enlarged celestial visuals are illustrative.

## Project-generated content and branding

Procedural fallback textures, local terrain, rocks, stars, Earth/Sun visuals,
interface and application code were produced for this project. Demonstration
Sun/Earth motion is illustrative and is kept distinct from NASA-backed analyses.
Named reference centers in the current catalog are source-cited; custom points
remain unverified.

The project owner supplied the moon/cyan-crosshair application artwork. It is
retained under `Assets/LunarNavigator/Resources/Branding/`; Windows ICO and installer
bitmaps are derived format/size variants. Artwork is branding, not a NASA dataset.

Unity/HDRP and other dependencies retain their respective terms. Packaged runtime
notices are in [deployment/THIRD_PARTY_NOTICES.txt](../deployment/THIRD_PARTY_NOTICES.txt).
No NASA endorsement or mission approval is implied.
