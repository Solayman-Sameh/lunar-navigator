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
        public int SelectionRevision { get; private set; }
        public SelectionLandmark Landmark { get; private set; }
        public LunarSite CustomSite { get; private set; }
        MeshCollider globePicker;
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
            Landmark = gameObject.AddComponent<SelectionLandmark>();
            Landmark.Initialize(this);
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
            SelectionRevision++;
            Data.AnalysisRepository.Instance.ActivateSite(site.id);
            if (Clock) { Clock.SetTime(0); Clock.paused = true; }
            if (Director) Director.Select(site);
            if (World) World.UpdateSkyline();
            if (UI) UI.OnSiteChanged(site);
        }

        public bool PickLocation(Ray ray)
        {
            if (!World || !World.globe) return false;
            if (!globePicker)
            {
                var mesh = World.globe.GetComponent<MeshFilter>();
                if (!mesh || !mesh.sharedMesh) return false;
                globePicker = World.globe.GetComponent<MeshCollider>();
                if (!globePicker) globePicker = World.globe.AddComponent<MeshCollider>();
                globePicker.sharedMesh = mesh.sharedMesh;
            }
            Physics.SyncTransforms();
            if (!globePicker.Raycast(ray, out var hit, float.MaxValue)) return false;
            // Transform back into the rendered globe's own geographic axes.
            var geographic = LunarCoordinates.Geographic(World.globe.transform.InverseTransformPoint(hit.point));
            SelectCustom(geographic.x, geographic.y);
            return true;
        }

        public void SelectCustom(double latitude, double longitude)
        {
            if (double.IsNaN(latitude) || double.IsInfinity(latitude) || latitude < -90 || latitude > 90 ||
                double.IsNaN(longitude) || double.IsInfinity(longitude) || longitude < -180 || longitude > 180)
                throw new System.ArgumentOutOfRangeException("Invalid lunar coordinates");
            if (CustomSite) Destroy(CustomSite);
            CustomSite = ScriptableObject.CreateInstance<LunarSite>();
            CustomSite.IsCustom = true;
            CustomSite.CustomLatitude = latitude; CustomSite.CustomLongitude = longitude;
            CustomSite.latitude = (float)latitude; CustomSite.longitude = (float)longitude;
            CustomSite.id = "custom:" + latitude.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ":" + longitude.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
            CustomSite.displayName = "Custom Location (unverified)";
            CustomSite.provenance = "User-picked coordinate; no terrain or touchdown verification.";
            SelectSite(CustomSite);
            UI?.SetMode(AppMode.Inspect);
        }

        void OnDestroy() { if (CustomSite) Destroy(CustomSite); }

        public void Back()
        {
            if (UI != null && UI.Close()) return;
            if (Director == null) return;
            if (Director.Mode == CameraMode.Approach) Director.Skip();
            else Director.ChangeMode(Director.Mode == CameraMode.Surface ? CameraMode.LocalOrbit : CameraMode.Global);
        }
    }
}
