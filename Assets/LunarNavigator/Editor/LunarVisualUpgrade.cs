using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace LunarNavigator.Editor
{
    public static class LunarVisualUpgrade
    {
        const string Root = "Assets/LunarNavigator/Generated/";

        [MenuItem("Lunar Navigator/Upgrade Visual Production Pass")]
        public static string ApplyUpgrades()
        {
            var app = UnityEngine.Object.FindFirstObjectByType<LunarApplication>();
            if (!app) return "Error: LunarApplication not found in open scene.";

            float radius = app.config.radius;

            // 1. Rebuild and Smooth Local Terrain Mesh
            string terrainPath = Root + "Terrain.asset";
            var terrainMesh = AssetDatabase.LoadAssetAtPath<Mesh>(terrainPath);
            if (!terrainMesh)
            {
                terrainMesh = new Mesh { name = "Terrain / horizon8_production", indexFormat = IndexFormat.UInt32 };
                AssetDatabase.CreateAsset(terrainMesh, terrainPath);
            }

            const int n = 192;
            int vertCount = (n + 1) * (n + 1);
            var vertices = new Vector3[vertCount];
            var normals = new Vector3[vertCount];
            var tangents = new Vector4[vertCount];
            var uvs = new Vector2[vertCount];
            var triangles = new int[n * n * 6];

            float step = (LunarTerrain.HalfSize * 2f) / n;
            float eps = 0.35f;

            for (int z = 0; z <= n; z++)
            {
                float zz = -LunarTerrain.HalfSize + z * step;
                for (int x = 0; x <= n; x++)
                {
                    float xx = -LunarTerrain.HalfSize + x * step;
                    int i = z * (n + 1) + x;

                    float y = LunarTerrain.Height(xx, zz, radius);
                    vertices[i] = new Vector3(xx, y, zz);
                    uvs[i] = new Vector2(xx * 0.05f, zz * 0.05f);

                    // Analytic gradient normal to eliminate faceting completely
                    float ydx1 = LunarTerrain.Height(xx + eps, zz, radius);
                    float ydx0 = LunarTerrain.Height(xx - eps, zz, radius);
                    float ydz1 = LunarTerrain.Height(xx, zz + eps, radius);
                    float ydz0 = LunarTerrain.Height(xx, zz - eps, radius);

                    float dhdx = (ydx1 - ydx0) / (2f * eps);
                    float dhdz = (ydz1 - ydz0) / (2f * eps);

                    Vector3 norm = new Vector3(-dhdx, 1f, -dhdz).normalized;
                    normals[i] = norm;

                    Vector3 tang = new Vector3(1f, dhdx, 0f).normalized;
                    tangents[i] = new Vector4(tang.x, tang.y, tang.z, 1f);
                }
            }

            int triIdx = 0;
            for (int z = 0; z < n; z++)
            {
                for (int x = 0; x < n; x++)
                {
                    int i = z * (n + 1) + x;
                    triangles[triIdx++] = i;
                    triangles[triIdx++] = i + n + 1;
                    triangles[triIdx++] = i + 1;

                    triangles[triIdx++] = i + 1;
                    triangles[triIdx++] = i + n + 1;
                    triangles[triIdx++] = i + n + 2;
                }
            }

            terrainMesh.Clear();
            terrainMesh.vertices = vertices;
            terrainMesh.normals = normals;
            terrainMesh.tangents = tangents;
            terrainMesh.uv = uvs;
            terrainMesh.triangles = triangles;
            terrainMesh.RecalculateBounds();
            EditorUtility.SetDirty(terrainMesh);

            // Update MeshCollider on terrain
            var col = app.World.terrain.GetComponent<MeshCollider>();
            if (col)
            {
                col.sharedMesh = null;
                col.sharedMesh = terrainMesh;
            }

            // 2. Rebuild Regolith Boulders (Pebbles & Rocks seated cleanly on terrain)
            string rockPath = Root + "Rocks.asset";
            var rockMesh = AssetDatabase.LoadAssetAtPath<Mesh>(rockPath);
            var rockSource = AssetDatabase.LoadAssetAtPath<Mesh>(Root + "RockSource.asset");
            if (rockMesh && rockSource)
            {
                var rng = new System.Random(4021);
                const int rockCount = 280;
                var combines = new CombineInstance[rockCount];

                for (int i = 0; i < rockCount; i++)
                {
                    float angle = (float)(rng.NextDouble() * Math.PI * 2.0);
                    // Clustered distribution: 40% near-field (<45m), 60% mid-field (45m - 240m)
                    float dist;
                    float s;
                    if (i < 110)
                    {
                        dist = 3.5f + (float)rng.NextDouble() * 45f;
                        s = 0.25f + (float)Math.Pow(rng.NextDouble(), 2.5) * 1.8f;
                    }
                    else
                    {
                        dist = 45f + (float)rng.NextDouble() * 200f;
                        s = 0.5f + (float)Math.Pow(rng.NextDouble(), 3.0) * 4.5f;
                    }

                    float rx = Mathf.Sin(angle) * dist;
                    float rz = Mathf.Cos(angle) * dist;
                    float ry = LunarTerrain.Height(rx, rz, radius) + s * 0.22f;

                    var rot = Quaternion.Euler(
                        (float)(rng.NextDouble() * 70.0),
                        (float)(rng.NextDouble() * 360.0),
                        (float)(rng.NextDouble() * 30.0)
                    );
                    var scale = new Vector3(s, s * (0.6f + (float)rng.NextDouble() * 0.25f), s * (0.75f + (float)rng.NextDouble() * 0.3f));

                    combines[i] = new CombineInstance
                    {
                        mesh = rockSource,
                        transform = Matrix4x4.TRS(new Vector3(rx, ry, rz), rot, scale)
                    };
                }

                rockMesh.Clear();
                rockMesh.CombineMeshes(combines, true, true);
                rockMesh.RecalculateBounds();
                EditorUtility.SetDirty(rockMesh);
            }

            // 3. Upgrade Terrain Material (Rough lunar soil.mat)
            var terrainMat = AssetDatabase.LoadAssetAtPath<Material>(Root + "Rough lunar soil.mat");
            if (terrainMat)
            {
                terrainMat.SetColor("_BaseColor", new Color(0.72f, 0.72f, 0.70f));
                terrainMat.SetFloat("_Metallic", 0f);
                terrainMat.SetFloat("_Smoothness", 0.08f);
                terrainMat.SetFloat("_NormalScale", 1.15f);
                terrainMat.SetTextureScale("_BaseColorMap", new Vector2(4f, 4f));
                terrainMat.EnableKeyword("_NORMALMAP");
                terrainMat.EnableKeyword("_NORMALMAP_TANGENT_SPACE");
                EditorUtility.SetDirty(terrainMat);
            }

            // 4. Upgrade Globe Material (Moon Regolith.mat)
            var globeMat = AssetDatabase.LoadAssetAtPath<Material>(Root + "Moon Regolith.mat");
            if (globeMat)
            {
                globeMat.SetColor("_BaseColor", new Color(0.82f, 0.82f, 0.82f));
                globeMat.SetFloat("_Metallic", 0f);
                globeMat.SetFloat("_Smoothness", 0.08f);
                globeMat.SetFloat("_NormalScale", 1.25f);
                globeMat.EnableKeyword("_NORMALMAP");
                globeMat.EnableKeyword("_NORMALMAP_TANGENT_SPACE");

                // Assign Detail Map for micro-relief during approach
                var regolithNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "Regolith_Normal.png");
                if (regolithNormal)
                {
                    globeMat.SetTexture("_DetailMap", regolithNormal);
                    globeMat.SetTextureScale("_DetailMap", new Vector2(60f, 30f));
                    globeMat.SetFloat("_DetailNormalScale", 0.7f);
                    globeMat.EnableKeyword("_DETAIL_MAP");
                }
                EditorUtility.SetDirty(globeMat);
            }

            // 5. Update HDRP Volume Exposure & Shadow Settings
            var vol = UnityEngine.Object.FindFirstObjectByType<Volume>();
            if (vol && vol.sharedProfile)
            {
                if (vol.sharedProfile.TryGet<Exposure>(out var exp))
                {
                    exp.fixedExposure.Override(12.2f);
                }
                if (vol.sharedProfile.TryGet<HDShadowSettings>(out var shadows))
                {
                    shadows.maxShadowDistance.Override(3500f);
                }
                EditorUtility.SetDirty(vol.sharedProfile);
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(app.gameObject.scene);
            EditorSceneManager.SaveOpenScenes();

            return $"Visual Production Upgrades applied: Terrain mesh ({terrainMesh.vertexCount} verts), Boulders (280 seated), Shaders & Materials refined, HDRP settings updated.";
        }
    }
}
