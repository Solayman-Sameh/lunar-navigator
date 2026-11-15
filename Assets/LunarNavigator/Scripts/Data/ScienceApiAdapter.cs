using System;
using System.Globalization;
using UnityEngine;

namespace LunarNavigator.Data
{
    public static class ScienceApiAdapter
    {
        /// <summary>
        /// Adapts a JSON response string from FastAPI POST /analyze into Unity's SiteAnalysis contract.
        /// Preserves GEOMETRIC_ONLY or TERRAIN_AWARE_ESTIMATE provenance strictly.
        /// Validates sample counts and binary64 array completeness.
        /// </summary>
        public static SiteAnalysis AdaptApiResponse(
            string jsonResponse,
            string fallbackSiteId,
            string fallbackSiteName,
            out string errorMessage)
        {
            errorMessage = null;

            if (string.IsNullOrEmpty(jsonResponse))
            {
                errorMessage = "Received empty response from scientific API.";
                return null;
            }

            SiteAnalysis siteAnalysis;
            try
            {
                siteAnalysis = JsonUtility.FromJson<SiteAnalysis>(jsonResponse);
            }
            catch (Exception ex)
            {
                errorMessage = "Failed to deserialize JSON response: " + ex.Message;
                return null;
            }

            if (siteAnalysis == null)
            {
                errorMessage = "Parsed SiteAnalysis is null.";
                return null;
            }

            // Ensure site metadata is populated
            if (siteAnalysis.site == null)
            {
                siteAnalysis.site = new SiteInfo();
            }

            if (string.IsNullOrEmpty(siteAnalysis.site.id))
            {
                siteAnalysis.site.id = !string.IsNullOrEmpty(fallbackSiteId) ? fallbackSiteId : "custom_site";
            }

            if (string.IsNullOrEmpty(siteAnalysis.site.name))
            {
                siteAnalysis.site.name = !string.IsNullOrEmpty(fallbackSiteName) ? fallbackSiteName : siteAnalysis.site.id;
            }

            if (string.IsNullOrEmpty(siteAnalysis.site.provenance))
            {
                siteAnalysis.site.provenance = "Live API: JPL Horizons apparent airless center";
            }

            // Validate Ephemeris
            if (siteAnalysis.ephemeris == null)
            {
                errorMessage = "API response missing 'ephemeris' section.";
                return null;
            }

            int n = siteAnalysis.ephemeris.n;
            if (n <= 0)
            {
                errorMessage = "API ephemeris contains 0 samples.";
                return null;
            }

            if (siteAnalysis.ephemeris.sun_az_deg == null || siteAnalysis.ephemeris.sun_az_deg.Length != n ||
                siteAnalysis.ephemeris.sun_el_deg == null || siteAnalysis.ephemeris.sun_el_deg.Length != n ||
                siteAnalysis.ephemeris.earth_az_deg == null || siteAnalysis.ephemeris.earth_az_deg.Length != n ||
                siteAnalysis.ephemeris.earth_el_deg == null || siteAnalysis.ephemeris.earth_el_deg.Length != n)
            {
                errorMessage = $"Ephemeris celestial coordinate array lengths do not match n={n}.";
                return null;
            }

            // Validate or initialize Terrain
            if (siteAnalysis.terrain == null)
            {
                siteAnalysis.terrain = new TerrainInfo();
            }

            if (siteAnalysis.terrain.horizon_el_deg == null || siteAnalysis.terrain.horizon_el_deg.Length != 360)
            {
                // In GEOMETRIC_ONLY mode, default to 360 spherical dip samples if not provided
                siteAnalysis.terrain.horizon_el_deg = new double[360];
                siteAnalysis.terrain.az_step_deg = 1.0;
                siteAnalysis.terrain.source = "spherical_dip";
            }

            if (siteAnalysis.terrain.incomplete_sectors == null)
            {
                siteAnalysis.terrain.incomplete_sectors = new SectorRange[0];
            }

            // Validate Visibility
            if (siteAnalysis.visibility == null)
            {
                siteAnalysis.visibility = new VisibilityInfo();
            }

            if (siteAnalysis.visibility.sun_visible == null || siteAnalysis.visibility.sun_visible.Length != n ||
                siteAnalysis.visibility.earth_visible == null || siteAnalysis.visibility.earth_visible.Length != n)
            {
                // If visibility was not pre-calculated in response, calculate it from ephemeris & horizon
                siteAnalysis.visibility.sun_visible = new int[n];
                siteAnalysis.visibility.earth_visible = new int[n];

                for (int i = 0; i < n; i++)
                {
                    double sAz = siteAnalysis.ephemeris.sun_az_deg[i];
                    double sEl = siteAnalysis.ephemeris.sun_el_deg[i];
                    double sSky = VisibilityMath.SkylineAt(siteAnalysis.terrain.horizon_el_deg, sAz) + siteAnalysis.visibility.sun_margin_deg;
                    siteAnalysis.visibility.sun_visible[i] = (sEl > sSky) ? 1 : 0;

                    double eAz = siteAnalysis.ephemeris.earth_az_deg[i];
                    double eEl = siteAnalysis.ephemeris.earth_el_deg[i];
                    double eSky = VisibilityMath.SkylineAt(siteAnalysis.terrain.horizon_el_deg, eAz);
                    siteAnalysis.visibility.earth_visible[i] = (eEl > eSky) ? 1 : 0;
                }
            }

            // Map live metrics into reference_metrics
            if (siteAnalysis.metrics != null)
            {
                siteAnalysis.reference_metrics = new ReferenceMetric[]
                {
                    new ReferenceMetric
                    {
                        start_utc = siteAnalysis.metrics.start_utc,
                        duration_h = (int)Math.Round(siteAnalysis.metrics.duration_h),
                        f_sun = siteAnalysis.metrics.f_sun,
                        f_earth = siteAnalysis.metrics.f_earth,
                        f_both = siteAnalysis.metrics.f_both,
                        dark_max_h = siteAnalysis.metrics.dark_max_h,
                        comm_gap_max_h = siteAnalysis.metrics.comm_gap_max_h
                    }
                };
            }

            // Enforce Strict Data Status Rules (Never upgrade GEOMETRIC_ONLY)
            if (string.IsNullOrEmpty(siteAnalysis.data_status))
            {
                siteAnalysis.data_status = "GEOMETRIC_ONLY";
            }
            else if (siteAnalysis.data_status != "TERRAIN_AWARE_ESTIMATE" &&
                     siteAnalysis.data_status != "GEOMETRIC_ONLY")
            {
                // Fallback to geometric only for live API responses
                siteAnalysis.data_status = "GEOMETRIC_ONLY";
            }

            return siteAnalysis;
        }
    }
}
