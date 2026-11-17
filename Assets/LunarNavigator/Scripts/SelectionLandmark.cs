using UnityEngine;

namespace LunarNavigator
{
    // Runtime-only geographic landmark. Visual aid, never scientific terrain.
    public class SelectionLandmark : MonoBehaviour
    {
        LunarApplication app;
        LineRenderer ring, stem, pole;
        LineRenderer outerRing, outerStem;
        Material poleMaterial;
        MeshCollider globeSurface;
        Material material;
        Material darkMaterial;
        float pulseUntil;
        public Vector3 Position { get; private set; }
        public void Initialize(LunarApplication value)
        {
            app = value;
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var c = transform.GetChild(i).gameObject;
                DestroyImmediate(c);
            }
            if (material) Destroy(material);
            if (darkMaterial) Destroy(darkMaterial);
            if (poleMaterial) Destroy(poleMaterial);

            var unlitShader = Shader.Find("HDRP/Unlit");
            material = new Material(unlitShader);
            material.SetColor("_UnlitColor", Color.black);
            material.SetColor("_EmissiveColor", new Color(.15f, 1f, .75f));
            material.SetFloat("_EmissiveExposureWeight", 0);

            // Dual-contrast border: dark outer stroke ensures contrast on bright regolith
            darkMaterial = new Material(unlitShader);
            darkMaterial.SetColor("_UnlitColor", new Color(.04f, .05f, .07f, 1f));
            darkMaterial.SetColor("_EmissiveColor", Color.black);
            darkMaterial.SetFloat("_EmissiveExposureWeight", 0);

            outerRing = MakeLine("Active location outer ring", 49, true, darkMaterial, 0);
            outerStem = MakeLine("Active location outer beacon", 2, false, darkMaterial, 0);
            ring = MakeLine("Active location ring", 49, true, material, 1);
            stem = MakeLine("Active location beacon", 2, false, material, 1);
            pole = MakeLine("South pole orientation guide", 129, true, null, 0);
            poleMaterial = new Material(material);
            poleMaterial.SetColor("_EmissiveColor", new Color(.05f, .2f, .25f));
            pole.sharedMaterial = poleMaterial; pole.startWidth = pole.endWidth = 1.2f;
            for(int i = 0; i < pole.positionCount; i++)
                pole.SetPosition(i, app.World.globe.transform.TransformPoint(LunarCoordinates.Direction(-80, i * 360.0 / (pole.positionCount - 1)) * (app.config.radius + 20)));
        }
        LineRenderer MakeLine(string label, int count, bool loop, Material mat, int sortingOrder)
        {
            var go = new GameObject(label); go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            if (mat != null) line.sharedMaterial = mat;
            line.sortingOrder = sortingOrder;
            line.useWorldSpace = true;
            line.positionCount = count; line.loop = loop;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return line;
        }
        public void Pulse() { pulseUntil = Time.unscaledTime + 3f; }
        void LateUpdate()
        {
            if (!app) return;
            if (!ring || !outerRing) { Initialize(app); }
            if (!app.SelectedSite || !ring) return;
            Position = app.Director.terrain.Anchor;
            if (!globeSurface) globeSurface=app.World.globe.GetComponent<MeshCollider>();
            var radial=app.SelectedSite.Direction;
            if (globeSurface && globeSurface.Raycast(new Ray(radial*app.config.radius*5,-radial),out var hit,app.config.radius*10) && hit.point.magnitude>=Position.magnitude)
                Position=hit.point+radial;
            pole.enabled=app.Director.Mode==CameraMode.Global && app.World.aidsVisible;
            float distance = Vector3.Distance(app.Director.view.transform.position, Position);
            float size = Mathf.Clamp(distance * .009f, .35f, 28f);
            if (Time.unscaledTime < pulseUntil) size *= 1f + .25f * Mathf.Sin(Time.unscaledTime * 10f);
            var frame = app.Director.Frame;
            var up = app.SelectedSite.Direction;
            for (int i=0;i<ring.positionCount;i++)
            {
                float angle = i * Mathf.PI * 2 / (ring.positionCount-1);
                var pos = Position + up * .5f + frame * new Vector3(Mathf.Cos(angle)*size,0,Mathf.Sin(angle)*size);
                ring.SetPosition(i, pos);
                if (outerRing) outerRing.SetPosition(i, pos);
            }
            // Keep the existing landmark readable from the global camera. A 1.5 m
            // cap becomes subpixel thousands of metres away, even on bright terrain.
            float innerWidth = Mathf.Clamp(distance*.003f,.07f,12f);
            ring.startWidth = ring.endWidth = innerWidth;
            stem.startWidth = stem.endWidth = innerWidth;
            stem.SetPosition(0, Position); stem.SetPosition(1, Position + up * size * 3);

            if (outerRing && outerStem)
            {
                float outerWidth = innerWidth * 1.55f;
                outerRing.startWidth = outerRing.endWidth = outerWidth;
                outerStem.startWidth = outerStem.endWidth = outerWidth;
                outerStem.SetPosition(0, Position);
                outerStem.SetPosition(1, Position + up * size * 3);
            }
        }
        void OnDestroy() { if(material) Destroy(material); if(darkMaterial) Destroy(darkMaterial); if(poleMaterial) Destroy(poleMaterial); }
    }
}
