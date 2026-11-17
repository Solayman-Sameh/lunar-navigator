using UnityEngine;
namespace LunarNavigator
{
    [CreateAssetMenu(menuName="Lunar Navigator/Site")]
    public class LunarSite : ScriptableObject
    {
        public string id, displayName;
        [Range(-90,90)] public float latitude;
        [Range(-180,180)] public float longitude;
        public float elevation;
        [SerializeField] private bool _isCustom;
        public bool IsCustom
        {
            get => _isCustom || (!string.IsNullOrEmpty(id) && id.StartsWith("custom"));
            set => _isCustom = value;
        }
        [SerializeField] public double CustomLatitude, CustomLongitude;
        public double AnalysisLatitude => IsCustom ? (CustomLatitude != 0 ? CustomLatitude : latitude) : latitude;
        public double AnalysisLongitude => IsCustom ? (CustomLongitude != 0 ? CustomLongitude : longitude) : longitude;
        [TextArea] public string description;
        [TextArea] public string provenance = "Fictional demo candidate. Coordinates are illustrative, not a verified landing location.";
        public Vector3 Direction => LunarCoordinates.Direction(AnalysisLatitude,AnalysisLongitude);
    }
}
