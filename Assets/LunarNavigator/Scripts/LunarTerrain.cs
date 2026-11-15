using UnityEngine;

namespace LunarNavigator
{
    public class LunarTerrain : MonoBehaviour, ILocalTerrainProvider
    {
        public const float HalfSize = 330f;
        public float radius = 1000f;

        public float SampleHeight(float x, float z) => Height(x, z, radius);

        public static float Height(float x, float z, float radius)
        {
            float r = Mathf.Sqrt(x * x + z * z);
            // True spherical curvature drop in tangent space
            float sphereDrop = radius > 0 ? (Mathf.Sqrt(Mathf.Max(0f, radius * radius - r * r)) - radius) : 0f;

            // Local crater and regolith topography
            float h = 3.2f + Mathf.PerlinNoise(x * 0.008f + 53f, z * 0.008f + 16f) * 7.5f 
                           + Mathf.PerlinNoise(x * 0.045f + 72f, z * 0.045f + 8f) * 1.4f;

            h += Crater(x, z, 64f, 72f, 55f, 16f);
            h += Crater(x, z, -88f, 20f, 74f, 22f);
            h += Crater(x, z, 90f, -100f, 43f, 12f);
            h += Crater(x, z, -150f, -140f, 62f, 18f);
            h += Crater(x, z, 10f, 210f, 90f, 24f);
            h += Crater(x, z, 230f, 5f, 40f, 10f);
            // Micro-craters for rich close-range fidelity
            h += Crater(x, z, -25f, -18f, 18f, 4.5f);
            h += Crater(x, z, 35f, -40f, 22f, 5.0f);
            h += Crater(x, z, 15f, 30f, 14f, 3.2f);
            h += Crater(x, z, -45f, 50f, 25f, 6.0f);

            // Smooth perimeter blend to merge seamlessly into the global Moon sphere
            float blend = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(180f, 320f, r));
            return sphereDrop + h * blend + 0.15f;
        }

        static float Crater(float x, float z, float cx, float cz, float r, float depth)
        {
            float d = Vector2.Distance(new Vector2(x, z), new Vector2(cx, cz)) / r;
            if (d > 1.6f) return 0f;
            return -depth * Mathf.Exp(-d * d * 3f) + depth * 0.38f * Mathf.Exp(-Mathf.Pow((d - 0.92f) * 5.5f, 2f));
        }

        public Vector3 Anchor => transform.TransformPoint(0, SampleHeight(0, 0), 0);

        public float Clearance(Vector3 world)
        {
            var p = transform.InverseTransformPoint(world);
            return p.y - SampleHeight(p.x, p.z);
        }
    }
}
