using System;
using LunarNavigator.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace LunarNavigator.UI
{
    public class SkylineChartElement : VisualElement
    {
        private SiteAnalysis currentSite;
        private double currentTimeSec;
        private double landingTimeSec;
        private double missionDurationSec = 72 * 3600;

        private readonly Color colSkylineFill = new Color(0.58f, 0.63f, 0.69f, 0.35f);
        private readonly Color colSkylineLine = new Color(0.60f, 0.65f, 0.72f, 0.90f);
        private readonly Color colIncomplete = new Color(0.96f, 0.42f, 0.26f, 0.30f);
        private readonly Color colGrid = new Color(0.4f, 0.5f, 0.6f, 0.18f);
        private readonly Color colSun = new Color(0.96f, 0.71f, 0.26f, 0.95f);
        private readonly Color colEarth = new Color(0.35f, 0.66f, 0.96f, 0.95f);

        public SkylineChartElement()
        {
            style.height = 160;
            style.width = Length.Percent(100);
            generateVisualContent += OnGenerateVisualContent;
        }

        public void SetData(SiteAnalysis site, double currentSec, double landingSec, double durationHours)
        {
            currentSite = site;
            currentTimeSec = currentSec;
            landingTimeSec = landingSec;
            missionDurationSec = durationHours * 3600.0;
            MarkDirtyRepaint();
        }

        public void SetCurrentTime(double currentSec)
        {
            currentTimeSec = currentSec;
            MarkDirtyRepaint();
        }

        private static void AddRectPath(Painter2D painter, float x, float y, float w, float h)
        {
            painter.MoveTo(new Vector2(x, y));
            painter.LineTo(new Vector2(x + w, y));
            painter.LineTo(new Vector2(x + w, y + h));
            painter.LineTo(new Vector2(x, y + h));
            painter.ClosePath();
        }

        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            float width = resolvedStyle.width;
            float height = resolvedStyle.height;
            if (width < 20f || height < 20f) return;

            var painter = context.painter2D;

            // Plot background
            painter.fillColor = new Color(0.06f, 0.08f, 0.12f, 0.92f);
            painter.BeginPath();
            AddRectPath(painter, 0, 0, width, height);
            painter.Fill();

            if (currentSite?.terrain?.horizon_el_deg == null || currentSite.terrain.horizon_el_deg.Length != 360)
                return;

            float padL = 24f, padR = 12f, padT = 12f, padB = 20f;
            float plotW = width - padL - padR;
            float plotH = height - padT - padB;

            float minEl = -2f;
            float maxEl = 10f;

            // Expand maxEl if skyline reaches higher
            foreach (var h in currentSite.terrain.horizon_el_deg)
            {
                if (h > maxEl) maxEl = Mathf.Min(20f, (float)h + 2f);
            }

            Func<float, float> azToX = az => padL + (Mathf.Repeat(az, 360f) / 360f) * plotW;
            Func<float, float> elToY = el => padT + plotH - ((el - minEl) / (maxEl - minEl)) * plotH;

            // Elevation Grid Lines
            painter.strokeColor = colGrid;
            painter.lineWidth = 1f;
            for (float el = 0f; el <= maxEl; el += 2f)
            {
                float gy = elToY(el);
                painter.BeginPath();
                painter.MoveTo(new Vector2(padL, gy));
                painter.LineTo(new Vector2(padL + plotW, gy));
                painter.Stroke();
            }

            // Incomplete sectors hatched bands
            if (currentSite.terrain.incomplete_sectors != null)
            {
                painter.fillColor = colIncomplete;
                foreach (var sec in currentSite.terrain.incomplete_sectors)
                {
                    float x1 = azToX((float)sec.az_from_deg);
                    float x2 = azToX((float)sec.az_to_deg);
                    painter.BeginPath();
                    AddRectPath(painter, x1, padT, Mathf.Max(2f, x2 - x1), plotH);
                    painter.Fill();
                }
            }

            // Draw Skyline fill & line
            double[] h360 = currentSite.terrain.horizon_el_deg;
            float baseY = elToY(minEl);

            painter.fillColor = colSkylineFill;
            painter.BeginPath();
            painter.MoveTo(new Vector2(padL, baseY));
            for (int deg = 0; deg < 360; deg++)
            {
                float x = azToX(deg);
                float y = elToY((float)h360[deg]);
                painter.LineTo(new Vector2(x, y));
            }
            painter.LineTo(new Vector2(padL + plotW, elToY((float)h360[0])));
            painter.LineTo(new Vector2(padL + plotW, baseY));
            painter.ClosePath();
            painter.Fill();

            // Skyline outline
            painter.strokeColor = colSkylineLine;
            painter.lineWidth = 1.5f;
            painter.BeginPath();
            painter.MoveTo(new Vector2(padL, elToY((float)h360[0])));
            for (int deg = 1; deg < 360; deg++)
            {
                painter.LineTo(new Vector2(azToX(deg), elToY((float)h360[deg])));
            }
            painter.LineTo(new Vector2(padL + plotW, elToY((float)h360[0])));
            painter.Stroke();

            // Mission window Sun & Earth tracks
            if (currentSite.ephemeris != null && currentSite.ephemeris.n > 0)
            {
                int step = currentSite.ephemeris.step_s;
                int startIdx = Mathf.Clamp((int)Math.Floor(landingTimeSec / step), 0, currentSite.ephemeris.n - 1);
                int count = Mathf.Clamp((int)Math.Ceiling(missionDurationSec / step), 1, currentSite.ephemeris.n - startIdx);

                DrawTrack(painter, currentSite.ephemeris.sun_az_deg, currentSite.ephemeris.sun_el_deg, startIdx, count, azToX, elToY, colSun);
                DrawTrack(painter, currentSite.ephemeris.earth_az_deg, currentSite.ephemeris.earth_el_deg, startIdx, count, azToX, elToY, colEarth);

                // Live Playhead Sun and Earth markers
                int curIdx = Mathf.Clamp((int)Math.Floor(currentTimeSec / step), 0, currentSite.ephemeris.n - 1);
                float curSunAz = (float)currentSite.ephemeris.sun_az_deg[curIdx];
                float curSunEl = (float)currentSite.ephemeris.sun_el_deg[curIdx];
                bool sunVis = VisibilityMath.SunVisibleAt(currentSite, curIdx);
                DrawMarker(painter, azToX(curSunAz), elToY(curSunEl), colSun, sunVis);

                float curEarthAz = (float)currentSite.ephemeris.earth_az_deg[curIdx];
                float curEarthEl = (float)currentSite.ephemeris.earth_el_deg[curIdx];
                bool earthVis = VisibilityMath.EarthVisibleAt(currentSite, curIdx);
                DrawMarker(painter, azToX(curEarthAz), elToY(curEarthEl), colEarth, earthVis);
            }
        }

        private void DrawTrack(Painter2D painter, double[] azArr, double[] elArr, int startIdx, int count, Func<float, float> azToX, Func<float, float> elToY, Color col)
        {
            if (azArr == null || elArr == null || count <= 1) return;

            painter.strokeColor = col;
            painter.lineWidth = 1.2f;

            bool drawing = false;
            float prevAz = 0f;

            for (int j = 0; j < count; j++)
            {
                int idx = startIdx + j;
                float az = (float)azArr[idx];
                float el = (float)elArr[idx];

                if (!drawing)
                {
                    painter.BeginPath();
                    painter.MoveTo(new Vector2(azToX(az), elToY(el)));
                    drawing = true;
                }
                else
                {
                    if (Mathf.Abs(az - prevAz) > 180f)
                    {
                        painter.Stroke();
                        painter.BeginPath();
                        painter.MoveTo(new Vector2(azToX(az), elToY(el)));
                    }
                    else
                    {
                        painter.LineTo(new Vector2(azToX(az), elToY(el)));
                    }
                }
                prevAz = az;
            }

            if (drawing) painter.Stroke();
        }

        private void DrawMarker(Painter2D painter, float cx, float cy, Color col, bool isVisible)
        {
            float r = 4.5f;
            if (isVisible)
            {
                painter.fillColor = col;
                painter.BeginPath();
                painter.Arc(new Vector2(cx, cy), r, 0, 360);
                painter.Fill();
            }
            else
            {
                painter.strokeColor = col;
                painter.lineWidth = 1.5f;
                painter.BeginPath();
                painter.Arc(new Vector2(cx, cy), r, 0, 360);
                painter.Stroke();
            }
        }
    }
}
