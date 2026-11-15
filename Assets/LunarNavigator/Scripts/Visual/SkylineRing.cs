using System;
using System.Collections.Generic;
using LunarNavigator.Data;
using UnityEngine;

namespace LunarNavigator.Visual
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class SkylineRing : MonoBehaviour
    {
        public Material ringMaterial;
        public float ringRadius = 1200f; // Inside camera far plane, in front of distant celestial discs
        public float bottomElevationDeg = -30f;

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Mesh ringMesh;
        private string currentSiteId;

        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            if (ringMaterial != null)
            {
                meshRenderer.sharedMaterial = ringMaterial;
            }
        }

        public void BuildForSite(SiteAnalysis siteData, LunarSite site, Quaternion siteFrame)
        {
            if (siteData == null || siteData.terrain == null || siteData.terrain.horizon_el_deg == null)
            {
                gameObject.SetActive(false);
                return;
            }

            currentSiteId = siteData.site?.id ?? "";

            if (ringMesh == null)
            {
                ringMesh = new Mesh { name = "SkylineRingMesh" };
            }
            else
            {
                ringMesh.Clear();
            }

            double[] h360 = siteData.terrain.horizon_el_deg;
            var incomplete = siteData.terrain.incomplete_sectors ?? new SectorRange[0];

            int sliceCount = 360;
            int vertCount = (sliceCount + 1) * 2;
            var vertices = new Vector3[vertCount];
            var normals = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];
            var colors = new Color[vertCount];
            var triangles = new int[sliceCount * 6];

            Vector3 up = siteFrame * Vector3.up;
            Vector3 north = siteFrame * Vector3.forward;
            Vector3 east = siteFrame * Vector3.right;

            float botElRad = bottomElevationDeg * Mathf.Deg2Rad;
            float cosBot = Mathf.Cos(botElRad);
            float sinBot = Mathf.Sin(botElRad);

            for (int k = 0; k <= sliceCount; k++)
            {
                int deg = k % 360;
                float az = deg;
                float topEl = (float)h360[deg];
                float azRad = az * Mathf.Deg2Rad;
                float topElRad = topEl * Mathf.Deg2Rad;

                float cosTop = Mathf.Cos(topElRad);
                float sinTop = Mathf.Sin(topElRad);
                float cosAz = Mathf.Cos(azRad);
                float sinAz = Mathf.Sin(azRad);

                Vector3 horizDir = cosAz * north + sinAz * east;
                Vector3 dirTop = (cosTop * horizDir + sinTop * up).normalized;
                Vector3 dirBot = (cosBot * horizDir + sinBot * up).normalized;

                int iTop = k * 2;
                int iBot = k * 2 + 1;

                vertices[iTop] = dirTop * ringRadius;
                vertices[iBot] = dirBot * ringRadius;

                // Normals facing inward toward the observer
                normals[iTop] = -dirTop;
                normals[iBot] = -dirBot;

                uvs[iTop] = new Vector2((float)k / sliceCount, 1f);
                uvs[iBot] = new Vector2((float)k / sliceCount, 0f);

                // Check if in incomplete sector
                bool isIncomplete = false;
                foreach (var sec in incomplete)
                {
                    if (az >= sec.az_from_deg && az <= sec.az_to_deg)
                    {
                        isIncomplete = true;
                        break;
                    }
                }

                Color c = isIncomplete ? new Color(1f, 0.7f, 0.4f, 0.7f) : Color.white;
                colors[iTop] = c;
                colors[iBot] = c;

                if (k < sliceCount)
                {
                    int triIdx = k * 6;
                    int v0 = iTop;
                    int v1 = iBot;
                    int v2 = iTop + 2;
                    int v3 = iBot + 2;

                    // Inside visible winding (looking outwards from origin)
                    triangles[triIdx + 0] = v0;
                    triangles[triIdx + 1] = v2;
                    triangles[triIdx + 2] = v1;

                    triangles[triIdx + 3] = v1;
                    triangles[triIdx + 4] = v2;
                    triangles[triIdx + 5] = v3;
                }
            }

            ringMesh.vertices = vertices;
            ringMesh.normals = normals;
            ringMesh.uv = uvs;
            ringMesh.colors = colors;
            ringMesh.triangles = triangles;
            ringMesh.RecalculateBounds();

            meshFilter.sharedMesh = ringMesh;
            gameObject.SetActive(true);
        }

        public void UpdateVisibility(CameraMode mode, Vector3 observerPos)
        {
            bool isSurface = mode == CameraMode.Surface;
            if (gameObject.activeSelf != isSurface)
            {
                gameObject.SetActive(isSurface);
            }
            if (isSurface)
            {
                transform.position = observerPos;
            }
        }
    }
}
