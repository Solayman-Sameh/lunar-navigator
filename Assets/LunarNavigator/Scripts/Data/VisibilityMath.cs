using System;
using System.Collections.Generic;

namespace LunarNavigator.Data
{
    public struct Metrics
    {
        public string start_utc;
        public double duration_h;
        public double f_sun;
        public double f_earth;
        public double f_both;
        public double dark_max_h;
        public double comm_gap_max_h;
    }

    public static class VisibilityMath
    {
        public static double SkylineAt(double[] h360, double az)
        {
            if (h360 == null || h360.Length == 0) return 0.0;
            double a = az % 360.0;
            if (a < 0.0) a += 360.0;

            int i0 = (int)a;
            int i1 = (i0 + 1) % 360;
            if (i0 >= h360.Length) i0 = h360.Length - 1;
            if (i1 >= h360.Length) i1 = 0;

            double t = a - (int)a;
            return (1.0 - t) * h360[i0] + t * h360[i1];
        }

        public static float SkylineAt(double[] h360, float az)
        {
            return (float)SkylineAt(h360, (double)az);
        }

        public static float SkylineAt(float[] h360, float az)
        {
            if (h360 == null || h360.Length == 0) return 0f;
            float a = az % 360f;
            if (a < 0f) a += 360f;

            int i0 = (int)a;
            int i1 = (i0 + 1) % 360;
            if (i0 >= h360.Length) i0 = h360.Length - 1;
            if (i1 >= h360.Length) i1 = 0;

            float t = a - (int)a;
            return (1f - t) * h360[i0] + t * h360[i1];
        }

        public static bool IsIncompleteSector(SiteAnalysis site, double az)
        {
            if (site?.terrain?.incomplete_sectors == null || site.terrain.incomplete_sectors.Length == 0)
                return false;
            double a = az % 360.0;
            if (a < 0.0) a += 360.0;
            foreach (var sec in site.terrain.incomplete_sectors)
            {
                if (sec == null) continue;
                if (sec.az_from_deg <= sec.az_to_deg)
                {
                    if (a >= sec.az_from_deg && a <= sec.az_to_deg) return true;
                }
                else
                {
                    if (a >= sec.az_from_deg || a <= sec.az_to_deg) return true;
                }
            }
            return false;
        }

        public static bool SunVisibleAt(SiteAnalysis site, int i)
        {
            if (site?.visibility?.sun_visible == null || i < 0 || i >= site.visibility.sun_visible.Length)
                return false;
            return site.visibility.sun_visible[i] == 1;
        }

        public static bool SunVisibleComputed(SiteAnalysis site, int i)
        {
            if (site?.ephemeris == null || site.terrain == null || i < 0 || i >= site.ephemeris.n)
                return false;
            double az = site.ephemeris.sun_az_deg[i];
            double el = site.ephemeris.sun_el_deg[i];
            double horizon = SkylineAt(site.terrain.horizon_el_deg, az) + (site.visibility?.sun_margin_deg ?? 0.0);
            return el > horizon;
        }

        public static bool EarthVisibleAt(SiteAnalysis site, int i)
        {
            if (site?.visibility?.earth_visible == null || i < 0 || i >= site.visibility.earth_visible.Length)
                return false;
            return site.visibility.earth_visible[i] == 1;
        }

        public static bool EarthVisibleComputed(SiteAnalysis site, int i)
        {
            if (site?.ephemeris == null || site.terrain == null || i < 0 || i >= site.ephemeris.n)
                return false;
            double az = site.ephemeris.earth_az_deg[i];
            double el = site.ephemeris.earth_el_deg[i];
            double horizon = SkylineAt(site.terrain.horizon_el_deg, az);
            return el > horizon;
        }

        public static Metrics Compute(SiteAnalysis site, int startIdx, int sampleCount)
        {
            if (site?.ephemeris == null || site.visibility?.sun_visible == null || site.visibility?.earth_visible == null)
                throw new ArgumentNullException(nameof(site));

            if (startIdx < 0 || sampleCount <= 0 || startIdx + sampleCount > site.ephemeris.n - 1)
            {
                throw new ArgumentOutOfRangeException(
                    $"Window [{startIdx}..{startIdx + sampleCount}) exceeds available samples ({site.ephemeris.n}).");
            }

            int sunSum = 0;
            int earthSum = 0;
            int bothSum = 0;
            int maxDark = 0;
            int curDark = 0;
            int maxCommGap = 0;
            int curCommGap = 0;

            int[] sunVis = site.visibility.sun_visible;
            int[] earthVis = site.visibility.earth_visible;

            for (int j = 0; j < sampleCount; j++)
            {
                int idx = startIdx + j;
                int s = sunVis[idx];
                int e = earthVis[idx];

                if (s == 1)
                {
                    sunSum++;
                    curDark = 0;
                }
                else
                {
                    curDark++;
                    if (curDark > maxDark) maxDark = curDark;
                }

                if (e == 1)
                {
                    earthSum++;
                    curCommGap = 0;
                }
                else
                {
                    curCommGap++;
                    if (curCommGap > maxCommGap) maxCommGap = curCommGap;
                }

                if (s == 1 && e == 1)
                {
                    bothSum++;
                }
            }

            double stepHours = site.ephemeris.step_s / 3600.0;
            double durationHours = sampleCount * stepHours;

            DateTime t0 = DateTime.Parse(site.ephemeris.t0_utc, null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal);
            DateTime windowStart = t0.AddSeconds(startIdx * site.ephemeris.step_s);

            return new Metrics
            {
                start_utc = windowStart.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                duration_h = durationHours,
                f_sun = (double)sunSum / sampleCount,
                f_earth = (double)earthSum / sampleCount,
                f_both = (double)bothSum / sampleCount,
                dark_max_h = maxDark * stepHours,
                comm_gap_max_h = maxCommGap * stepHours
            };
        }

        public static Metrics ComputeForUtc(SiteAnalysis site, DateTime landingUtc, double durationHours)
        {
            if (site?.ephemeris == null) throw new ArgumentNullException(nameof(site));

            DateTime t0 = DateTime.Parse(site.ephemeris.t0_utc, null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal);
            double deltaSec = (landingUtc.ToUniversalTime() - t0).TotalSeconds;
            if (double.IsNaN(durationHours) || double.IsInfinity(durationHours) || durationHours <= 0 ||
                deltaSec % site.ephemeris.step_s != 0 || (durationHours * 3600) % site.ephemeris.step_s != 0)
                throw new ArgumentException("Mission must align exactly to complete source cells.");
            int startIdx = (int)(deltaSec / site.ephemeris.step_s);
            int sampleCount = (int)Math.Round(durationHours * 3600.0 / site.ephemeris.step_s);

            return Compute(site, startIdx, sampleCount);
        }

        public static List<(int start, int endExcl)> Windows(int[] mask)
        {
            var list = new List<(int start, int endExcl)>();
            if (mask == null || mask.Length == 0) return list;

            int runStart = -1;
            for (int i = 0; i < mask.Length; i++)
            {
                if (mask[i] == 1)
                {
                    if (runStart < 0) runStart = i;
                }
                else
                {
                    if (runStart >= 0)
                    {
                        list.Add((runStart, i));
                        runStart = -1;
                    }
                }
            }

            if (runStart >= 0)
            {
                list.Add((runStart, mask.Length));
            }

            return list;
        }

        public static int NextLoss(int[] mask, int currentIdx)
        {
            if (mask == null || currentIdx < 0 || currentIdx >= mask.Length) return -1;
            if (mask[currentIdx] == 0) return 0; // Already lost

            for (int i = currentIdx + 1; i < mask.Length; i++)
            {
                if (mask[i] == 0) return i - currentIdx;
            }
            return -1; // Never lost within the dataset
        }
    }
}
