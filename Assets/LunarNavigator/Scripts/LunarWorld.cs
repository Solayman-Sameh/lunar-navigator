using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using LunarNavigator.Data;
using LunarNavigator.Visual;

namespace LunarNavigator
{
    public class LunarWorld : MonoBehaviour
    {
        public Light sunlight;
        public Light earthlight;
        public Transform earth, sun;
        public GameObject stars;
        public LunarTerrain terrain;
        public GameObject globe;
        public SkylineRing skylineRing;
        public bool markersVisible = true, aidsVisible = true;
        public string Preset { get; private set; } = "Balanced";
        LunarApplication app;
        Transform boulderPatch;

        public void Initialize(LunarApplication value)
        {
            app = value;
            ApplyQuality("Balanced");

            // Setup Earthlight for realistic polar indirect fill (Earthshine)
            if (!earthlight)
            {
                var elGo = GameObject.Find("Earthlight · indirect lunar illumination");
                if (!elGo)
                {
                    elGo = new GameObject("Earthlight · indirect lunar illumination");
                    elGo.transform.SetParent(transform, false);
                }
                earthlight = elGo.GetComponent<Light>();
                if (!earthlight)
                {
                    earthlight = elGo.AddComponent<Light>();
                }
                earthlight.type = LightType.Directional;
                earthlight.color = new Color(0.48f, 0.68f, 0.95f, 1f);
                earthlight.shadows = LightShadows.None;
                earthlight.intensity = 450f;
                var hdLight = elGo.GetComponent<HDAdditionalLightData>();
                if (!hdLight)
                {
                    hdLight = elGo.AddComponent<HDAdditionalLightData>();
                }
            }

            // These distant display objects visualize directions, not occulting geometry.
            if (earth && earth.TryGetComponent<MeshRenderer>(out var earthRend))
                earthRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (sun && sun.TryGetComponent<MeshRenderer>(out var sunRend))
                sunRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            if (terrain)
            {
                boulderPatch = terrain.transform.Find("Regolith boulders · combined");
            }

            if (!skylineRing)
            {
                var ringGo = new GameObject("Skyline Ring · Horizon Profile");
                skylineRing = ringGo.AddComponent<SkylineRing>();
                var basaltMat = Resources.Load<Material>("Basalt rocks");
                if (!basaltMat && terrain && terrain.TryGetComponent<MeshRenderer>(out var tRend))
                {
                    basaltMat = tRend.sharedMaterial;
                }
                skylineRing.ringMaterial = basaltMat;
                if (ringGo.TryGetComponent<MeshRenderer>(out var mr))
                {
                    mr.sharedMaterial = basaltMat;
                }
            }

            UpdateSkyline();
        }

        public void UpdateSkyline()
        {
            if (skylineRing && app && app.SelectedSite)
            {
                var siteData = AnalysisRepository.Instance.GetSite(app.SelectedSite.id);
                skylineRing.BuildForSite(siteData, app.SelectedSite, app.Director.Frame);
            }
        }

        public void ApplyQuality(string preset)
        {
            Preset = preset;
            string wanted = preset == "Cinematic" ? "High Fidelity" : preset == "Performance" ? "Performant" : "Balanced";
            int level = System.Array.FindIndex(QualitySettings.names, name => name == wanted);
            if (level >= 0 && QualitySettings.GetQualityLevel() != level) QualitySettings.SetQualityLevel(level, true);
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            QualitySettings.globalTextureMipmapLimit = preset == "Performance" ? 1 : 0;
            QualitySettings.lodBias = preset == "Performance" ? .7f : preset == "Cinematic" ? 2 : 1;
            if (sunlight)
            {
                sunlight.shadows = preset == "Performance" ? LightShadows.Hard : LightShadows.Soft;
                sunlight.shadowCustomResolution = preset == "Cinematic" ? 4096 : preset == "Balanced" ? 2048 : 1024;
            }
            if (sunlight && sunlight.TryGetComponent<HDAdditionalLightData>(out var lightData))
            {
                lightData.SetShadowResolution(preset == "Cinematic" ? 4096 : preset == "Balanced" ? 2048 : 1024);
                lightData.SetShadowResolutionOverride(true);
            }
            if (app && app.Director && app.Director.view && app.Director.view.TryGetComponent<HDAdditionalCameraData>(out var data))
            {
                data.antialiasing = preset == "Performance"
                    ? HDAdditionalCameraData.AntialiasingMode.FastApproximateAntialiasing
                    : HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;
            }
        }

        void Update()
        {
            if (!app) return;
            var camera = app.Director.view;
            var sd = app.Clock.Provider.SunDirection(app.Clock.Utc);
            sunlight.transform.rotation = Quaternion.LookRotation(-sd, Vector3.up);

            var ed = app.Clock.Provider.EarthDirection(app.Clock.Utc);
            earth.position = camera.transform.position + ed * 14000;
            sun.position = camera.transform.position + sd * 15000;

            if (earthlight)
            {
                earthlight.transform.rotation = Quaternion.LookRotation(-ed, Vector3.up);
                earthlight.enabled = app.Director.Mode != CameraMode.Global;
            }

            bool nearPatch = Vector3.Distance(camera.transform.position, terrain.Anchor) < 330;
            bool local = app.Director.Mode == CameraMode.LocalOrbit || app.Director.Mode == CameraMode.Surface;
            bool handoff = (app.Director.Mode == CameraMode.Approach || app.Director.IsTransitioning) && nearPatch;
            bool terrainVisible = local || handoff;
            terrain.gameObject.SetActive(terrainVisible);
            
            // Keep Moon globe active in all modes to maintain global horizon & curvature
            if (globe)
            {
                globe.SetActive(true);
            }

            if (skylineRing)
            {
                skylineRing.UpdateVisibility(app.Director.Mode, camera.transform.position);
            }
            if (boulderPatch)
            {
                // Regolith boulders provide immediate scale and tactile surface relief
                boulderPatch.gameObject.SetActive(true);
            }
        }
    }
}
