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
        public bool terrainVisibilityAid = false;
        public string Preset { get; private set; } = "Balanced";
        LunarApplication app;
        Transform boulderPatch;
        Light visibilityAidLight;

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
                if (!elGo.TryGetComponent<Light>(out earthlight))
                {
                    earthlight = elGo.AddComponent<Light>();
                }
                earthlight.type = LightType.Directional;
                earthlight.color = new Color(0.48f, 0.68f, 0.95f, 1f);
                earthlight.shadows = LightShadows.None;
                earthlight.intensity = 450f;
                if (!elGo.TryGetComponent<HDAdditionalLightData>(out var hdLight))
                {
                    hdLight = elGo.AddComponent<HDAdditionalLightData>();
                }
            }

            // Optional visualization aid for inspecting crater floors in local modes
            if (!visibilityAidLight)
            {
                var valGo = GameObject.Find("Terrain Visibility Aid · subtle exploration fill");
                if (!valGo)
                {
                    valGo = new GameObject("Terrain Visibility Aid · subtle exploration fill");
                    valGo.transform.SetParent(transform, false);
                }
                if (!valGo.TryGetComponent<Light>(out var l))
                {
                    l = valGo.AddComponent<Light>();
                }
                visibilityAidLight = l;
                visibilityAidLight.type = LightType.Directional;
                visibilityAidLight.color = new Color(0.72f, 0.82f, 0.95f, 1f);
                visibilityAidLight.shadows = LightShadows.None;
                visibilityAidLight.intensity = 2800f;
                if (!valGo.TryGetComponent<HDAdditionalLightData>(out var hdValLight))
                {
                    hdValLight = valGo.AddComponent<HDAdditionalLightData>();
                }
                visibilityAidLight.enabled = false;
            }

            // These distant display objects visualize directions, not occulting geometry.
            if (earth)
            {
                earth.localScale = Vector3.one * 320f;
                if (earth.TryGetComponent<MeshRenderer>(out var earthRend))
                {
                    earthRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    var mat = earthRend.material;
                    if (mat)
                    {
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissiveColor", new Color(0.28f, 0.58f, 0.98f, 1f));
                        mat.SetFloat("_EmissiveIntensity", 1200f);
                        mat.SetFloat("_AlbedoAffectEmissive", 1f);
                    }
                }
            }
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

        public void EnsureVisibilityAidLight()
        {
            if (visibilityAidLight) return;
            var valGo = GameObject.Find("Terrain Visibility Aid · subtle exploration fill");
            if (!valGo)
            {
                valGo = new GameObject("Terrain Visibility Aid · subtle exploration fill");
                valGo.transform.SetParent(transform, false);
            }
            if (!valGo.TryGetComponent<Light>(out var l))
            {
                l = valGo.AddComponent<Light>();
            }
            visibilityAidLight = l;
            visibilityAidLight.type = LightType.Directional;
            visibilityAidLight.color = new Color(0.72f, 0.82f, 0.95f, 1f);
            visibilityAidLight.shadows = LightShadows.None;
            visibilityAidLight.intensity = 2800f;
            if (!valGo.TryGetComponent<HDAdditionalLightData>(out var hdValLight))
            {
                hdValLight = valGo.AddComponent<HDAdditionalLightData>();
            }
            visibilityAidLight.enabled = false;
        }

        void Update()
        {
            if (!app) return;
            EnsureVisibilityAidLight();
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

            if (visibilityAidLight)
            {
                bool active = terrainVisibilityAid && (app.Director.Mode == CameraMode.LocalOrbit || app.Director.Mode == CameraMode.Surface);
                visibilityAidLight.enabled = active;
                if (active)
                {
                    visibilityAidLight.transform.rotation = camera.transform.rotation;
                }
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
