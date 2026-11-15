# Asset provenance

## Global lunar color
NASA Scientific Visualization Studio, **CGI Moon Kit**, Ernie Wright,
https://svs.gsfc.nasa.gov/4720/ . Downloaded 2026-11-15 using the OS HTTP client
inside the Unity Editor after UnityWebRequest failed TLS negotiation.

Direct file: https://svs.gsfc.nasa.gov/vis/a000000/a004700/a004720/lroc_color_16bit_srgb_4k.tif
4096 x 2048, 16-bit sRGB, published 2025 color version, approximately 59 MB source.
Source texture retained in Assets/LunarNavigator/Generated/NASA_LROC_4K.tif.

Credits: NASA / GSFC Scientific Visualization Studio / LRO / LROC camera team.
Map adapted from the LROC Hapke-normalized WAC mosaic; polar gaps are filled from
LOLA albedo. NASA describes these display maps as optimized for aesthetics rather
than science. This application is not a scientific planning product.

The map is centered on zero longitude. Sphere UV maps zero longitude to u=0.5,
east-positive longitude to increasing u, north to v=1. Unity image import provides
the corresponding bottom-left texture convention.

## Additional reference inspected
NASA LOLA global elevation display map: https://svs.gsfc.nasa.gov/vis/a000000/a004700/a004720/ldem_16_uint.tif
Downloaded from the same CGI Moon Kit page, approximately 33 MB source. Imported at
4096 maximum resolution. It drives the global mesh relief and normal mapping with
an artistic vertical scale. It does not drive the representative local terrain.

https://svs.gsfc.nasa.gov/14959/ was retrieved and preserved as
NASA-14959-dotnet.html. No assets from it are falsely claimed as integrated.

## Original generated content
Procedural globe fallback, synthetic crater relief, local terrain, rocks, star geometry,
Earth texture, Sun disc, interface, scripts and configuration were generated for this
project. Local terrain is illustrative. Candidate coordinates and names are fictional.
Sun/Earth motion is deterministic demonstration data, not JPL Horizons ephemerides.
Earth/Sun apparent sizes and distances are artistic approximations.

The failed initial UnityWebRequest source responses are retained for diagnostic history.
The successful NASA-dotnet.txt response is the retrieved CGI Moon Kit page.
