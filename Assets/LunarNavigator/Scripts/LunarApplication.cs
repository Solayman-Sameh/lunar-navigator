using UnityEngine;

namespace LunarNavigator
{
    public class LunarApplication : MonoBehaviour, ISiteCatalog
    {
        public NavigatorConfig config;
        public LunarSite[] sites;
        public CameraDirector Director;
        public DemoSimulation Clock;
        public LunarWorld World;
        public NavigatorUI UI;
        public LunarSite SelectedSite { get; private set; }
        public LunarSite[] Sites => sites;

        void Awake()
        {
            Application.targetFrameRate = 60;
            SelectedSite = (sites != null && sites.Length > 0) ? sites[0] : null;
            if (Director && SelectedSite)
            {
                Director.selected = SelectedSite;
                Director.Select(SelectedSite);
            }
            if (Clock) Clock.Initialize(this);
            if (World) World.Initialize(this);
            if (UI) UI.Initialize(this);
        }

        void Start()
        {
            // Sync authoritative coordinates from GET /sites if available
            if (Data.LunarApiClient.Instance != null)
            {
                Data.LunarApiClient.Instance.GetSites((success, json, err) =>
                {
                    if (success && !string.IsNullOrEmpty(json))
                    {
                        SyncSitesFromCatalog(json);
                    }
                });
            }
        }

        public void SyncSitesFromCatalog(string json)
        {
            try
            {
                var cat = JsonUtility.FromJson<Data.SitesResponse>(json);
                if (cat?.sites != null && sites != null)
                {
                    foreach (var cr in cat.sites)
                    {
                        var s = System.Array.Find(sites, x => x.id == cr.id);
                        if (s != null)
                        {
                            s.latitude = (float)cr.lat_deg;
                            s.longitude = (float)cr.lon_east_deg;
                            s.displayName = cr.name;
                            s.provenance = cr.verification_scope + " (" + cr.coordinate_frame + ")";
                        }
                    }
                    if (SelectedSite != null)
                    {
                        if (Director) Director.Select(SelectedSite);
                        if (World) World.UpdateSkyline();
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[LunarApplication] Error syncing /sites: " + ex.Message);
            }
        }

        public void SelectSite(LunarSite site)
        {
            if (site == null) return;
            SelectedSite = site;
            if (Director) Director.Select(site);
            if (World) World.UpdateSkyline();
            if (UI) UI.OnSiteChanged(site);
        }

        public void Back()
        {
            if (UI != null && UI.Close()) return;
            if (Director == null) return;
            if (Director.Mode == CameraMode.Approach) Director.Skip();
            else Director.ChangeMode(Director.Mode == CameraMode.Surface ? CameraMode.LocalOrbit : CameraMode.Global);
        }
    }
}
