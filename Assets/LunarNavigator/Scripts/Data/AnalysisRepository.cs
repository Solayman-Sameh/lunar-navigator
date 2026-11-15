using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace LunarNavigator.Data
{
    public class AnalysisRepository
    {
        private static AnalysisRepository _instance;
        public static AnalysisRepository Instance => _instance ??= new AnalysisRepository();

        public List<SiteAnalysis> Sites { get; private set; } = new List<SiteAnalysis>();
        public OpportunitiesFile Opportunities { get; private set; }
        public LiveOpportunitiesResponse LiveOpportunities { get; private set; }
        public bool IsDemoMode { get; private set; } = true;
        public string DataStatus { get; private set; } = "ILLUSTRATIVE_DEMO";
        public List<string> DataProblems { get; private set; } = new List<string>();

        public DateTime T0 { get; private set; } = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc);
        public int StepSeconds { get; private set; } = 600;
        public int Count { get; private set; } = 0;
        public bool HasData => Sites.Count > 0;

        public SiteAnalysis GetSite(string id)
        {
            if (string.IsNullOrEmpty(id)) return Sites.Count > 0 ? Sites[0] : null;
            return Sites.Find(s => s.site != null && (s.site.id == id || s.site.id.Equals(id, StringComparison.OrdinalIgnoreCase)));
        }

        public DateTime TimeAt(SiteAnalysis site, int i)
        {
            int step = site?.ephemeris?.step_s ?? StepSeconds;
            DateTime baseT0 = T0;
            if (site?.ephemeris?.t0_utc != null && DateTime.TryParse(site.ephemeris.t0_utc, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed))
            {
                baseT0 = parsed;
            }
            return baseT0.AddSeconds(i * step);
        }

        public int IndexAt(SiteAnalysis site, DateTime utc)
        {
            int step = site?.ephemeris?.step_s ?? StepSeconds;
            int n = site?.ephemeris?.n ?? Count;
            if (n <= 0) return 0;

            DateTime baseT0 = T0;
            if (site?.ephemeris?.t0_utc != null && DateTime.TryParse(site.ephemeris.t0_utc, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed))
            {
                baseT0 = parsed;
            }

            double diff = (utc.ToUniversalTime() - baseT0).TotalSeconds;
            int idx = (int)Math.Round(diff / step);
            return Mathf.Clamp(idx, 0, n - 1);
        }

        public void SetDynamicAnalysis(SiteAnalysis dynamicSite)
        {
            if (dynamicSite == null) return;

            int idx = Sites.FindIndex(s => s.site != null && s.site.id == dynamicSite.site.id);
            if (idx >= 0)
            {
                Sites[idx] = dynamicSite;
            }
            else
            {
                Sites.Insert(0, dynamicSite);
            }

            IsDemoMode = (dynamicSite.data_status == "ILLUSTRATIVE_DEMO");
            DataStatus = dynamicSite.data_status;

            if (dynamicSite.ephemeris != null)
            {
                StepSeconds = dynamicSite.ephemeris.step_s;
                Count = dynamicSite.ephemeris.n;
                if (DateTime.TryParse(dynamicSite.ephemeris.t0_utc, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var t0))
                {
                    T0 = t0;
                }
            }
        }

        public void SetLiveOpportunities(LiveOpportunitiesResponse resp)
        {
            LiveOpportunities = resp;
        }

        public void Reload()
        {
            Sites.Clear();
            Opportunities = null;
            DataProblems.Clear();
            IsDemoMode = true;
            DataStatus = "ILLUSTRATIVE_DEMO";
            Count = 0;

            string baseDir = Path.Combine(Application.streamingAssetsPath, "analysis");
            if (!Directory.Exists(baseDir))
            {
                DataProblems.Add($"Analysis directory missing: {baseDir}");
                Debug.LogWarning($"[AnalysisRepository] Analysis directory not found at: {baseDir}");
                return;
            }

            string indexPath = Path.Combine(baseDir, "index.json");
            string stubIndexPath = Path.Combine(baseDir, "stub_index.json");
            bool hasRealIndex = File.Exists(indexPath);
            bool hasStubIndex = File.Exists(stubIndexPath);
            AnalysisIndex index = null;

            if (hasRealIndex)
            {
                try
                {
                    string json = File.ReadAllText(indexPath);
                    index = JsonUtility.FromJson<AnalysisIndex>(json);
                }
                catch (Exception ex)
                {
                    DataProblems.Add($"Error parsing index.json: {ex.Message}");
                    Debug.LogWarning($"[AnalysisRepository] Error parsing index.json: {ex.Message}");
                }
            }
            else if (hasStubIndex)
            {
                try
                {
                    string json = File.ReadAllText(stubIndexPath);
                    index = JsonUtility.FromJson<AnalysisIndex>(json);
                }
                catch (Exception ex)
                {
                    DataProblems.Add($"Error parsing stub_index.json: {ex.Message}");
                    Debug.LogWarning($"[AnalysisRepository] Error parsing stub_index.json: {ex.Message}");
                }
            }

            // Check if we should load real sites or scan for stubs
            List<string> candidateFiles = new List<string>();
            bool preferReal = false;

            if (index != null && index.sites != null && index.sites.Length > 0)
            {
                // Check if any listed file is non-stub
                foreach (var s in index.sites)
                {
                    if (!string.IsNullOrEmpty(s.file) && !Path.GetFileName(s.file).StartsWith("stub_", StringComparison.OrdinalIgnoreCase))
                    {
                        string fullPath = Path.Combine(baseDir, s.file);
                        if (File.Exists(fullPath))
                        {
                            preferReal = true;
                            break;
                        }
                    }
                }

                foreach (var s in index.sites)
                {
                    if (string.IsNullOrEmpty(s.file)) continue;
                    bool isStub = Path.GetFileName(s.file).StartsWith("stub_", StringComparison.OrdinalIgnoreCase);
                    if (preferReal && isStub) continue; // Skip stubs if real files are being loaded
                    candidateFiles.Add(Path.Combine(baseDir, s.file));
                }
            }

            // If index gave no files or wasn't found, look on disk
            if (candidateFiles.Count == 0)
            {
                // Scan for real site files first (*.json except index, opportunities, and stub_*)
                var allFiles = Directory.GetFiles(baseDir, "*.json");
                var realFiles = new List<string>();
                var stubFiles = new List<string>();

                foreach (var f in allFiles)
                {
                    string name = Path.GetFileName(f).ToLowerInvariant();
                    if (name == "index.json" || name == "stub_index.json" || name == "opportunities.json" || name == "stub_opportunities.json")
                        continue;
                    if (name.StartsWith("stub_")) stubFiles.Add(f);
                    else realFiles.Add(f);
                }

                if (realFiles.Count > 0)
                {
                    candidateFiles.AddRange(realFiles);
                    preferReal = true;
                }
                else
                {
                    candidateFiles.AddRange(stubFiles);
                    preferReal = false;
                }
            }

            // Parse each candidate file
            foreach (var filePath in candidateFiles)
            {
                if (!File.Exists(filePath))
                {
                    DataProblems.Add($"File referenced not found: {Path.GetFileName(filePath)}");
                    continue;
                }

                try
                {
                    string json = File.ReadAllText(filePath);
                    var siteData = JsonUtility.FromJson<SiteAnalysis>(json);
                    if (ValidateSiteData(siteData, filePath))
                    {
                        Sites.Add(siteData);
                    }
                }
                catch (Exception ex)
                {
                    DataProblems.Add($"Failed to parse {Path.GetFileName(filePath)}: {ex.Message}");
                }
            }

            // Determine Demo Mode & Data Status
            if (Sites.Count > 0)
            {
                bool allStubs = true;
                foreach (var s in Sites)
                {
                    if (s.data_status != "ILLUSTRATIVE_DEMO")
                    {
                        allStubs = false;
                        break;
                    }
                }
                IsDemoMode = allStubs;
                DataStatus = Sites[0].data_status;

                // Sync global time reference from first site
                if (Sites[0].ephemeris != null)
                {
                    StepSeconds = Sites[0].ephemeris.step_s;
                    Count = Sites[0].ephemeris.n;
                    if (DateTime.TryParse(Sites[0].ephemeris.t0_utc, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var t0))
                    {
                        T0 = t0;
                    }
                }
            }
            else
            {
                IsDemoMode = true;
                DataStatus = preferReal ? "INVALID_DATA" : "ILLUSTRATIVE_DEMO";
                DataProblems.Add(preferReal ? "All targeted real scientific candidate files failed validation." : "No valid site analysis files loaded.");
            }

            // Load opportunities file if present
            string oppFileName = index?.opportunities_file;
            string oppPath = null;
            if (!string.IsNullOrEmpty(oppFileName))
            {
                oppPath = Path.Combine(baseDir, oppFileName);
            }
            if (string.IsNullOrEmpty(oppPath) || !File.Exists(oppPath))
            {
                // Fallbacks: opportunities.json or stub_opportunities.json
                string realOpp = Path.Combine(baseDir, "opportunities.json");
                string stubOpp = Path.Combine(baseDir, "stub_opportunities.json");
                if (File.Exists(realOpp)) oppPath = realOpp;
                else if (File.Exists(stubOpp)) oppPath = stubOpp;
            }

            if (!string.IsNullOrEmpty(oppPath) && File.Exists(oppPath))
            {
                try
                {
                    string oppJson = File.ReadAllText(oppPath);
                    Opportunities = JsonUtility.FromJson<OpportunitiesFile>(oppJson);
                }
                catch (Exception ex)
                {
                    DataProblems.Add($"Failed to parse opportunities file ({Path.GetFileName(oppPath)}): {ex.Message}");
                }
            }

            Debug.Log($"[AnalysisRepository] Loaded {Sites.Count} sites. IsDemoMode={IsDemoMode}, Status={DataStatus}, Opportunities loaded={(Opportunities != null)}");
        }

        public bool ValidateSiteData(SiteAnalysis site, string path)
        {
            string fileName = Path.GetFileName(path);
            if (site == null)
            {
                DataProblems.Add($"{fileName}: Deserialized object is null.");
                return false;
            }
            if (site.site == null || string.IsNullOrEmpty(site.site.id))
            {
                DataProblems.Add($"{fileName}: Missing site metadata or ID.");
                return false;
            }
            if (site.ephemeris == null)
            {
                DataProblems.Add($"{fileName}: Missing ephemeris section.");
                return false;
            }
            int n = site.ephemeris.n;
            if (n <= 0)
            {
                DataProblems.Add($"{fileName}: Ephemeris sample count n <= 0.");
                return false;
            }
            if (site.ephemeris.sun_az_deg == null || site.ephemeris.sun_az_deg.Length != n ||
                site.ephemeris.sun_el_deg == null || site.ephemeris.sun_el_deg.Length != n ||
                site.ephemeris.earth_az_deg == null || site.ephemeris.earth_az_deg.Length != n ||
                site.ephemeris.earth_el_deg == null || site.ephemeris.earth_el_deg.Length != n)
            {
                DataProblems.Add($"{fileName}: Ephemeris array length does not match n={n}.");
                return false;
            }
            if (site.terrain == null || site.terrain.horizon_el_deg == null || site.terrain.horizon_el_deg.Length != 360)
            {
                DataProblems.Add($"{fileName}: Terrain horizon_el_deg must have exactly 360 entries.");
                return false;
            }
            if (Math.Abs(site.terrain.az_step_deg - 1.0) > 0.001)
            {
                DataProblems.Add($"{fileName}: Terrain az_step_deg must be 1.0 in contract v1 (found {site.terrain.az_step_deg}).");
                return false;
            }
            if (site.visibility == null || site.visibility.sun_visible == null || site.visibility.sun_visible.Length != n ||
                site.visibility.earth_visible == null || site.visibility.earth_visible.Length != n)
            {
                DataProblems.Add($"{fileName}: Visibility array length does not match n={n}.");
                return false;
            }

            // Numerical sanity and finiteness validation
            for (int k = 0; k < 360; k++)
            {
                double h = site.terrain.horizon_el_deg[k];
                if (!double.IsFinite(h) || h < -50.0 || h > 75.0)
                {
                    DataProblems.Add($"{fileName}: Non-finite or implausible horizon elevation at az={k}° ({h}).");
                    return false;
                }
            }

            // Sample sanity check
            for (int j = 0; j < Math.Min(n, 100); j++)
            {
                if (!double.IsFinite(site.ephemeris.sun_az_deg[j]) || !double.IsFinite(site.ephemeris.sun_el_deg[j]) ||
                    !double.IsFinite(site.ephemeris.earth_az_deg[j]) || !double.IsFinite(site.ephemeris.earth_el_deg[j]))
                {
                    DataProblems.Add($"{fileName}: Non-finite celestial ephemeris value detected at sample {j}.");
                    return false;
                }
            }

            // Incomplete DEM coverage check
            if (site.terrain.incomplete_sectors != null && site.terrain.incomplete_sectors.Length > 0)
            {
                foreach (var sec in site.terrain.incomplete_sectors)
                {
                    if (sec == null || !double.IsFinite(sec.az_from_deg) || !double.IsFinite(sec.az_to_deg) ||
                        sec.az_from_deg < 0.0 || sec.az_from_deg > 360.0 || sec.az_to_deg < 0.0 || sec.az_to_deg > 360.0)
                    {
                        DataProblems.Add($"{fileName}: Incomplete sector angles must be finite in range [0, 360].");
                        return false;
                    }
                }
                DataProblems.Add($"{fileName}: Site has {site.terrain.incomplete_sectors.Length} incomplete DEM sector(s); uncertainty indicator active.");
            }

            return true;
        }
    }
}
