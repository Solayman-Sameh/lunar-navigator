using UnityEngine;
namespace LunarNavigator
{
    [CreateAssetMenu(menuName="Lunar Navigator/Configuration")]
    public class NavigatorConfig : ScriptableObject
    {
        public float radius=1000, approachDuration=10, transitionDuration=1.8f;
        public float globalMin=1250, globalMax=6000, globalStart=3600;
        public float localMin=8, localMax=260, localStart=150, observerHeight=1.8f;
        public AnimationCurve easing=AnimationCurve.EaseInOut(0,0,1,1);
        public Texture2D moonTexture;
        public string imagerySource="Procedural illustrative crater field; NASA retrieval unavailable";
    }
}
