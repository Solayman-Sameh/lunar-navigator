using System;
using UnityEngine;

namespace LunarNavigator
{
    public static class LunarCoordinates
    {
        public static Vector3 Direction(double latitude, double longitude)
        {
            double a = latitude * Math.PI / 180, b = longitude * Math.PI / 180;
            return new Vector3((float)(Math.Cos(a)*Math.Cos(b)), (float)Math.Sin(a), (float)(Math.Cos(a)*Math.Sin(b)));
        }
        public static Vector2 Geographic(Vector3 direction)
        {
            var n = direction.normalized;
            return new Vector2(Mathf.Asin(Mathf.Clamp(n.y,-1,1))*Mathf.Rad2Deg, Mathf.Atan2(n.z,n.x)*Mathf.Rad2Deg);
        }
        public static Quaternion Frame(Vector3 up)
        {
            up = up.normalized;
            var north = Vector3.ProjectOnPlane(Vector3.up, up).normalized;
            if (north.sqrMagnitude < .1f) north = up.y > 0 ? Vector3.left : Vector3.right;
            return Quaternion.LookRotation(north, up);
        }
    }
    public interface ILunarEphemeris
    {
        string Label { get; }
        Vector3 SunDirection(DateTime utc);
        Vector3 EarthDirection(DateTime utc);
    }
    public interface ISiteCatalog { LunarSite[] Sites { get; } }
    public interface ILocalTerrainProvider { float SampleHeight(float x, float z); }
}
