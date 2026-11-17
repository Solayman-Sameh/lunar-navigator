using System;
using LunarNavigator.Data;
using UnityEngine;

namespace LunarNavigator
{
    public sealed class IllustrativeDemoProvider : ILunarEphemeris
    {
        public string Label => "Illustrative Demo Provider";
        public Vector3 SunDirection(DateTime utc)
        {
            float phase = (float)((utc.ToUniversalTime() - DemoSimulation.Epoch).TotalDays / 29.53059 * Math.PI * 2);
            return new Vector3(Mathf.Cos(phase) * .94f, -.22f, Mathf.Sin(phase) * .94f + .35f).normalized;
        }
        public Vector3 EarthDirection(DateTime utc)
        {
            float p = (float)((utc.ToUniversalTime() - DemoSimulation.Epoch).TotalDays / 27.32 * Math.PI * 2);
            return new Vector3(1, .06f * Mathf.Sin(p), .08f * Mathf.Cos(p)).normalized;
        }
    }

    public sealed class DataDrivenEphemerisProvider : ILunarEphemeris
    {
        public string Label => "Scientific Ephemeris Provider";
        private readonly IllustrativeDemoProvider fallback = new IllustrativeDemoProvider();
        private readonly LunarApplication app;

        public DataDrivenEphemerisProvider(LunarApplication application)
        {
            app = application;
        }

        public Vector3 SunDirection(DateTime utc)
        {
            var repo = AnalysisRepository.Instance;
            if (!repo.HasData) return fallback.SunDirection(utc);

            var siteData = repo.GetSite(app?.SelectedSite?.id);
            if (siteData?.ephemeris == null || siteData.ephemeris.n <= 0)
                return fallback.SunDirection(utc);

            int step = siteData.ephemeris.step_s;
            DateTime t0 = repo.T0;
            double sec = (utc.ToUniversalTime() - t0).TotalSeconds;
            double sample = sec / step;
            int i0 = Mathf.Clamp((int)Math.Floor(sample), 0, siteData.ephemeris.n - 1);
            int i1 = Mathf.Clamp(i0 + 1, 0, siteData.ephemeris.n - 1);
            float t = (float)(sample - i0);

            float az = Mathf.LerpAngle((float)siteData.ephemeris.sun_az_deg[i0], (float)siteData.ephemeris.sun_az_deg[i1], t);
            float el = Mathf.Lerp((float)siteData.ephemeris.sun_el_deg[i0], (float)siteData.ephemeris.sun_el_deg[i1], t);

            Vector3 siteNormal = app?.SelectedSite?.Direction ?? LunarCoordinates.Direction(siteData.site.lat_deg, siteData.site.lon_east_deg);
            Quaternion frame = LunarCoordinates.Frame(siteNormal);
            Vector3 north = frame * Vector3.forward;
            Vector3 east = frame * Vector3.right;
            Vector3 up = frame * Vector3.up;

            float azRad = az * Mathf.Deg2Rad;
            float elRad = el * Mathf.Deg2Rad;
            Vector3 dir = Mathf.Cos(elRad) * (Mathf.Cos(azRad) * north + Mathf.Sin(azRad) * east) + Mathf.Sin(elRad) * up;
            return dir.normalized;
        }

        public Vector3 EarthDirection(DateTime utc)
        {
            var repo = AnalysisRepository.Instance;
            if (!repo.HasData) return fallback.EarthDirection(utc);

            var siteData = repo.GetSite(app?.SelectedSite?.id);
            if (siteData?.ephemeris == null || siteData.ephemeris.n <= 0)
                return fallback.EarthDirection(utc);

            int step = siteData.ephemeris.step_s;
            DateTime t0 = repo.T0;
            double sec = (utc.ToUniversalTime() - t0).TotalSeconds;
            double sample = sec / step;
            int i0 = Mathf.Clamp((int)Math.Floor(sample), 0, siteData.ephemeris.n - 1);
            int i1 = Mathf.Clamp(i0 + 1, 0, siteData.ephemeris.n - 1);
            float t = (float)(sample - i0);

            float az = Mathf.LerpAngle((float)siteData.ephemeris.earth_az_deg[i0], (float)siteData.ephemeris.earth_az_deg[i1], t);
            float el = Mathf.Lerp((float)siteData.ephemeris.earth_el_deg[i0], (float)siteData.ephemeris.earth_el_deg[i1], t);

            Vector3 siteNormal = app?.SelectedSite?.Direction ?? LunarCoordinates.Direction(siteData.site.lat_deg, siteData.site.lon_east_deg);
            Quaternion frame = LunarCoordinates.Frame(siteNormal);
            Vector3 north = frame * Vector3.forward;
            Vector3 east = frame * Vector3.right;
            Vector3 up = frame * Vector3.up;

            float azRad = az * Mathf.Deg2Rad;
            float elRad = el * Mathf.Deg2Rad;
            Vector3 dir = Mathf.Cos(elRad) * (Mathf.Cos(azRad) * north + Mathf.Sin(azRad) * east) + Mathf.Sin(elRad) * up;
            return dir.normalized;
        }
    }

    public class DemoSimulation : MonoBehaviour
    {
        public static readonly DateTime Epoch = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc);
        public const double Duration = 30 * 86400;
        public double seconds;
        public bool paused = true;
        public double speed = 3600;

        public ILunarEphemeris Provider { get; private set; } = new IllustrativeDemoProvider();

        public DateTime StartTime => AnalysisRepository.Instance.HasData ? AnalysisRepository.Instance.T0 : Epoch;
        public double TotalDuration => AnalysisRepository.Instance.HasData && AnalysisRepository.Instance.Count > 1
            ? (AnalysisRepository.Instance.Count - 1) * (double)AnalysisRepository.Instance.StepSeconds
            : Duration;

        public DateTime Utc => StartTime.AddSeconds(seconds);

        public void Initialize(LunarApplication app)
        {
            AnalysisRepository.Instance.Reload();
            Provider = new DataDrivenEphemerisProvider(app);
            seconds = 0;
        }

        public void SetTime(double value) { seconds = double.IsNaN(value) ? 0 : Math.Clamp(value, 0, TotalDuration); }
        public void Advance(double delta) { if (!paused) SetTime(seconds + delta * speed); }
        public void Toggle() { paused = !paused; }
        void Update() { Advance(Time.unscaledDeltaTime); }
    }
}
