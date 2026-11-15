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
        [TextArea] public string description;
        [TextArea] public string provenance = "Fictional demo candidate. Coordinates are illustrative, not a verified landing location.";
        public Vector3 Direction => LunarCoordinates.Direction(latitude,longitude);
    }
}
