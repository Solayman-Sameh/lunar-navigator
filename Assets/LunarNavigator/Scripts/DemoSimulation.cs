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
        public bool paused = false;
        public double speed = 1;

        public ILunarEphemeris Provider { get; private set; } = new IllustrativeDemoProvider();

        // Mission selection owns the clock, not whichever dataset was last loaded.
        DateTime missionStartUtc = DeviceUtcSecond;
        double missionDurationSeconds = 72 * 3600;
        public static DateTime DeviceUtcMinute
        {
            get { var now = DateTime.UtcNow; return new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, DateTimeKind.Utc); }
        }
        public DateTime StartTime => missionStartUtc;
        public static DateTime DeviceUtcSecond
        {
            get { var now = DateTime.UtcNow; return new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Utc); }
        }
        public double TotalDuration => missionDurationSeconds;

        public DateTime Utc => StartTime.AddSeconds(seconds);

        public void Initialize(LunarApplication app)
        {
            AnalysisRepository.Instance.Reload();
            Provider = new DataDrivenEphemerisProvider(app);
            seconds = 0;
            paused = true;
            speed = 1;
            SetMissionWindow(DeviceUtcSecond, 72);
            paused = false;
        }

        public void SetMissionWindow(DateTime utc, double durationHours)
        {
            if (!double.IsFinite(durationHours) || durationHours <= 0 || durationHours > 744)
                throw new ArgumentOutOfRangeException(nameof(durationHours));
            utc = utc.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(utc, DateTimeKind.Utc) : utc.ToUniversalTime();
            if (missionStartUtc == utc && missionDurationSeconds == durationHours * 3600) return;
            missionStartUtc = utc;
            missionDurationSeconds = durationHours * 3600;
            seconds = 0;
            paused = true;
        }

        public void SetTime(double value) { seconds = double.IsNaN(value) ? 0 : Math.Clamp(value, 0, TotalDuration); }
        public void Advance(double delta)
        {
            if (paused || !double.IsFinite(delta) || delta <= 0) return;
            if (!double.IsFinite(speed) || speed <= 0) speed = 1;
            SetTime(seconds + delta * speed);
            if (seconds >= TotalDuration) paused = true;
        }
        public void Toggle() { paused = !paused; }
        void Update() { Advance(Time.unscaledDeltaTime); }
    }
}
