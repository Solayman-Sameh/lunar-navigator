using System;
using UnityEngine;

namespace LunarNavigator.Data
{
    [Serializable]
    public class AnalysisIndex
    {
        public int schema_version = 1;
        public IndexSite[] sites;
        public string opportunities_file;
    }

    [Serializable]
    public class IndexSite
    {
        public string id;
        public string name;
        public string file;
        public string data_status;
    }

    [Serializable]
    public class SiteAnalysis
    {
        public int schema_version = 1;
        public string data_status;
        public string algorithm_version;
        public string generated_utc;
        public SiteInfo site;
        public Ephemeris ephemeris;
        public TerrainInfo terrain;
        public VisibilityInfo visibility;
        public LiveMetrics metrics;
        public ReferenceMetric[] reference_metrics;

        public bool HasIncompleteSectors => terrain?.incomplete_sectors != null && terrain.incomplete_sectors.Length > 0;
    }

    [Serializable]
    public class SiteInfo
    {
        public string id;
        public string name;
        public double lat_deg;
        public double lon_east_deg;
        public double observer_height_m = 2.0;
        public string provenance;
    }

    [Serializable]
    public class Ephemeris
    {
        public string source;
        public string t0_utc;
        public int step_s = 600;
        public int n;
        public double[] sun_az_deg;
        public double[] sun_el_deg;
        public double[] earth_az_deg;
        public double[] earth_el_deg;
    }

    [Serializable]
    public class TerrainInfo
    {
        public string source;
        public int resolution_m = 100;
        public int max_range_km = 150;
        public double az_step_deg = 1.0;
        public double[] horizon_el_deg;
        public SectorRange[] incomplete_sectors;
    }

    [Serializable]
    public class SectorRange
    {
        public double az_from_deg;
        public double az_to_deg;
    }

    [Serializable]
    public class VisibilityInfo
    {
        public double sun_margin_deg = 0.0;
        public int[] sun_visible;
        public int[] earth_visible;
    }

    [Serializable]
    public class LiveMetrics
    {
        public double f_sun;
        public double f_earth;
        public double f_both;
        public string start_utc;
        public string end_utc;
        public double duration_h;
        public int cells;
        public int step_s;
        public double dark_max_h;
        public double comm_gap_max_h;
    }

    [Serializable]
    public class ReferenceMetric
    {
        public string start_utc;
        public int duration_h;
        public double f_sun;
        public double f_earth;
        public double f_both;
        public double dark_max_h;
        public double comm_gap_max_h;
    }

    [Serializable]
    public class OpportunitiesFile
    {
        public int schema_version = 1;
        public string algorithm_version = "0.1.0";
        public string data_status;
        public OpportunityRequest request;
        public string[] candidate_sites;
        public string scoring_note;
        public OpportunityResult[] best_overall;
        public OpportunityResult[] earliest_qualifying;
        public ClosestMiss[] closest_misses;
        public int evaluated_count;
        public int rejected_count;
    }

    [Serializable]
    public class OpportunityRequest
    {
        public string earliest_landing_utc;
        public int search_days = 14;
        public int landing_step_h = 6;
        public int duration_h = 72;
        public Constraints constraints;
        public string constraints_note;
    }

    [Serializable]
    public class Constraints
    {
        public double min_sun_fraction = 0.30;
        public double min_earth_fraction = 0.40;
        public double max_dark_h = 36.0;
        public double max_comm_gap_h = 48.0;
    }

    [Serializable]
    public class OpportunityResult
    {
        public int rank;
        public string site_id;
        public string landing_utc;
        public int duration_h;
        public double score;
        public double f_sun;
        public double f_earth;
        public double f_both;
        public double dark_max_h;
        public double comm_gap_max_h;
    }

    [Serializable]
    public class ClosestMiss
    {
        public string site_id;
        public string landing_utc;
        public string[] failed;
        public double f_sun;
        public double f_earth;
        public double f_both;
        public double dark_max_h;
        public double comm_gap_max_h;
    }

    [Serializable]
    public class SitesResponse
    {
        public int schema_version;
        public string api_profile;
        public string data_status;
        public CatalogSiteRecord[] sites;
        public string limitations;
    }

    [Serializable]
    public class CatalogSiteRecord
    {
        public string id;
        public string name;
        public double lat_deg;
        public double lon_east_deg;
        public bool verified;
        public string point_source;
        public string coordinate_frame;
        public string coordinate_convention;
        public string reference_description;
        public string verification_scope;
    }

    [Serializable]
    public class LiveOpportunityRequest
    {
        public string earliest_landing_utc;
        public double search_window_h = 48.0;
        public string search_window_mode = "CONTAINED_MISSION";
        public double mission_duration_h = 24.0;
        public string[] candidate_site_ids;
        public double min_sun_fraction = 0.30;
        public double min_earth_fraction = 0.40;
        public double max_dark_h = 36.0;
        public double max_comm_gap_h = 48.0;
        public double search_step_h = 6.0;
        public string ranking_policy = "BEST_OVERALL";
        public int sampling_step_minutes = 10;
        public double observer_height_m = 2.0;

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }
    }

    [Serializable]
    public class LiveOpportunitiesResponse
    {
        public int schema_version;
        public string api_profile;
        public string data_status;
        public string run_id;
        public string[] candidate_sites;
        public int arrival_count_per_site;
        public int source_pair_count;
        public string score_formula;
        public string constraints_note;
        public string[] limitations;
        public LiveOpportunityRecommendation[] recommendations;
        public LiveClosestMiss[] closest_misses;
        public int evaluated_count;
        public int rejected_count;
        public int qualifying_count;
        public string empty_reason;
    }

    [Serializable]
    public class LiveOpportunityRecommendation
    {
        public int rank;
        public string site_id;
        public string site_name;
        public string landing_utc;
        public string mission_end_utc;
        public double mission_duration_h;
        public LiveMetrics metrics;
        public double score;
        public string explanation;
    }

    [Serializable]
    public class LiveClosestMiss
    {
        public string site_id;
        public string site_name;
        public string landing_utc;
        public string mission_end_utc;
        public double mission_duration_h;
        public LiveMetrics metrics;
        public string[] failed_constraints;
        public double normalized_violation;
    }
}
