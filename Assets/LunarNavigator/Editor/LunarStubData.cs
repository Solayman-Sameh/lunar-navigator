using System;
using System.Collections.Generic;
using System.IO;
using LunarNavigator.Data;
using UnityEditor;
using UnityEngine;

namespace LunarNavigator.Editor
{
    public static class LunarStubData
    {
        [MenuItem("Lunar Navigator/Data/Generate Stub Data")]
        public static void GenerateStubs()
        {
            string outDir = Path.Combine(Application.streamingAssetsPath, "analysis");
            if (!Directory.Exists(outDir))
            {
                Directory.CreateDirectory(outDir);
            }

            var sites = new[]
            {
                new { id = "shackleton_rim", name = "Shackleton Rim", lat = -89.6f, lon = 129.0f, phase = 0.0f, hasIncomplete = true },
                new { id = "malapert_mountain", name = "Malapert Mountain", lat = -85.9f, lon = 2.9f, phase = 1.2f, hasIncomplete = false },
                new { id = "connecting_ridge", name = "Connecting Ridge", lat = -89.4f, lon = -137.5f, phase = 2.4f, hasIncomplete = false }
            };

            const int n = 4465; // 31 days at 600s
            const int step_s = 600;
            const string t0_utc = "2026-12-01T00:00:00Z";
            DateTime t0 = DateTime.Parse(t0_utc, null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal);

            var indexSites = new List<IndexSite>();

            foreach (var s in sites)
            {
                var analysis = new SiteAnalysis
                {
                    schema_version = 1,
                    data_status = "ILLUSTRATIVE_DEMO",
                    algorithm_version = "0.1.0",
                    generated_utc = "2026-11-15T17:00:00Z",
                    site = new SiteInfo
                    {
                        id = s.id,
                        name = s.name,
                        lat_deg = s.lat,
                        lon_east_deg = s.lon,
                        observer_height_m = 2.0f,
                        provenance = "SYNTHETIC. Not real data. Generated for demonstrative verification."
                    },
                    ephemeris = new Ephemeris
                    {
                        source = "Synthetic analytical ephemeris for simulation testing",
                        t0_utc = t0_utc,
                        step_s = step_s,
                        n = n,
                        sun_az_deg = new double[n],
                        sun_el_deg = new double[n],
                        earth_az_deg = new double[n],
                        earth_el_deg = new double[n]
                    },
                    terrain = new TerrainInfo
                    {
                        source = "Synthetic harmonic horizon profile",
                        resolution_m = 100,
                        max_range_km = 150,
                        az_step_deg = 1.0f,
                        horizon_el_deg = new double[360],
                        incomplete_sectors = s.hasIncomplete ? new[] { new SectorRange { az_from_deg = 200.0f, az_to_deg = 215.0f } } : new SectorRange[0]
                    },
                    visibility = new VisibilityInfo
                    {
                        sun_margin_deg = 0.0f,
                        sun_visible = new int[n],
                        earth_visible = new int[n]
                    }
                };

                // Generate 360-degree skyline
                for (int deg = 0; deg < 360; deg++)
                {
                    float a = deg * Mathf.Deg2Rad;
                    float h = 0.8f + 1.2f * Mathf.Sin(3f * a) + 0.7f * Mathf.Sin(7f * a + s.phase);
                    analysis.terrain.horizon_el_deg[deg] = Math.Round(h, 4);
                }

                // Generate tracks and compute visibility
                for (int i = 0; i < n; i++)
                {
                    double t_sec = i * step_s;
                    double t_days = t_sec / 86400.0;

                    double sunAz = (t_days / 29.53059 * 360.0 + s.phase * 50.0) % 360.0;
                    if (sunAz < 0.0) sunAz += 360.0;
                    double sunEl = 1.4 * Math.Sin(2.0 * Math.PI * t_days / 29.53059 + s.phase);

                    double earthAz = (t_days / 27.32 * 360.0 + s.phase * 30.0 + 90.0) % 360.0;
                    if (earthAz < 0.0) earthAz += 360.0;
                    double earthEl = 6.0 * Math.Sin(2.0 * Math.PI * t_days / 27.32 + s.phase + 0.5);

                    analysis.ephemeris.sun_az_deg[i] = Math.Round(sunAz, 4);
                    analysis.ephemeris.sun_el_deg[i] = Math.Round(sunEl, 4);
                    analysis.ephemeris.earth_az_deg[i] = Math.Round(earthAz, 4);
                    analysis.ephemeris.earth_el_deg[i] = Math.Round(earthEl, 4);

                    // Compute visibility flags using exact contract formula
                    double sunH = VisibilityMath.SkylineAt(analysis.terrain.horizon_el_deg, sunAz);
                    analysis.visibility.sun_visible[i] = (sunEl > sunH) ? 1 : 0;

                    double earthH = VisibilityMath.SkylineAt(analysis.terrain.horizon_el_deg, earthAz);
                    analysis.visibility.earth_visible[i] = (earthEl > earthH) ? 1 : 0;
                }

                // Reference metrics: 9 entries (24, 72, 168 h at 3 different start offsets)
                var refList = new List<ReferenceMetric>();
                int[] durations = { 24, 72, 168 };
                int[] startDayOffsets = { 0, 5, 12 };

                foreach (int dayOffset in startDayOffsets)
                {
                    int startIdx = dayOffset * (86400 / step_s);
                    foreach (int dur in durations)
                    {
                        int count = dur * 3600 / step_s;
                        var m = VisibilityMath.Compute(analysis, startIdx, count);
                        refList.Add(new ReferenceMetric
                        {
                            start_utc = m.start_utc,
                            duration_h = (int)System.Math.Round(m.duration_h),
                            f_sun = Math.Round(m.f_sun, 4),
                            f_earth = Math.Round(m.f_earth, 4),
                            f_both = Math.Round(m.f_both, 4),
                            dark_max_h = Math.Round(m.dark_max_h, 4),
                            comm_gap_max_h = Math.Round(m.comm_gap_max_h, 4)
                        });
                    }
                }
                analysis.reference_metrics = refList.ToArray();

                string fileName = $"stub_{s.id}.json";
                string json = JsonUtility.ToJson(analysis, true);
                File.WriteAllText(Path.Combine(outDir, fileName), json);

                indexSites.Add(new IndexSite
                {
                    id = s.id,
                    name = s.name,
                    file = fileName,
                    data_status = "ILLUSTRATIVE_DEMO"
                });
            }

            // Generate opportunities stub
            var opp = new OpportunitiesFile
            {
                schema_version = 1,
                algorithm_version = "0.1.0",
                data_status = "ILLUSTRATIVE_DEMO",
                request = new OpportunityRequest
                {
                    earliest_landing_utc = "2026-12-01T00:00:00Z",
                    search_days = 14,
                    landing_step_h = 6,
                    duration_h = 72,
                    constraints = new Constraints
                    {
                        min_sun_fraction = 0.30f,
                        min_earth_fraction = 0.40f,
                        max_dark_h = 36.0f,
                        max_comm_gap_h = 48.0f
                    },
                    constraints_note = "Illustrative, user-editable. Not NASA standards."
                },
                candidate_sites = new[] { "malapert_mountain", "shackleton_rim", "connecting_ridge" },
                scoring_note = "score = 0.4*f_sun + 0.3*f_earth + 0.2*f_both + 0.1*(1-min(1,dark_max_h/duration_h))",
                best_overall = new[]
                {
                    new OpportunityResult { rank = 1, site_id = "shackleton_rim", landing_utc = "2026-12-03T12:00:00Z", duration_h = 72, score = 84.5f, f_sun = 0.88f, f_earth = 0.92f, f_both = 0.85f, dark_max_h = 6.0f, comm_gap_max_h = 4.0f },
                    new OpportunityResult { rank = 2, site_id = "connecting_ridge", landing_utc = "2026-12-05T06:00:00Z", duration_h = 72, score = 78.2f, f_sun = 0.79f, f_earth = 0.85f, f_both = 0.74f, dark_max_h = 12.0f, comm_gap_max_h = 8.0f },
                    new OpportunityResult { rank = 3, site_id = "malapert_mountain", landing_utc = "2026-12-07T18:00:00Z", duration_h = 72, score = 71.0f, f_sun = 0.70f, f_earth = 0.80f, f_both = 0.65f, dark_max_h = 18.0f, comm_gap_max_h = 10.0f }
                },
                earliest_qualifying = new[]
                {
                    new OpportunityResult { rank = 1, site_id = "shackleton_rim", landing_utc = "2026-12-01T06:00:00Z", duration_h = 72, score = 79.0f, f_sun = 0.80f, f_earth = 0.88f, f_both = 0.75f, dark_max_h = 8.0f, comm_gap_max_h = 6.0f },
                    new OpportunityResult { rank = 2, site_id = "malapert_mountain", landing_utc = "2026-12-01T12:00:00Z", duration_h = 72, score = 68.5f, f_sun = 0.65f, f_earth = 0.78f, f_both = 0.60f, dark_max_h = 20.0f, comm_gap_max_h = 12.0f },
                    new OpportunityResult { rank = 3, site_id = "connecting_ridge", landing_utc = "2026-12-02T00:00:00Z", duration_h = 72, score = 74.0f, f_sun = 0.75f, f_earth = 0.82f, f_both = 0.68f, dark_max_h = 14.0f, comm_gap_max_h = 9.0f }
                },
                closest_misses = new ClosestMiss[0],
                evaluated_count = 168,
                rejected_count = 124
            };

            string oppJson = JsonUtility.ToJson(opp, true);
            string oppTarget = Path.Combine(outDir, "stub_opportunities.json");
            File.WriteAllText(oppTarget, oppJson);

            // Index file
            var indexData = new AnalysisIndex
            {
                schema_version = 1,
                sites = indexSites.ToArray(),
                opportunities_file = "stub_opportunities.json"
            };

            string indexTarget = Path.Combine(outDir, "index.json");
            // If real index exists, do NOT overwrite it; write stub_index.json instead
            if (File.Exists(indexTarget))
            {
                string existing = File.ReadAllText(indexTarget);
                if (!existing.Contains("stub_"))
                {
                    indexTarget = Path.Combine(outDir, "stub_index.json");
                }
            }

            File.WriteAllText(indexTarget, JsonUtility.ToJson(indexData, true));
            AssetDatabase.Refresh();

            Debug.Log($"[LunarStubData] Synthetic stub datasets successfully generated at: {outDir}");
        }

        [MenuItem("Lunar Navigator/Data/Reload")]
        public static void ReloadData()
        {
            AnalysisRepository.Instance.Reload();
        }
    }
}
