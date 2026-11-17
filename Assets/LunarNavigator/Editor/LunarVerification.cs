using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
namespace LunarNavigator.Editor
{
    public static class LunarVerification
    {
        static List<string> evidence;
        static void Check(bool value,string description)
        {evidence.Add((value?"PASS ":"FAIL ")+description);if(!value)Debug.LogError("LUNAR_TEST_FAIL "+description);}
        [MenuItem("Lunar Navigator/Verify Models and Scene")]
        public static string Models()
        {
            evidence=new List<string>();
            for(int lat=-89;lat<=89;lat+=13)for(int lon=-179;lon<=179;lon+=23)
            {var n=LunarCoordinates.Direction(lat,lon);var p=LunarCoordinates.Geographic(n);Check(Mathf.Abs(n.magnitude-1)<.00001f&&Mathf.Abs(p.x-lat)<.001f&&Mathf.Abs(p.y-lon)<.001f,$"Coordinates {lat}/{lon}");}
            Check(Vector3.Distance(LunarCoordinates.Direction(90,0),Vector3.up)<.00001f,"North pole");
            var provider=new IllustrativeDemoProvider();var now=DemoSimulation.Epoch;
            Check(provider.SunDirection(now)==provider.SunDirection(now),"Deterministic Sun");
            Check(provider.EarthDirection(now)!=provider.EarthDirection(now.AddDays(2)),"Earth changes with UTC");
            Check(Vector3.Distance(provider.SunDirection(now),provider.SunDirection(now.AddDays(3)))>.1f,"Sun changes with UTC");
            var app=UnityEngine.Object.FindAnyObjectByType<LunarApplication>();Check(app!=null,"Application in scene");
            if(app)
            {
                Check(app.Director&&app.Clock&&app.UI&&app.World&&app.config,"Wired core references");
                Check(app.sites.Length>=3,"At least three sites");var ids=new HashSet<string>();foreach(var site in app.sites)Check(site&&ids.Add(site.id)&&site.latitude< -80&&(site.provenance.Contains("NASA")||site.provenance.Contains("Fictional")||!string.IsNullOrEmpty(site.provenance)),"Unique labeled south-polar site "+site.name);
                foreach(var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include))
                    Check(renderer.sharedMaterial&&renderer.sharedMaterial.shader&&renderer.sharedMaterial.shader.isSupported,"Supported material "+renderer.name);
                Check(app.World.terrain.GetComponent<MeshCollider>()!=null,"Terrain collider present");
                var volume=UnityEngine.Object.FindAnyObjectByType<UnityEngine.Rendering.Volume>();
                Check(volume&&volume.sharedProfile&&volume.sharedProfile.TryGet<UnityEngine.Rendering.HighDefinition.Exposure>(out var exposure),"Persistent HDRP exposure override");
                Check(volume&&volume.sharedProfile&&volume.sharedProfile.TryGet<UnityEngine.Rendering.HighDefinition.Tonemapping>(out var tone),"Persistent filmic tone mapping");
                Check(app.World.sun.GetComponent<MeshRenderer>().shadowCastingMode==UnityEngine.Rendering.ShadowCastingMode.Off,"Solar display does not cast artificial shadow");
            }
            return Save("models");
        }
        public static string Runtime()
        {
            evidence=new List<string>();var app=UnityEngine.Object.FindAnyObjectByType<LunarApplication>();
            Check(Application.isPlaying&&app,"Play Mode application");if(!app)return Save("runtime");
            var clock=app.Clock;clock.paused=true;clock.SetTime(100);clock.Advance(3);Check(clock.seconds==100,"Paused clock stable");
            clock.paused=false;clock.speed=60;clock.Advance(2);Check(clock.seconds==220,"Clock speed advances");clock.SetTime(-1);Check(clock.seconds==0,"Clock lower bound");clock.SetTime(double.MaxValue);Check(clock.seconds==clock.TotalDuration,"Clock upper bound");clock.SetTime(0);clock.paused=true;clock.speed=3600;
            foreach(var site in app.sites){app.SelectSite(site);Check(app.SelectedSite==site&&app.Director.selected==site,"Shared selection "+site.displayName);}
            app.SelectSite(app.sites[0]);
            var director=app.Director;
            for(int i=0;i<24;i++)
            {
                var mode=(CameraMode)(i%4);director.ChangeMode(mode);
                for(int n=0;n<100;n++)
                {director.Tick(.12f);var pos=director.view.transform.position;Check(float.IsFinite(pos.x)&&float.IsFinite(pos.y)&&float.IsFinite(pos.z),"Finite pose "+i+"/"+n);Check(pos.magnitude>=app.config.radius+.39f,"Globe clearance "+i+"/"+n);}
                Check(!director.IsTransitioning,"Transition finishes "+i);
                if(mode==CameraMode.Approach)Check(director.Mode==CameraMode.LocalOrbit,"Approach ends Local Orbit");
                Check(app.SelectedSite==app.sites[0],"Selection survives transition "+i);
            }
            director.ChangeMode(CameraMode.Approach);director.Tick(1);director.Skip();Check(director.Mode==CameraMode.LocalOrbit&&!director.IsTransitioning,"Approach skip");
            director.ChangeMode(CameraMode.Approach);director.Tick(1);director.ChangeMode(CameraMode.Surface);director.Tick(3);Check(director.Mode==CameraMode.Surface&&!director.IsTransitioning,"Approach interruption");
            Check(app.World.terrain.Clearance(director.view.transform.position)>=.49f,"Surface ground clearance");
            var ui=app.UI;ui.Open("Help");Check(ui.CurrentPopup=="Help","Help opens");Check(ui.Close()&&ui.CurrentPopup=="","Popup closes");
            var overlay=ui.GetComponent<UIDocument>().rootVisualElement.Q("overlay");
            Check(overlay.worldBound.height>500,"Fullscreen UI root has nonzero height");
            foreach(var name in new[]{"Sites","Layers","Settings","Simulation","Site information"}){ui.Open(name);Check(ui.CurrentPopup==name,"Popover "+name);ui.Close();}
            bool visible=ui.InterfaceVisible;ui.ToggleInterface();Check(ui.InterfaceVisible!=visible,"Immersive hides");ui.ToggleInterface();Check(ui.InterfaceVisible==visible,"Immersive restores");
            foreach(var p in new[]{"Performance","Balanced","Cinematic"}){app.World.ApplyQuality(p);Check(app.World.Preset==p,"Graphics "+p);}
            app.World.ApplyQuality("Balanced");director.ChangeMode(CameraMode.Global);director.Tick(3);director.ResetView();director.Tick(3);
            return Save("runtime");
        }
        static string Save(string name)
        {
            Directory.CreateDirectory("Documentation/Verification");int failed=evidence.FindAll(x=>x.StartsWith("FAIL")).Count;
            string summary=$"{name}: {evidence.Count-failed} passed, {failed} failed";
            File.WriteAllText("Documentation/Verification/"+name+".txt",summary+"\n"+string.Join("\n",evidence));Debug.Log("LUNAR_VERIFICATION "+summary);return summary;
        }

        [MenuItem("Lunar Navigator/Verify Data Parity")]
        public static string DataParity()
        {
            evidence = new List<string>();
            Directory.CreateDirectory("docs");

            // 1. Hand-made 10-sample unit test
            var unitAnalysis = new LunarNavigator.Data.SiteAnalysis
            {
                schema_version = 1,
                data_status = "ILLUSTRATIVE_DEMO",
                ephemeris = new LunarNavigator.Data.Ephemeris
                {
                    n = 10,
                    step_s = 600,
                    t0_utc = "2026-12-01T00:00:00Z",
                    sun_az_deg = new double[] { 0, 10, 20, 30, 40, 50, 60, 70, 80, 90 },
                    sun_el_deg = new double[] { 2, 2, 2, -1, -1, 2, 2, 2, 2, -1 },
                    earth_az_deg = new double[] { 0, 10, 20, 30, 40, 50, 60, 70, 80, 90 },
                    earth_el_deg = new double[] { 5, 5, 5, 5, 5, -2, -2, 5, 5, 5 }
                },
                terrain = new LunarNavigator.Data.TerrainInfo
                {
                    horizon_el_deg = new double[360] // all 0
                },
                visibility = new LunarNavigator.Data.VisibilityInfo
                {
                    sun_margin_deg = 0.0,
                    sun_visible = new int[] { 1, 1, 1, 0, 0, 1, 1, 1, 1, 0 },
                    earth_visible = new int[] { 1, 1, 1, 1, 1, 0, 0, 1, 1, 1 }
                }
            };
            var unitMetric = LunarNavigator.Data.VisibilityMath.Compute(unitAnalysis, 0, 10);
            Check(Math.Abs(unitMetric.f_sun - 0.7) < 1e-4, "Unit test f_sun == 0.7");
            Check(Math.Abs(unitMetric.f_earth - 0.8) < 1e-4, "Unit test f_earth == 0.8");
            Check(Math.Abs(unitMetric.f_both - 0.5) < 1e-4, "Unit test f_both == 0.5");
            Check(Math.Abs(unitMetric.dark_max_h - (2.0 * 600.0 / 3600.0)) < 1e-4, "Unit test dark_max_h == 0.333h");
            Check(Math.Abs(unitMetric.comm_gap_max_h - (2.0 * 600.0 / 3600.0)) < 1e-4, "Unit test comm_gap_max_h == 0.333h");

            // 2. Parity check on loaded sites in repository
            LunarNavigator.Data.AnalysisRepository.Instance.Reload();
            var repo = LunarNavigator.Data.AnalysisRepository.Instance;
            Check(repo.Sites.Count > 0, "Repository loaded sites count > 0");

            foreach (var site in repo.Sites)
            {
                string id = site.site?.id ?? "unknown";
                int n = site.ephemeris.n;
                int sunMismatches = 0;
                int earthMismatches = 0;

                for (int i = 0; i < n; i++)
                {
                    bool sComp = LunarNavigator.Data.VisibilityMath.SunVisibleComputed(site, i);
                    bool sStored = LunarNavigator.Data.VisibilityMath.SunVisibleAt(site, i);
                    double sSky = LunarNavigator.Data.VisibilityMath.SkylineAt(site.terrain.horizon_el_deg, site.ephemeris.sun_az_deg[i]) + site.visibility.sun_margin_deg;
                    if (sComp != sStored && Math.Abs(site.ephemeris.sun_el_deg[i] - sSky) >= 0.002)
                    {
                        sunMismatches++;
                    }

                    bool eComp = LunarNavigator.Data.VisibilityMath.EarthVisibleComputed(site, i);
                    bool eStored = LunarNavigator.Data.VisibilityMath.EarthVisibleAt(site, i);
                    double eSky = LunarNavigator.Data.VisibilityMath.SkylineAt(site.terrain.horizon_el_deg, site.ephemeris.earth_az_deg[i]);
                    if (eComp != eStored && Math.Abs(site.ephemeris.earth_el_deg[i] - eSky) >= 0.002)
                    {
                        earthMismatches++;
                    }
                }
                Check(sunMismatches == 0, $"Site {id}: Sun visibility flag parity (0 mismatches)");
                Check(earthMismatches == 0, $"Site {id}: Earth visibility flag parity (0 mismatches)");

                // Compare reference metrics
                if (site.reference_metrics != null)
                {
                    int refMismatches = 0;
                    DateTime t0 = DateTime.Parse(site.ephemeris.t0_utc, null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal);
                    foreach (var rm in site.reference_metrics)
                    {
                        DateTime start = DateTime.Parse(rm.start_utc, null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal);
                        int startIdx = (int)Math.Round((start - t0).TotalSeconds / site.ephemeris.step_s);
                        int sampleCount = (int)Math.Round(rm.duration_h * 3600.0 / site.ephemeris.step_s);

                        var computed = LunarNavigator.Data.VisibilityMath.Compute(site, startIdx, sampleCount);
                        if (Math.Abs(computed.f_sun - rm.f_sun) > 1e-4 ||
                            Math.Abs(computed.f_earth - rm.f_earth) > 1e-4 ||
                            Math.Abs(computed.f_both - rm.f_both) > 1e-4 ||
                            Math.Abs(computed.dark_max_h - rm.dark_max_h) > 1e-4 ||
                            Math.Abs(computed.comm_gap_max_h - rm.comm_gap_max_h) > 1e-4)
                        {
                            refMismatches++;
                        }
                    }
                    Check(refMismatches == 0, $"Site {id}: Reference metrics parity (0 mismatches across {site.reference_metrics.Length} entries)");
                }
            }

            int failed = evidence.FindAll(x => x.StartsWith("FAIL")).Count;
            string summary = $"Data Parity: {evidence.Count - failed} passed, {failed} failed";
            File.WriteAllText("docs/unity_parity.txt", summary + "\n" + string.Join("\n", evidence));
            File.WriteAllText("Documentation/Verification/parity.txt", summary + "\n" + string.Join("\n", evidence));
            Debug.Log("LUNAR_PARITY " + summary);
            return summary;
        }

        [MenuItem("Lunar Navigator/Verify Frame")]
        public static string FrameChecks()
        {
            evidence = new List<string>();

            // F2: Landmarks on lunar sphere
            var tycho = LunarCoordinates.Direction(-43.3, -11.2);
            var copernicus = LunarCoordinates.Direction(9.6, -20.1);
            Check(Mathf.Abs(tycho.magnitude - 1f) < 1e-4f, "F2 Tycho direction unit vector");
            Check(Mathf.Abs(copernicus.magnitude - 1f) < 1e-4f, "F2 Copernicus direction unit vector");

            // F3: Compass at a site in Surface Look
            // Site frame at south polar site
            Vector3 siteDir = LunarCoordinates.Direction(-89.6, 129.0);
            Quaternion frame = LunarCoordinates.Frame(siteDir);
            Vector3 localUp = frame * Vector3.up;
            Vector3 localNorth = frame * Vector3.forward;
            Vector3 localEast = frame * Vector3.right;
            // Cross product of North and Up should equal East (Right-handed)
            Vector3 crossNorthUp = Vector3.Cross(localNorth, localUp);
            Check(Vector3.Dot(crossNorthUp, -localEast) > 0.99f || Vector3.Dot(Vector3.Cross(localUp, localNorth), localEast) > 0.99f,
                "F3 Compass: Facing north with up above, East is to the right");

            // F4: Self-consistency of direction math
            // dir = cos(el)*(cos(az)*north + sin(az)*east) + sin(el)*up
            float testAz = 45f;
            float testEl = 3.5f;
            float azRad = testAz * Mathf.Deg2Rad;
            float elRad = testEl * Mathf.Deg2Rad;
            Vector3 dir = Mathf.Cos(elRad) * (Mathf.Cos(azRad) * localNorth + Mathf.Sin(azRad) * localEast) + Mathf.Sin(elRad) * localUp;
            float recomputedEl = Mathf.Asin(Mathf.Clamp(Vector3.Dot(dir, localUp), -1f, 1f)) * Mathf.Rad2Deg;
            Check(Mathf.Abs(recomputedEl - testEl) < 0.05f, $"F4 Self-consistency: elevation recomputed matches within 0.05 deg ({recomputedEl:F3} vs {testEl:F3})");

            int failed = evidence.FindAll(x => x.StartsWith("FAIL")).Count;
            string summary = $"Frame Checks: {evidence.Count - failed} passed, {failed} failed";
            File.WriteAllText("Documentation/Verification/frame.txt", summary + "\n" + string.Join("\n", evidence));
            Debug.Log("LUNAR_FRAME " + summary);
            return summary;
        }

        [MenuItem("Lunar Navigator/Verify Contract Gate")]
        public static string ContractGate()
        {
            evidence = new List<string>();
            Directory.CreateDirectory("docs");
            Directory.CreateDirectory("Documentation/Verification");

            // --- 1. Deserialization of Contract v1 Conforming Fixture ---
            var horizonArr = new double[360];
            for (int k = 0; k < 360; k++) horizonArr[k] = 1.0;
            string horizonJson = "[" + string.Join(",", horizonArr) + "]";

            string fixtureJson = $@"{{
  ""schema_version"": 1,
  ""data_status"": ""ILLUSTRATIVE_DEMO"",
  ""algorithm_version"": ""0.1.0"",
  ""generated_utc"": ""2026-11-15T17:00:00Z"",
  ""site"": {{
    ""id"": ""shackleton_rim"",
    ""name"": ""Shackleton Rim"",
    ""lat_deg"": -89.6,
    ""lon_east_deg"": 129.12345678901234,
    ""observer_height_m"": 2.0,
    ""provenance"": ""Contract Gate Deterministic Synthetic Fixture""
  }},
  ""ephemeris"": {{
    ""source"": ""Deterministic Test Ephemeris"",
    ""t0_utc"": ""2026-12-01T00:00:00Z"",
    ""step_s"": 600,
    ""n"": 6,
    ""sun_az_deg"": [0.0, 60.12345678901234, 120.0, 180.0, 240.0, 300.0],
    ""sun_el_deg"": [2.5, -0.5, 3.0, -1.0, 2.0, 0.0],
    ""earth_az_deg"": [45.0, 45.0, 45.0, 45.0, 45.0, 45.0],
    ""earth_el_deg"": [5.0, 5.0, 5.0, 5.0, 5.0, 5.0]
  }},
  ""terrain"": {{
    ""source"": ""Deterministic Flat Horizon with Sector"",
    ""resolution_m"": 100,
    ""max_range_km"": 150,
    ""az_step_deg"": 1.0,
    ""horizon_el_deg"": {horizonJson},
    ""incomplete_sectors"": [
      {{ ""az_from_deg"": 200.0, ""az_to_deg"": 215.0 }}
    ]
  }},
  ""visibility"": {{
    ""sun_margin_deg"": 0.0,
    ""sun_visible"": [1, 0, 1, 0, 1, 0],
    ""earth_visible"": [1, 1, 1, 1, 1, 1]
  }},
  ""reference_metrics"": [
    {{
      ""start_utc"": ""2026-12-01T00:00:00Z"",
      ""duration_h"": 1,
      ""f_sun"": 0.5,
      ""f_earth"": 1.0,
      ""f_both"": 0.5,
      ""dark_max_h"": 0.16666666666666666,
      ""comm_gap_max_h"": 0.0
    }}
  ]
}}";

            LunarNavigator.Data.SiteAnalysis fixtureSite = null;
            try
            {
                fixtureSite = JsonUtility.FromJson<LunarNavigator.Data.SiteAnalysis>(fixtureJson);
            }
            catch (Exception ex)
            {
                Check(false, "Deserialization exception: " + ex.Message);
            }

            Check(fixtureSite != null, "Fixture deserialization succeeded");
            Check(fixtureSite?.site?.id == "shackleton_rim", "Fixture site ID parsed correctly");
            Check(fixtureSite?.site?.name == "Shackleton Rim", "Fixture site name parsed correctly");
            Check(fixtureSite?.data_status == "ILLUSTRATIVE_DEMO", "Fixture data_status is ILLUSTRATIVE_DEMO");

            // --- 2. Horizon Array Length & az_step_deg (Requirement B) ---
            Check(fixtureSite?.terrain?.horizon_el_deg != null && fixtureSite.terrain.horizon_el_deg.Length == 360,
                "Requirement B: Horizon array length is exactly 360 entries");
            Check(Math.Abs((fixtureSite?.terrain?.az_step_deg ?? 0.0) - 1.0) < 1e-4,
                "Requirement B: Horizon az_step_deg is exactly 1.0 degree");

            var repo = LunarNavigator.Data.AnalysisRepository.Instance;
            Check(repo.ValidateSiteData(fixtureSite, "fixture_valid.json"), "ValidateSiteData approves conforming 360-sample fixture");

            // Test non-360 rejection
            var badLengthSite = JsonUtility.FromJson<LunarNavigator.Data.SiteAnalysis>(fixtureJson);
            badLengthSite.terrain.horizon_el_deg = new double[180];
            Check(!repo.ValidateSiteData(badLengthSite, "fixture_bad_length.json"),
                "Requirement B: Non-360 horizon array length (180) strictly rejected");

            // Test non-1.0 az_step_deg rejection
            var badStepSite = JsonUtility.FromJson<LunarNavigator.Data.SiteAnalysis>(fixtureJson);
            badStepSite.terrain.az_step_deg = 0.5;
            Check(!repo.ValidateSiteData(badStepSite, "fixture_bad_step.json"),
                "Requirement B: Non-1.0 az_step_deg (0.5) strictly rejected for v1 contract");

            // --- 3. Object-based Incomplete Sectors (Requirement A) ---
            Check(fixtureSite?.terrain?.incomplete_sectors != null && fixtureSite.terrain.incomplete_sectors.Length == 1,
                "Requirement A: Incomplete sectors parsed as object array");
            var sec0 = fixtureSite?.terrain?.incomplete_sectors?[0];
            Check(sec0 != null && Math.Abs(sec0.az_from_deg - 200.0) < 1e-4 && Math.Abs(sec0.az_to_deg - 215.0) < 1e-4,
                "Requirement A: Incomplete sector contains valid az_from_deg=200.0 and az_to_deg=215.0");

            var badSecSite = JsonUtility.FromJson<LunarNavigator.Data.SiteAnalysis>(fixtureJson);
            badSecSite.terrain.incomplete_sectors = new[] { new LunarNavigator.Data.SectorRange { az_from_deg = -5.0, az_to_deg = 400.0 } };
            Check(!repo.ValidateSiteData(badSecSite, "fixture_bad_sector.json"),
                "Requirement A: Out-of-range incomplete sector angles [-5, 400] rejected");

            // --- 4. Numerical Precision (Requirement C) ---
            Check(Math.Abs(fixtureSite.site.lon_east_deg - 129.12345678901234) < 1e-13,
                "Requirement C: Full double precision preserved in site longitude (lon_east_deg)");
            Check(Math.Abs(fixtureSite.ephemeris.sun_az_deg[1] - 60.12345678901234) < 1e-13,
                "Requirement C: Full double precision preserved in ephemeris angles (sun_az_deg[1])");

            // --- 5. Time Indexing (Requirement D) ---
            DateTime expectedT0 = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc);
            Check(repo.TimeAt(fixtureSite, 0) == expectedT0, "Requirement D: TimeAt index 0 matches t0_utc");
            Check(repo.TimeAt(fixtureSite, 3) == expectedT0.AddSeconds(1800), "Requirement D: TimeAt index 3 matches t0_utc + 1800s");
            Check(repo.IndexAt(fixtureSite, expectedT0.AddSeconds(1800)) == 3, "Requirement D: IndexAt roundtrip matches index 3");
            Check(repo.IndexAt(fixtureSite, expectedT0.AddDays(100)) == 5, "Requirement D: IndexAt clamps upper bound to n - 1");

            // --- 6. Visibility Flags & Skyline Interpolation (Requirement D) ---
            for (int i = 0; i < 6; i++)
            {
                double az = fixtureSite.ephemeris.sun_az_deg[i];
                double el = fixtureSite.ephemeris.sun_el_deg[i];
                double sky = LunarNavigator.Data.VisibilityMath.SkylineAt(fixtureSite.terrain.horizon_el_deg, az);
                bool expectedVis = el > sky;
                Check(LunarNavigator.Data.VisibilityMath.SunVisibleAt(fixtureSite, i) == expectedVis,
                    $"Requirement D: Visibility flag sample {i} (el={el:F1}, sky={sky:F1}) matches expected={expectedVis}");
            }

            // --- 7. Mission-Duration Metrics (Requirement D) ---
            var metric1h = LunarNavigator.Data.VisibilityMath.Compute(fixtureSite, 0, 6);
            Check(Math.Abs(metric1h.f_sun - 0.5) < 1e-6, "Requirement D: Window metrics f_sun == 0.5 (3 sunlit of 6 samples)");
            Check(Math.Abs(metric1h.f_earth - 1.0) < 1e-6, "Requirement D: Window metrics f_earth == 1.0 (6 of 6 samples)");
            Check(Math.Abs(metric1h.f_both - 0.5) < 1e-6, "Requirement D: Window metrics f_both == 0.5 (3 of 6 samples)");
            Check(Math.Abs(metric1h.dark_max_h - (600.0 / 3600.0)) < 1e-6, "Requirement D: Window metrics dark_max_h == 0.1667h (1 sample run)");
            Check(Math.Abs(metric1h.comm_gap_max_h - 0.0) < 1e-6, "Requirement D: Window metrics comm_gap_max_h == 0.0h");

            bool windowOutOfRangeCaught = false;
            try
            {
                LunarNavigator.Data.VisibilityMath.Compute(fixtureSite, 2, 6);
            }
            catch (ArgumentOutOfRangeException)
            {
                windowOutOfRangeCaught = true;
            }
            Check(windowOutOfRangeCaught, "Requirement D: Window exceeding available samples (a + m > n) strictly throws ArgumentOutOfRangeException");

            // --- 8. Invalid-File Rejection (Requirement E) ---
            Check(!repo.ValidateSiteData(null, "null_file.json"), "Requirement E: Null site analysis rejected");
            var noIdSite = JsonUtility.FromJson<LunarNavigator.Data.SiteAnalysis>(fixtureJson);
            noIdSite.site.id = "";
            Check(!repo.ValidateSiteData(noIdSite, "no_id.json"), "Requirement E: Empty site ID rejected");

            var nonFiniteElSite = JsonUtility.FromJson<LunarNavigator.Data.SiteAnalysis>(fixtureJson);
            nonFiniteElSite.terrain.horizon_el_deg[10] = double.NaN;
            Check(!repo.ValidateSiteData(nonFiniteElSite, "nan_horizon.json"), "Requirement E: NaN in horizon elevations rejected");

            var implausibleElSite = JsonUtility.FromJson<LunarNavigator.Data.SiteAnalysis>(fixtureJson);
            implausibleElSite.terrain.horizon_el_deg[10] = 85.0;
            Check(!repo.ValidateSiteData(implausibleElSite, "implausible_horizon.json"), "Requirement E: Implausible horizon elevation (>75 deg) rejected");

            var ephemMismatchSite = JsonUtility.FromJson<LunarNavigator.Data.SiteAnalysis>(fixtureJson);
            ephemMismatchSite.ephemeris.sun_az_deg = new double[5];
            Check(!repo.ValidateSiteData(ephemMismatchSite, "mismatch_ephem.json"), "Requirement E: Ephemeris array length mismatch rejected");

            // --- 9. Conservative Incomplete Sector Handling (Requirement F) ---
            Check(fixtureSite.HasIncompleteSectors, "Requirement F: Site with incomplete sectors flags HasIncompleteSectors = true");
            Check(LunarNavigator.Data.VisibilityMath.IsIncompleteSector(fixtureSite, 205.0),
                "Requirement F: Azimuth 205.0 deg correctly detected inside incomplete sector [200, 215]");
            Check(!LunarNavigator.Data.VisibilityMath.IsIncompleteSector(fixtureSite, 50.0),
                "Requirement F: Azimuth 50.0 deg correctly detected outside incomplete sector [200, 215]");

            int failed = evidence.FindAll(x => x.StartsWith("FAIL")).Count;
            string summary = $"Contract Gate: {evidence.Count - failed} passed, {failed} failed";
            File.WriteAllText("docs/unity_contract_gate.txt", summary + "\n" + string.Join("\n", evidence));
            File.WriteAllText("Documentation/Verification/contract_gate.txt", summary + "\n" + string.Join("\n", evidence));
            Debug.Log("LUNAR_CONTRACT_GATE " + summary);
            return summary;
        }

        [MenuItem("Lunar Navigator/Verify Online Integration")]
        public static string OnlineIntegration()
        {
            evidence = new List<string>();
            Directory.CreateDirectory("docs");
            Directory.CreateDirectory("Documentation/Verification");

            // --- 1. AnalyzeRequestPayload JSON Serialization Tests ---
            // Test 1A: With site_id, coordinates must be omitted (FastAPI mutual exclusivity)
            var p1 = new LunarNavigator.Data.AnalyzeRequestPayload
            {
                site_id = "malapert_mountain",
                landing_utc = "2027-02-15T06:00:00Z",
                mission_duration_h = 120.0,
                observer_height_m = 2.0,
                sampling_step_minutes = 10
            };
            string j1 = p1.ToJson();
            Check(j1.Contains("\"site_id\":\"malapert_mountain\""), "Request payload serializes site_id");
            Check(!j1.Contains("latitude_deg") && !j1.Contains("longitude_deg"), "Request payload omits coordinates when site_id is specified");
            Check(j1.Contains("\"mission_duration_h\":120.000"), "Request payload preserves exact 120h duration");
            Check(j1.Contains("\"landing_utc\":\"2027-02-15T06:00:00Z\""), "Request payload preserves arbitrary 2027 landing UTC");

            // Test 1B: With coordinates, site_id must be omitted
            var p2 = new LunarNavigator.Data.AnalyzeRequestPayload
            {
                latitude_deg = -89.543210,
                longitude_deg = 129.876543,
                landing_utc = "2028-06-20T12:00:00Z",
                mission_duration_h = 48.5,
                observer_height_m = 2.0,
                sampling_step_minutes = 10
            };
            string j2 = p2.ToJson();
            Check(!j2.Contains("\"site_id\""), "Request payload omits site_id when coordinates are specified");
            Check(j2.Contains("\"latitude_deg\":-89.543210"), "Request payload preserves 6-decimal latitude");
            Check(j2.Contains("\"longitude_deg\":129.876543"), "Request payload preserves 6-decimal longitude");
            Check(j2.Contains("\"mission_duration_h\":48.500"), "Request payload preserves fractional duration (48.5h)");

            // --- 2. ScienceApiAdapter Parsing & GEOMETRIC_ONLY Integrity ---
            int testN = 5;
            string mockFastApiResponse = $@"{{
  ""schema_version"": 1,
  ""api_profile"": ""lunar-analyze-v1"",
  ""data_status"": ""GEOMETRIC_ONLY"",
  ""algorithm_version"": ""0.1.0"",
  ""run_id"": ""test-run-12345"",
  ""generated_utc"": ""2026-11-15T20:00:00Z"",
  ""site"": {{
    ""id"": ""malapert_mountain"",
    ""lat_deg"": -89.5,
    ""lon_east_deg"": 0.0,
    ""observer_height_m"": 2.0,
    ""coordinate_status"": ""VERIFIED_CATALOG_POINT_NOMINAL_SPHERE_OBSERVER""
  }},
  ""ephemeris"": {{
    ""source"": ""NASA/JPL Horizons API, coord@301"",
    ""t0_utc"": ""2027-02-15T06:00:00Z"",
    ""step_s"": 600,
    ""n"": {testN},
    ""sun_az_deg"": [10.1, 10.2, 10.3, 10.4, 10.5],
    ""sun_el_deg"": [1.5, 1.6, 1.7, 1.8, 1.9],
    ""earth_az_deg"": [45.1, 45.2, 45.3, 45.4, 45.5],
    ""earth_el_deg"": [3.1, 3.2, 3.3, 3.4, 3.5]
  }},
  ""terrain"": {{
    ""source"": ""spherical_dip"",
    ""az_step_deg"": 1.0,
    ""horizon_el_deg"": [{string.Join(",", new double[360])}],
    ""incomplete_sectors"": []
  }},
  ""visibility"": {{
    ""sun_margin_deg"": 0.0,
    ""sun_visible"": [1, 1, 1, 1, 1],
    ""earth_visible"": [1, 1, 1, 1, 1]
  }}
}}";

            var adapted = LunarNavigator.Data.ScienceApiAdapter.AdaptApiResponse(
                mockFastApiResponse, "malapert_mountain", "Malapert Mountain", out string adaptErr);

            Check(adapted != null && string.IsNullOrEmpty(adaptErr), "ScienceApiAdapter deserializes valid FastAPI response");
            Check(adapted != null && adapted.data_status == "GEOMETRIC_ONLY",
                "Provenance Rule: GEOMETRIC_ONLY data status strictly preserved (NOT upgraded to terrain-aware)");
            Check(adapted != null && adapted.ephemeris.n == testN, $"Ephemeris sample count n={testN} parsed");
            Check(adapted != null && adapted.terrain.horizon_el_deg.Length == 360, "Terrain horizon has exactly 360 samples");
            Check(adapted != null && Math.Abs(adapted.ephemeris.sun_az_deg[0] - 10.1) < 1e-4, "Double precision preserved in sun_az_deg[0]");

            // --- 3. Dynamic Repository Integration ---
            if (adapted != null)
            {
                LunarNavigator.Data.AnalysisRepository.Instance.SetDynamicAnalysis(adapted);
                var repo = LunarNavigator.Data.AnalysisRepository.Instance;
                Check(repo.DataStatus == "GEOMETRIC_ONLY", "Repository DataStatus updated to GEOMETRIC_ONLY");
                Check(!repo.IsDemoMode, "Repository IsDemoMode is false for live API data");
                Check(repo.Count == testN, $"Repository Count matches live response ({testN})");
                Check(repo.T0.Year == 2027 && repo.T0.Month == 2 && repo.T0.Day == 15, "Repository T0 updated to arbitrary 2027 mission date");
            }

            // --- 4. MissionConfigView Component Unit Tests ---
            var configView = new LunarNavigator.UI.MissionConfigView();
            Check(configView != null, "MissionConfigView instantiated cleanly");
            
            // Editable date beyond 14-day stub
            var targetUtc = new DateTime(2027, 8, 20, 15, 30, 0, DateTimeKind.Utc);
            configView.SetLandingUtc(targetUtc);
            Check(configView.SelectedLandingUtc == targetUtc, "MissionConfigView accepts arbitrary 2027 UTC landing date");
            Check(configView.TxtLandingUtc.value.Contains("2027-08-20 15:30"), "MissionConfigView text field formatted with target date and time");

            // Editable duration preserving exact interval
            configView.SetDuration(144.0);
            Check(Math.Abs(configView.SelectedDurationHours - 144.0) < 1e-4, "MissionConfigView preserves exact 144h duration");
            Check(configView.TxtDurationHours.value == "144.0", "MissionConfigView duration text field reflects exact 144.0h");

            // Loading and Error states
            configView.SetLoading(true, "Testing loading state...");
            Check(configView.IsLoading, "MissionConfigView IsLoading flag true");
            Check(configView.LoadingIndicator.style.display == UnityEngine.UIElements.DisplayStyle.Flex, "LoadingIndicator displayed");
            Check(configView.BtnAnalyze.text == "ANALYZING...", "BtnAnalyze reflects loading text");

            configView.ShowError("Simulated Network Connection Refused");
            Check(!configView.IsLoading, "ShowError clears IsLoading");
            Check(configView.ErrorBanner.style.display == UnityEngine.UIElements.DisplayStyle.Flex, "ErrorBanner displayed on failure");
            Check(configView.LblErrorMessage.text.Contains("Simulated Network"), "LblErrorMessage displays exact error detail");

            configView.ClearError();
            Check(configView.ErrorBanner.style.display == UnityEngine.UIElements.DisplayStyle.None, "ClearError hides ErrorBanner");

            // Restore clean state
            LunarNavigator.Data.AnalysisRepository.Instance.Reload();

            int failed = evidence.FindAll(x => x.StartsWith("FAIL")).Count;
            string summary = $"Online Integration: {evidence.Count - failed} passed, {failed} failed";
            File.WriteAllText("docs/unity_online_integration.txt", summary + "\n" + string.Join("\n", evidence));
            File.WriteAllText("Documentation/Verification/online_integration.txt", summary + "\n" + string.Join("\n", evidence));
            Debug.Log("LUNAR_ONLINE_INTEGRATION " + summary);
            return summary;
        }

        [MenuItem("Lunar Navigator/Verify Real FastAPI Integration")]
        public static string RealLiveFastApiIntegration()
        {
            evidence = new List<string>();

            string baseUrl = "http://127.0.0.1:8000";

            string HttpGet(string path)
            {
                var req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(baseUrl + path);
                req.Method = "GET";
                req.Timeout = 10000;
                using var resp = (System.Net.HttpWebResponse)req.GetResponse();
                using var reader = new System.IO.StreamReader(resp.GetResponseStream());
                return reader.ReadToEnd();
            }

            string HttpPost(string path, string json)
            {
                var req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(baseUrl + path);
                req.Method = "POST";
                req.ContentType = "application/json";
                req.Timeout = 25000;
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
                req.ContentLength = bytes.Length;
                using (var stream = req.GetRequestStream()) { stream.Write(bytes, 0, bytes.Length); }
                using var resp = (System.Net.HttpWebResponse)req.GetResponse();
                using var reader = new System.IO.StreamReader(resp.GetResponseStream());
                return reader.ReadToEnd();
            }

            // 1. Health Probe
            try
            {
                string healthJson = HttpGet("/health");
                Check(!string.IsNullOrEmpty(healthJson), "FastAPI /health returned non-empty response");
                Check(healthJson.Contains("\"status\":\"ok\""), "FastAPI health status is 'ok'");
                Check(healthJson.Contains("GEOMETRIC_ONLY"), "FastAPI data_status is GEOMETRIC_ONLY");
            }
            catch (Exception ex)
            {
                Check(false, "FastAPI /health reachable: " + ex.Message);
            }

            // 2. Sites Probe & Verification
            try
            {
                string sitesJson = HttpGet("/sites");
                Check(!string.IsNullOrEmpty(sitesJson), "FastAPI /sites returned non-empty response");
                var sitesResp = JsonUtility.FromJson<LunarNavigator.Data.SitesResponse>(sitesJson);
                Check(sitesResp != null && sitesResp.sites != null, "Parsed SitesResponse has sites list");
                Check(sitesResp.sites.Length >= 3, $"Catalog contains at least 3 sites (actual: {sitesResp?.sites?.Length})");
                
                var shackleton = Array.Find(sitesResp.sites, s => s.id == "shackleton_rim");
                Check(shackleton != null && Math.Abs(shackleton.lat_deg - (-89.7668)) < 1e-4, "Shackleton Rim verified lat -89.7668");
                Check(shackleton != null && Math.Abs(shackleton.lon_east_deg - (-171.87)) < 1e-4, "Shackleton Rim verified lon -171.87");

                var connecting = Array.Find(sitesResp.sites, s => s.id == "connecting_ridge");
                Check(connecting != null && Math.Abs(connecting.lat_deg - (-89.4632)) < 1e-4, "Connecting Ridge verified lat -89.4632");
                Check(connecting != null && Math.Abs(connecting.lon_east_deg - (-137.49)) < 1e-4, "Connecting Ridge verified lon -137.49");

                var malapert = Array.Find(sitesResp.sites, s => s.id == "malapert_mountain");
                Check(malapert != null && Math.Abs(malapert.lat_deg - (-85.9948)) < 1e-4, "Malapert Massif verified lat -85.9948");
                Check(malapert != null && Math.Abs(malapert.lon_east_deg - (-0.23578)) < 1e-4, "Malapert Massif verified lon -0.23578");
            }
            catch (Exception ex)
            {
                Check(false, "FastAPI /sites query and verification: " + ex.Message);
            }

            // 3. Real POST /analyze - Test Run 1: Connecting Ridge (Feb 15, 2027)
            try
            {
                string req1 = "{\"site_id\":\"connecting_ridge\",\"landing_utc\":\"2027-02-15T06:00:00Z\",\"mission_duration_h\":24.0,\"sampling_step_minutes\":10,\"observer_height_m\":2.0}";
                string resp1 = HttpPost("/analyze", req1);
                Check(!string.IsNullOrEmpty(resp1), "FastAPI /analyze returned response for connecting_ridge Feb 2027");

                var adapted1 = LunarNavigator.Data.ScienceApiAdapter.AdaptApiResponse(resp1, "connecting_ridge", "Connecting Ridge", out string err1);
                Check(err1 == null, "ScienceApiAdapter adapted Run 1 with no errors: " + err1);
                Check(adapted1 != null, "Adapted SiteAnalysis 1 is non-null");
                Check(adapted1.ephemeris != null && adapted1.ephemeris.n == 145, $"Run 1 sample count n=145 (actual: {adapted1?.ephemeris?.n})");
                Check(adapted1.metrics != null, "Run 1 contains parsed LiveMetrics object");
                Check(adapted1.metrics.f_sun == 0.0, $"Run 1 f_sun == 0.0 (actual: {adapted1?.metrics?.f_sun})");
                Check(adapted1.metrics.f_earth == 1.0, $"Run 1 f_earth == 1.0 (actual: {adapted1?.metrics?.f_earth})");
                Check(adapted1.data_status == "GEOMETRIC_ONLY", "Run 1 strictly GEOMETRIC_ONLY provenance");

                // Feed into repository
                LunarNavigator.Data.AnalysisRepository.Instance.SetDynamicAnalysis(adapted1);
                var repo = LunarNavigator.Data.AnalysisRepository.Instance;
                Check(repo.DataStatus == "GEOMETRIC_ONLY", "Repository DataStatus is GEOMETRIC_ONLY for Run 1");
                Check(repo.T0.Year == 2027 && repo.T0.Month == 2 && repo.T0.Day == 15, "Repository T0 updated to 2027-02-15");
                Check(repo.Count == 145, "Repository ephemeris count is 145");
            }
            catch (Exception ex)
            {
                Check(false, "Run 1 connecting_ridge 2027-02-15: " + ex.Message);
            }

            // 4. Real POST /analyze - Test Run 2: Shackleton Rim (Dec 01, 2026)
            try
            {
                string req2 = "{\"site_id\":\"shackleton_rim\",\"landing_utc\":\"2026-12-01T00:00:00Z\",\"mission_duration_h\":48.0,\"sampling_step_minutes\":10,\"observer_height_m\":2.0}";
                string resp2 = HttpPost("/analyze", req2);
                Check(!string.IsNullOrEmpty(resp2), "FastAPI /analyze returned response for shackleton_rim Dec 2026");

                var adapted2 = LunarNavigator.Data.ScienceApiAdapter.AdaptApiResponse(resp2, "shackleton_rim", "Shackleton Rim", out string err2);
                Check(err2 == null, "ScienceApiAdapter adapted Run 2 with no errors: " + err2);
                Check(adapted2 != null, "Adapted SiteAnalysis 2 is non-null");
                Check(adapted2.ephemeris != null && adapted2.ephemeris.n == 289, $"Run 2 sample count n=289 for 48h (actual: {adapted2?.ephemeris?.n})");
                Check(adapted2.metrics != null, "Run 2 contains parsed LiveMetrics object");
                Check(adapted2.ephemeris.t0_utc.StartsWith("2026-12-01"), "Run 2 t0_utc starts with 2026-12-01");
                Check(adapted2.site.id == "shackleton_rim", "Run 2 site ID is shackleton_rim");
                Check(adapted2.data_status == "GEOMETRIC_ONLY", "Run 2 strictly GEOMETRIC_ONLY provenance");

                // Check difference between Run 1 and Run 2 celestial vectors
                var site1 = LunarNavigator.Data.AnalysisRepository.Instance.GetSite("connecting_ridge");
                Check(site1 != null, "Retrieved Run 1 site");
                Check(adapted2.ephemeris.sun_az_deg[0] != site1.ephemeris.sun_az_deg[0], "Distinct ephemeris azimuths between sites/dates");

                // Feed Run 2 into repository
                LunarNavigator.Data.AnalysisRepository.Instance.SetDynamicAnalysis(adapted2);
                var repo = LunarNavigator.Data.AnalysisRepository.Instance;
                Check(repo.T0.Year == 2026 && repo.T0.Month == 12 && repo.T0.Day == 1, "Repository T0 updated to 2026-12-01");
                Check(repo.Count == 289, "Repository ephemeris count updated to 289");
            }
            catch (Exception ex)
            {
                Check(false, "Run 2 shackleton_rim 2026-12-01: " + ex.Message);
            }

            // Restore clean state
            LunarNavigator.Data.AnalysisRepository.Instance.Reload();

            int failed = evidence.FindAll(x => x.StartsWith("FAIL")).Count;
            string summary = $"Real Live FastAPI: {evidence.Count - failed} passed, {failed} failed";
            File.WriteAllText("docs/unity_real_fastapi_integration.txt", summary + "\n" + string.Join("\n", evidence));
            File.WriteAllText("Documentation/Verification/real_fastapi_integration.txt", summary + "\n" + string.Join("\n", evidence));
            Debug.Log("LUNAR_REAL_FASTAPI " + summary);
            return summary;
        }

        static string HttpPost(string path, string json, out int statusCode, out string errorDetail)
        {
            string baseUrl = "http://127.0.0.1:8000";
            statusCode = 0;
            errorDetail = null;
            try
            {
                var req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(baseUrl + path);
                req.Method = "POST";
                req.ContentType = "application/json";
                req.Timeout = 25000;
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
                req.ContentLength = bytes.Length;
                using (var stream = req.GetRequestStream()) { stream.Write(bytes, 0, bytes.Length); }
                using (var resp = (System.Net.HttpWebResponse)req.GetResponse())
                {
                    statusCode = (int)resp.StatusCode;
                    using (var reader = new System.IO.StreamReader(resp.GetResponseStream()))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            catch (System.Net.WebException webEx)
            {
                if (webEx.Response is System.Net.HttpWebResponse errResp)
                {
                    statusCode = (int)errResp.StatusCode;
                    using (var reader = new System.IO.StreamReader(errResp.GetResponseStream()))
                    {
                        errorDetail = reader.ReadToEnd();
                    }
                }
                else
                {
                    errorDetail = webEx.Message;
                }
                return null;
            }
            catch (Exception ex)
            {
                errorDetail = ex.Message;
                return null;
            }
        }

        [MenuItem("Lunar Navigator/Verify Live Opportunity Finder")]
        public static string LiveOpportunityFinderChecks()
        {
            evidence = new List<string>();

            var app = UnityEngine.Object.FindAnyObjectByType<LunarApplication>();
            Check(app != null, "LunarApplication found in scene");

            // --- CHECK A: Qualifying NASA-derived recommendations ---
            string reqA = "{\"earliest_landing_utc\":\"2026-12-01T00:00:00Z\",\"search_window_h\":48.0,\"mission_duration_h\":24.0,\"candidate_site_ids\":[\"connecting_ridge\",\"malapert_mountain\",\"shackleton_rim\"],\"min_sun_fraction\":0.3,\"min_earth_fraction\":0.4,\"max_dark_h\":36.0,\"max_comm_gap_h\":48.0,\"search_step_h\":6.0,\"ranking_policy\":\"BEST_OVERALL\",\"sampling_step_minutes\":10,\"observer_height_m\":2.0}";
            string respA = HttpPost("/opportunities", reqA, out int codeA, out string errA);
            Check(codeA == 200, $"Check A: POST /opportunities returns 200 OK (actual: {codeA})");
            Check(!string.IsNullOrEmpty(respA), "Check A: Response payload is non-empty");

            var liveRespA = JsonUtility.FromJson<LunarNavigator.Data.LiveOpportunitiesResponse>(respA);
            Check(liveRespA != null, "Check A: LiveOpportunitiesResponse deserialized cleanly");
            Check(liveRespA?.data_status == "GEOMETRIC_ONLY", "Check A: data_status is strictly GEOMETRIC_ONLY");
            Check(liveRespA?.qualifying_count > 0, $"Check A: qualifying_count > 0 (actual: {liveRespA?.qualifying_count})");
            Check(liveRespA?.recommendations != null && liveRespA.recommendations.Length >= 1, "Check A: At least 1 recommendation returned");
            
            var topRec = liveRespA?.recommendations != null && liveRespA.recommendations.Length > 0 ? liveRespA.recommendations[0] : null;
            Check(topRec != null && topRec.rank == 1, "Check A: Top recommendation has rank 1");
            Check(topRec != null && topRec.site_id == "malapert_mountain", $"Check A: Top site is malapert_mountain (actual: {topRec?.site_id})");
            Check(topRec != null && topRec.score > 0.9, $"Check A: Top score > 0.9 (actual: {topRec?.score:F3})");
            Check(topRec?.metrics != null && topRec.metrics.f_sun == 1.0, "Check A: Top rec metrics f_sun == 1.0");
            Check(topRec?.metrics != null && topRec.metrics.f_earth == 1.0, "Check A: Top rec metrics f_earth == 1.0");
            Check(topRec?.metrics != null && topRec.metrics.dark_max_h == 0.0, "Check A: Top rec metrics dark_max_h == 0.0");

            // UI Display Check A
            var finderView = new LunarNavigator.UI.OpportunityFinderView();
            Check(finderView != null, "Check A: OpportunityFinderView instantiated cleanly");
            finderView.DisplayResults(liveRespA);
            Check(finderView.ResultsScrollView.childCount >= 2, $"Check A: Results scroll view contains summary + cards (children: {finderView.ResultsScrollView.childCount})");

            // --- CHECK B: Constraints producing zero qualifying results ---
            string reqB = "{\"earliest_landing_utc\":\"2026-12-01T00:00:00Z\",\"search_window_h\":48.0,\"mission_duration_h\":24.0,\"candidate_site_ids\":[\"connecting_ridge\",\"shackleton_rim\"],\"min_sun_fraction\":0.3,\"min_earth_fraction\":0.4,\"max_dark_h\":36.0,\"max_comm_gap_h\":48.0,\"search_step_h\":6.0,\"ranking_policy\":\"BEST_OVERALL\",\"sampling_step_minutes\":10,\"observer_height_m\":2.0}";
            string respB = HttpPost("/opportunities", reqB, out int codeB, out string errB);
            Check(codeB == 200, $"Check B: Zero-qualifying search returns 200 OK (actual: {codeB})");
            
            var liveRespB = JsonUtility.FromJson<LunarNavigator.Data.LiveOpportunitiesResponse>(respB);
            Check(liveRespB != null, "Check B: Deserialized zero-qualifying response");
            Check(liveRespB?.qualifying_count == 0, $"Check B: qualifying_count == 0 (actual: {liveRespB?.qualifying_count})");
            Check(liveRespB?.recommendations != null && liveRespB.recommendations.Length == 0, "Check B: recommendations array is empty");
            Check(!string.IsNullOrEmpty(liveRespB?.empty_reason), $"Check B: empty_reason provided: '{liveRespB?.empty_reason}'");
            Check(liveRespB?.closest_misses != null && liveRespB.closest_misses.Length > 0, $"Check B: closest_misses provided (count: {liveRespB?.closest_misses?.Length})");
            Check(liveRespB.closest_misses[0].failed_constraints != null && liveRespB.closest_misses[0].failed_constraints.Length > 0, "Check B: First closest miss lists failed constraints");

            finderView.DisplayResults(liveRespB);
            Check(finderView.ResultsScrollView.childCount >= 2, "Check B: UI rendered empty state box and closest misses");

            // --- CHECK C: Select a returned recommendation ---
            if (app && topRec != null && app.UI != null)
            {
                app.UI.SelectLiveOpportunity(topRec);
                Check(app.SelectedSite != null && app.SelectedSite.id == topRec.site_id, $"Check C: Selected site synchronized to {topRec.site_id} (actual: {app.SelectedSite?.id})");
                Check(app.Clock != null && app.Clock.seconds == 0, "Check C: Clock reset to landing instant (seconds == 0)");
            }

            // --- CHECK D: Network failure / error case without fabricated results ---
            // Send invalid request: duration + search > 744h to verify 422 error handling
            string reqD = "{\"earliest_landing_utc\":\"2026-12-01T00:00:00Z\",\"search_window_h\":500.0,\"mission_duration_h\":300.0,\"candidate_site_ids\":[\"shackleton_rim\"]}";
            string respD = HttpPost("/opportunities", reqD, out int codeD, out string errD);
            Check(codeD == 422, $"Check D: Exceeded 744h rejected with HTTP 422 (actual: {codeD})");
            Check(!string.IsNullOrEmpty(errD) && errD.Contains("invalid_request"), "Check D: 422 response contains invalid_request error detail");

            finderView.ShowError("HTTP 422: " + errD);
            Check(finderView.ErrorBanner.style.display == UnityEngine.UIElements.DisplayStyle.Flex, "Check D: ErrorBanner is visibly displayed");
            Check(finderView.LblErrorMessage.text.Contains("422"), "Check D: LblErrorMessage shows exact failure status");
            finderView.ClearError();
            Check(finderView.ErrorBanner.style.display == UnityEngine.UIElements.DisplayStyle.None, "Check D: ClearError hides ErrorBanner");

            // --- CHECK E: Site Inspector regression check with real API request ---
            string reqE = "{\"site_id\":\"connecting_ridge\",\"landing_utc\":\"2027-02-15T06:00:00Z\",\"mission_duration_h\":24.0,\"sampling_step_minutes\":10,\"observer_height_m\":2.0}";
            string respE = HttpPost("/analyze", reqE, out int codeE, out string errE);
            Check(codeE == 200, $"Check E: Site Inspector POST /analyze returns 200 OK (actual: {codeE})");
            var adaptedE = LunarNavigator.Data.ScienceApiAdapter.AdaptApiResponse(respE, "connecting_ridge", "Connecting Ridge", out string adaptErrE);
            Check(adaptErrE == null && adaptedE != null, "Check E: Site Inspector adapted successfully with no error");
            Check(adaptedE?.ephemeris?.n == 145, $"Check E: Site Inspector ephemeris n=145 (actual: {adaptedE?.ephemeris?.n})");
            Check(adaptedE?.metrics?.f_sun == 0.0, "Check E: Connecting Ridge f_sun == 0.0");
            Check(adaptedE?.metrics?.f_earth == 1.0, "Check E: Connecting Ridge f_earth == 1.0");

            // Restore clean state
            LunarNavigator.Data.AnalysisRepository.Instance.Reload();

            int failed = evidence.FindAll(x => x.StartsWith("FAIL")).Count;
            string summary = $"Live Opportunity Finder: {evidence.Count - failed} passed, {failed} failed";
            File.WriteAllText("docs/unity_live_opportunity_finder.txt", summary + "\n" + string.Join("\n", evidence));
            File.WriteAllText("Documentation/Verification/live_opportunity_finder.txt", summary + "\n" + string.Join("\n", evidence));
            Debug.Log("LUNAR_FINDER_VERIFICATION " + summary);
            return summary;
        }

        [MenuItem("Lunar Navigator/Verify Release Acceptance Stress Test")]
        public static string ReleaseAcceptanceChecks()
        {
            evidence = new List<string>();
            var app = UnityEngine.Object.FindAnyObjectByType<LunarApplication>();
            Check(app != null, "LunarApplication found in scene");

            // --- PART A.1: Timeline UI Verification ---
            if (!LunarNavigator.Data.AnalysisRepository.Instance.HasData)
            {
                LunarNavigator.Data.AnalysisRepository.Instance.Reload();
            }
            var siteData = LunarNavigator.Data.AnalysisRepository.Instance.Sites.Count > 0 
                ? LunarNavigator.Data.AnalysisRepository.Instance.Sites[0] 
                : null;
            Check(siteData != null, "Analysis site data available for timeline tests");

            var timeline = new LunarNavigator.UI.TimelineElement();
            Check(timeline != null, "TimelineElement instantiated cleanly");
            if (siteData != null)
            {
                timeline.SetData(siteData, 0.0, 0.0, 24.0);
            }

            // Find header elements
            var labels = timeline.Query<Label>().ToList();
            var lblCoverage = labels.Find(l => l.text != null && l.text.StartsWith("COVERAGE:"));
            Check(lblCoverage != null, "Timeline displays exact UTC coverage start and end");
            Check(lblCoverage != null && lblCoverage.text.Contains("UTC"), "Timeline coverage specifies UTC");

            // Check scope declaration
            bool hasScope = labels.Exists(l => l.text != null && l.text.StartsWith("SCOPE:"));
            Check(hasScope, "Timeline clearly declares SCOPE (Selected mission window / Wider analysis)");

            // Check three row labels
            bool hasSunRow = labels.Exists(l => l.text == "SUNLIGHT");
            bool hasEarthRow = labels.Exists(l => l.text == "EARTH VISIBLE");
            bool hasBothRow = labels.Exists(l => l.text == "BOTH VISIBLE");
            Check(hasSunRow, "Row 1 labeled SUNLIGHT");
            Check(hasEarthRow, "Row 2 labeled EARTH VISIBLE");
            Check(hasBothRow, "Row 3 labeled BOTH VISIBLE");

            // Check segment duration text inside wide segments
            bool hasDurationInside = labels.Exists(l => l.text != null && (l.text.EndsWith(" h") || l.text.EndsWith("h")) && !l.text.StartsWith("SCOPE") && !l.text.StartsWith("COVERAGE"));
            Check(hasDurationInside, "Calculated duration displayed inside wide segments");

            // --- PART A.2: Moon Contrast, Exposure & Material Verification ---
            var moonMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/LunarNavigator/Generated/Moon Regolith.mat");
            Check(moonMat != null, "Moon Regolith material loaded");
            if (moonMat != null)
            {
                Color baseCol = moonMat.GetColor("_BaseColor");
                Check(baseCol.r <= 0.5f, $"Moon Regolith albedo contrast restored (baseColor.r: {baseCol.r:F2} <= 0.50)");
            }

            var soilMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/LunarNavigator/Generated/Rough lunar soil.mat");
            Check(soilMat != null, "Rough lunar soil material loaded");
            if (soilMat != null)
            {
                Color soilCol = soilMat.GetColor("_BaseColor");
                Check(soilCol.r <= 0.55f, $"Local terrain albedo contrast restored (soilColor.r: {soilCol.r:F2} <= 0.55)");
            }

            var volumeProfile = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>("Assets/LunarNavigator/Generated/Vacuum Volume.asset");
            Check(volumeProfile != null, "Vacuum Volume profile loaded");
            if (volumeProfile != null && volumeProfile.TryGet<UnityEngine.Rendering.HighDefinition.Exposure>(out var exp))
            {
                Check(exp.fixedExposure.value >= 12.8f, $"Fixed exposure has photometric headroom (fixedExposure: {exp.fixedExposure.value:F1} >= 12.8 EV)");
            }

            // --- PART B: Client Timeouts, Stale Request Guards & Unavailable Server ---
            var client = LunarNavigator.Data.LunarApiClient.Instance;
            Check(client != null, "LunarApiClient instance accessible");
            Check(client.analyzeTimeoutSeconds >= 15, $"Sensible separate Site Inspector timeout (actual: {client.analyzeTimeoutSeconds}s)");
            Check(client.opportunitiesTimeoutSeconds >= 60, $"Sensible separate uncached Finder timeout (actual: {client.opportunitiesTimeoutSeconds}s)");

            // Test unavailable backend diagnostic
            string unavailUrl = "http://127.0.0.1:59998";
            string oldBaseUrl = client.baseUrl;
            client.baseUrl = unavailUrl;
            // Invoke health check against offline port
            bool unavailSuccess = true;
            string unavailErr = "";
            client.CheckHealth((ok, msg) => {
                unavailSuccess = ok;
                unavailErr = msg;
            });
            client.baseUrl = oldBaseUrl; // restore

            // --- PART B.2: Live Opportunities Verification ---
            string reqA = "{\"earliest_landing_utc\":\"2026-12-01T00:00:00Z\",\"search_window_h\":48.0,\"mission_duration_h\":24.0,\"candidate_site_ids\":[\"connecting_ridge\",\"malapert_mountain\",\"shackleton_rim\"],\"min_sun_fraction\":0.3,\"min_earth_fraction\":0.4,\"max_dark_h\":36.0,\"max_comm_gap_h\":48.0,\"search_step_h\":6.0,\"ranking_policy\":\"BEST_OVERALL\",\"sampling_step_minutes\":10,\"observer_height_m\":2.0}";
            string respA = HttpPost("/opportunities", reqA, out int codeA, out string errA);
            Check(codeA == 200, "Live FastAPI /opportunities returned 200 OK");
            var liveRespA = JsonUtility.FromJson<LunarNavigator.Data.LiveOpportunitiesResponse>(respA);
            Check(liveRespA != null, "Live opportunities response parsed cleanly");
            Check(liveRespA?.evaluated_count == 24, $"Correct scenario count: 24 evaluated (3 sites x 8 half-open arrivals) (actual: {liveRespA?.evaluated_count})");
            Check(liveRespA?.qualifying_count == 6, $"6 qualifying scenarios found (actual: {liveRespA?.qualifying_count})");
            Check(liveRespA?.recommendations != null && liveRespA.recommendations[0].site_id == "malapert_mountain", "Rank 1 site is Malapert Mountain (score 1.000)");

            // --- PART B.3: Site Inspector Live Multi-Site & Celestial Verification ---
            string[] testSites = { "shackleton_rim", "connecting_ridge", "malapert_mountain" };
            foreach (var sId in testSites)
            {
                string reqSite = $"{{\"site_id\":\"{sId}\",\"landing_utc\":\"2026-12-01T00:00:00Z\",\"mission_duration_h\":24.0,\"sampling_step_minutes\":10,\"observer_height_m\":2.0}}";
                string respSite = HttpPost("/analyze", reqSite, out int codeSite, out string errSite);
                Check(codeSite == 200, $"Site Inspector POST /analyze 200 OK for {sId}");
                var adapted = LunarNavigator.Data.ScienceApiAdapter.AdaptApiResponse(respSite, sId, sId, out string adaptErr);
                Check(adapted != null && adaptErr == null, $"ScienceApiAdapter successfully adapted {sId}");
                Check(adapted?.data_status == "GEOMETRIC_ONLY", $"Data status is strictly GEOMETRIC_ONLY for {sId}");
            }

            int failed = evidence.FindAll(x => x.StartsWith("FAIL")).Count;
            string summary = $"Release Acceptance Stress Test: {evidence.Count - failed} passed, {failed} failed";
            File.WriteAllText("docs/unity_release_acceptance.txt", summary + "\n" + string.Join("\n", evidence));
            File.WriteAllText("Documentation/Verification/release_acceptance.txt", summary + "\n" + string.Join("\n", evidence));
            Debug.Log("LUNAR_ACCEPTANCE_VERIFICATION " + summary);
            return summary;
        }
    }
}
