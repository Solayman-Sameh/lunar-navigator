using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace LunarNavigator.Editor
{
    public static class LunarSurfaceDetail
    {
        [MenuItem("Lunar Navigator/Add Near Surface Geology")]
        public static string Apply()
        {
            var app=Object.FindAnyObjectByType<LunarApplication>();var terrain=app.World.terrain;
            if(terrain.transform.Find("Near-field regolith fragments"))return "Near-field geology already present";
            const string root="Assets/LunarNavigator/Generated/";
            var source=AssetDatabase.LoadAssetAtPath<Mesh>(root+"RockSource.asset");
            var random=new System.Random(2811);var parts=new CombineInstance[260];
            for(int i=0;i<parts.Length;i++)
            {
                float angle=(float)random.NextDouble()*Mathf.PI*2;
                float distance=i<8?8+i*1.5f:3.5f+Mathf.Sqrt((float)random.NextDouble())*47;
                float size=i<8?.55f+(float)random.NextDouble()*.7f:.04f+Mathf.Pow((float)random.NextDouble(),3)*.35f;
                float x=Mathf.Sin(angle)*distance,z=Mathf.Cos(angle)*distance;
                var pos=new Vector3(x,terrain.SampleHeight(x,z)+size*.28f,z);
                parts[i]=new CombineInstance{mesh=source,transform=Matrix4x4.TRS(pos,Quaternion.Euler((float)random.NextDouble()*80,(float)random.NextDouble()*360,0),new Vector3(size,size*.65f,size*.85f))};
            }
            var mesh=new Mesh{name="Near-field regolith / combined",indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(parts);
            AssetDatabase.CreateAsset(mesh,root+"NearFieldRocks.asset");
            var field=new GameObject("Near-field regolith fragments");field.transform.SetParent(terrain.transform,false);
            field.AddComponent<MeshFilter>().sharedMesh=mesh;field.AddComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(root+"Basalt rocks.mat");
            PrefabUtility.SaveAsPrefabAsset(field,root+"NearFieldRocks.prefab");
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(app.gameObject.scene);EditorSceneManager.SaveOpenScenes();
            return "Added 260 near-field fragments in one reusable mesh and one renderer";
        }
    }
}
