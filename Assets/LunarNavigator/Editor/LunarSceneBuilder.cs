using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;

namespace LunarNavigator.Editor
{
    public static class LunarSceneBuilder
    {
        const string Root="Assets/LunarNavigator/Generated/";
        static readonly Vector3[] craterCenters=new Vector3[64];
        static readonly float[] craterRadii=new float[64];
        static void Seed()
        {
            var rng=new System.Random(7149);
            for(int i=0;i<64;i++)
            {craterCenters[i]=LunarCoordinates.Direction(rng.NextDouble()*180-90,rng.NextDouble()*360-180);craterRadii[i]=.018f+(float)rng.NextDouble()*.12f;}
        }
        static float Relief(Vector3 n)
        {
            float h=0;
            for(int i=0;i<64;i++)
            {
                float d=(n-craterCenters[i]).magnitude/craterRadii[i];
                if(d<1.5f)h+=craterRadii[i]*(-Mathf.Exp(-d*d*3)+.42f*Mathf.Exp(-Mathf.Pow((d-.92f)*6,2)));
            }
            return h;
        }
        static T Asset<T>(T value,string name) where T:UnityEngine.Object
        {
            string path=Root+name;
            var existing=AssetDatabase.LoadAssetAtPath<T>(path);
            if(existing){EditorUtility.CopySerialized(value,existing);UnityEngine.Object.DestroyImmediate(value);EditorUtility.SetDirty(existing);return existing;}
            AssetDatabase.CreateAsset(value,path);return value;
        }
        static Texture2D Texture(string name,int width,int height,Func<int,int,Color> sample)
        {
            var t=new Texture2D(width,height,TextureFormat.RGBA32,true);t.name=name;
            var pixels=new Color[width*height];for(int y=0;y<height;y++)for(int x=0;x<width;x++)pixels[y*width+x]=sample(x,y);
            t.SetPixels(pixels);t.Apply();
            string path=Root+name+".png";File.WriteAllBytes(path,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);AssetDatabase.ImportAsset(path);
            var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.maxTextureSize=4096;imp.anisoLevel=8;imp.wrapMode=TextureWrapMode.Repeat;imp.textureCompression=TextureImporterCompression.CompressedHQ;imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        static Material Lit(string name,Color color,Texture2D tex=null,float smooth=.1f)
        {
            var m=new Material(Shader.Find("HDRP/Lit")){name=name};m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smooth);m.SetFloat("_Metallic",0);
            if(tex)m.SetTexture("_BaseColorMap",tex);return Asset(m,name+".mat");
        }
        static Material Unlit(string name,Color color)
        {
            var m=new Material(Shader.Find("HDRP/Unlit")){name=name};m.SetColor("_UnlitColor",Color.black);m.SetColor("_EmissiveColor",color);m.SetFloat("_EmissiveExposureWeight",0);return Asset(m,name+".mat");
        }
        static GameObject MeshObject(string name,Mesh mesh,Material material,Transform parent=null)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;return go;
        }
        static Mesh Sphere(int lon,int lat,float radius,bool relief)
        {
            var vertices=new Vector3[(lon+1)*(lat+1)];var uv=new Vector2[vertices.Length];var tris=new int[lon*lat*6];int j=0;
            for(int y=0;y<=lat;y++)for(int x=0;x<=lon;x++)
            {int i=y*(lon+1)+x;var n=LunarCoordinates.Direction((double)y/lat*180-90,(double)x/lon*360-180);vertices[i]=n*(radius+(relief?Relief(n)*35:0));uv[i]=new Vector2((float)x/lon,(float)y/lat);}
            for(int y=0;y<lat;y++)for(int x=0;x<lon;x++)
            {int i=y*(lon+1)+x;tris[j++]=i;tris[j++]=i+lon+1;tris[j++]=i+1;tris[j++]=i+1;tris[j++]=i+lon+1;tris[j++]=i+lon+2;}
            var m=new Mesh{name="Lunar sphere",indexFormat=IndexFormat.UInt32};m.vertices=vertices;m.uv=uv;m.triangles=tris;m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();return m;
        }
        static Mesh Ground(float radius)
        {
            const int n=192;var v=new Vector3[(n+1)*(n+1)];var uv=new Vector2[v.Length];var tri=new int[n*n*6];int j=0;
            for(int z=0;z<=n;z++)for(int x=0;x<=n;x++)
            {int i=z*(n+1)+x;float xx=((float)x/n*2-1)*LunarTerrain.HalfSize,zz=((float)z/n*2-1)*LunarTerrain.HalfSize;v[i]=new Vector3(xx,LunarTerrain.Height(xx,zz,radius),zz);uv[i]=new Vector2(xx*.025f,zz*.025f);}
            for(int z=0;z<n;z++)for(int x=0;x<n;x++){int i=z*(n+1)+x;tri[j++]=i;tri[j++]=i+n+1;tri[j++]=i+1;tri[j++]=i+1;tri[j++]=i+n+1;tri[j++]=i+n+2;}
            var m=new Mesh{name="Illustrative crater terrain / horizon8",indexFormat=IndexFormat.UInt32};m.vertices=v;m.uv=uv;m.triangles=tri;m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();return m;
        }
        [MenuItem("Lunar Navigator/Build Main Scene")]
        public static void Build()
        {
            Directory.CreateDirectory(Root);Seed();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var config=ScriptableObject.CreateInstance<NavigatorConfig>();config=Asset(config,"NavigatorConfig.asset");
            const int w=2048,h=1024;
            var color=Texture("Moon_Procedural_2K",w,h,(x,y)=>
            {
                var n=LunarCoordinates.Direction((double)y/(h-1)*180-90,(double)x/(w-1)*360-180);
                float a=Mathf.PerlinNoise(n.x*3+13,n.z*3+n.y*2+21),b=Mathf.PerlinNoise(n.x*19+n.y*9+47,n.z*19+n.y*4+12);
                float fine=Mathf.PerlinNoise(x*.11f,y*.11f)*.08f;
                float mare=Mathf.SmoothStep(.45f,.72f,a)*.15f*(n.x>.0f?1:.25f);
                float g=Mathf.Clamp(.45f-mare+b*.11f+fine+Relief(n)*.8f,.12f,.7f);
                return new Color(g*1.015f,g,g*.98f);
            });
            var relief=Texture("Moon_Relief",w,h,(x,y)=>{var n=LunarCoordinates.Direction((double)y/h*180-90,(double)x/w*360-180);float v=.5f+Relief(n)*2+Mathf.PerlinNoise(x*.05f,y*.05f)*.035f;return new Color(v,v,v);});
            var ri=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(relief));ri.textureType=TextureImporterType.NormalMap;ri.convertToNormalmap=true;ri.heightmapScale=.04f;ri.SaveAndReimport();
            config.moonTexture=color;EditorUtility.SetDirty(config);
            var moonMat=Lit("Moon Regolith",Color.white,color,.06f);moonMat.SetTexture("_NormalMap",relief);moonMat.SetFloat("_NormalScale",1.3f);moonMat.EnableKeyword("_NORMALMAP");moonMat.EnableKeyword("_NORMALMAP_TANGENT_SPACE");
            var globe=MeshObject("Moon · geographically oriented",Asset(Sphere(384,192,config.radius,true),"Moon.asset"),moonMat);
            var groundTexture=Texture("Regolith_Detail",512,512,(x,y)=>{float a=Mathf.PerlinNoise(x*.08f,y*.08f),b=Mathf.PerlinNoise(x*.45f+5,y*.45f);float v=.32f+a*.19f+b*.08f;return new Color(v,v*.99f,v*.97f);});
            var groundMat=Lit("Rough lunar soil",Color.white,groundTexture,.035f);
            var normal=Texture("Regolith_Normal",512,512,(x,y)=>{float n=Mathf.PerlinNoise(x*.19f,y*.19f);return new Color(n,n,n);});
            var ni=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(normal));ni.textureType=TextureImporterType.NormalMap;ni.convertToNormalmap=true;ni.heightmapScale=.04f;ni.SaveAndReimport();groundMat.SetTexture("_NormalMap",normal);groundMat.EnableKeyword("_NORMALMAP");groundMat.EnableKeyword("_NORMALMAP_TANGENT_SPACE");
            var terrainGo=MeshObject("Illustrative Terrain · tangent patch",Asset(Ground(config.radius),"Terrain.asset"),groundMat);
            var terrain=terrainGo.AddComponent<LunarTerrain>();terrain.radius=config.radius;terrainGo.AddComponent<MeshCollider>().sharedMesh=terrainGo.GetComponent<MeshFilter>().sharedMesh;
            // Combine rocks once: no per-rock renderers or runtime mesh generation.
            var rockSource=Sphere(9,6,1,false);var sourceVertices=rockSource.vertices;for(int i=0;i<sourceVertices.Length;i++)sourceVertices[i]*=.7f+Mathf.PerlinNoise(i*.9f,6)*.6f;rockSource.vertices=sourceVertices;rockSource.RecalculateNormals();
            var rng=new System.Random(937);var combine=new CombineInstance[210];
            for(int i=0;i<combine.Length;i++)
            {float x=(float)(rng.NextDouble()*560-280),z=(float)(rng.NextDouble()*560-280),s=.3f+(float)Math.Pow(rng.NextDouble(),3)*4;var pos=new Vector3(x,LunarTerrain.Height(x,z,config.radius)+s*.35f,z);combine[i]=new CombineInstance{mesh=rockSource,transform=Matrix4x4.TRS(pos,Quaternion.Euler((float)rng.NextDouble()*80,(float)rng.NextDouble()*360,0),new Vector3(s,s*.65f,s*.8f))};}
            var rockMesh=new Mesh{name="Combined scattered lunar rocks / horizon8",indexFormat=IndexFormat.UInt32};rockMesh.CombineMeshes(combine);rockMesh=Asset(rockMesh,"Rocks.asset");
            var rocks=MeshObject("Regolith boulders · combined",rockMesh,Lit("Basalt rocks",new Color(.36f,.35f,.33f),groundTexture,.04f),terrainGo.transform);
            var rockPrefab=new GameObject("Rock reference");rockPrefab.AddComponent<MeshFilter>().sharedMesh=Asset(rockSource,"RockSource.asset");rockPrefab.AddComponent<MeshRenderer>().sharedMaterial=rocks.GetComponent<MeshRenderer>().sharedMaterial;
            PrefabUtility.SaveAsPrefabAsset(rockPrefab,Root+"Rock.prefab");UnityEngine.Object.DestroyImmediate(rockPrefab);
            var cameraGo=new GameObject("Exploration Camera");cameraGo.tag="MainCamera";
            var camera=cameraGo.AddComponent<Camera>();camera.fieldOfView=42;camera.nearClipPlane=1;camera.farClipPlane=30000;camera.allowHDR=true;
            camera.transform.position=LunarCoordinates.Direction(-32,12)*3100;camera.transform.LookAt(Vector3.zero,Vector3.up);
            var hd=cameraGo.AddComponent<HDAdditionalCameraData>();hd.clearColorMode=HDAdditionalCameraData.ClearColorMode.Color;hd.backgroundColorHDR=Color.black;hd.antialiasing=HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;
            var lightGo=new GameObject("Sun · illustrative directional illumination");var light=lightGo.AddComponent<Light>();light.type=LightType.Directional;light.shadows=LightShadows.Soft;
            var hdl=lightGo.AddComponent<HDAdditionalLightData>();light.lightUnit=UnityEngine.Rendering.LightUnit.Lux;light.intensity=110000;lightGo.transform.rotation=Quaternion.LookRotation(-new IllustrativeDemoProvider().SunDirection(DemoSimulation.Epoch));
            var volumeGo=new GameObject("Vacuum exposure and filmic response");var volume=volumeGo.AddComponent<Volume>();volume.isGlobal=true;
            var profile=Asset(ScriptableObject.CreateInstance<VolumeProfile>(),"Vacuum Volume.asset");var exposure=profile.Add<Exposure>(true);exposure.mode.Override(ExposureMode.Fixed);exposure.fixedExposure.Override(12.7f);
            var tone=profile.Add<Tonemapping>(true);tone.mode.Override(TonemappingMode.ACES);
            var bloom=profile.Add<Bloom>(true);bloom.intensity.Override(.025f);bloom.threshold.Override(1.5f);
            var shadow=profile.Add<HDShadowSettings>(true);shadow.maxShadowDistance.Override(2500);
            volume.sharedProfile=profile;
            foreach(var component in volume.sharedProfile.components)if(!AssetDatabase.Contains(component))AssetDatabase.AddObjectToAsset(component,volume.sharedProfile);
            EditorUtility.SetDirty(profile);
            var stars=CreateStars();
            var earthTexture=Texture("Earth_Illustrative",1024,512,(x,y)=>
            {float lat=(float)y/512;float noise=Mathf.PerlinNoise(x*.011f+4,y*.012f+18);float cloud=Mathf.PerlinNoise(x*.037f,y*.026f);var c=noise>.54f?new Color(.1f,.18f,.09f):new Color(.015f,.07f,.18f);if(lat<.08f||lat>.92f)c=new Color(.65f,.7f,.72f);return Color.Lerp(c,new Color(.8f,.84f,.87f),Mathf.SmoothStep(.58f,.78f,cloud)*.8f);});
            var earth=MeshObject("Earth · illustrative visual scale",Asset(Sphere(96,48,1,false),"EarthSphere.asset"),Lit("Earth · illustrative",Color.white,earthTexture,.35f));earth.transform.localScale=Vector3.one*130;
            var sun=MeshObject("Solar disc · illustrative visual scale",Asset(Sphere(32,16,1,false),"SunSphere.asset"),Unlit("Solar disc",new Color(12,11,9)));sun.transform.localScale=Vector3.one*55;
            var sites=new LunarSite[3];string[] labels={"Aster Ridge","Selene Basin","Umbra Rise"};float[] lats={-84.2f,-86.3f,-82.8f},lons={-30,35,110};
            for(int i=0;i<3;i++){var s=ScriptableObject.CreateInstance<LunarSite>();s.id="demo-"+(i+1);s.displayName=labels[i];s.latitude=lats[i];s.longitude=lons[i];s.description="Fictional south-polar demonstration candidate. Explore representative cratered terrain and illustrative lighting. This is not a surveyed landing site.";sites[i]=Asset(s,"Site_"+(i+1)+".asset");}
            terrainGo.transform.SetPositionAndRotation(sites[0].Direction*config.radius,LunarCoordinates.Frame(sites[0].Direction));
            terrainGo.SetActive(false);
            var services=new GameObject("Lunar Navigator · Application");
            var app=services.AddComponent<LunarApplication>();var clock=services.AddComponent<DemoSimulation>();var director=services.AddComponent<CameraDirector>();var input=services.AddComponent<NavigatorInput>();var world=services.AddComponent<LunarWorld>();
            var uiGo=new GameObject("Floating viewport interface");var document=uiGo.AddComponent<UIDocument>();var ui=uiGo.AddComponent<NavigatorUI>();
            var panel=ScriptableObject.CreateInstance<PanelSettings>();panel.scaleMode=PanelScaleMode.ScaleWithScreenSize;panel.referenceResolution=new Vector2Int(1600,900);panel.screenMatchMode=PanelScreenMatchMode.MatchWidthOrHeight;panel.match=.5f;document.panelSettings=Asset(panel,"PanelSettings.asset");
            ui.styleSheet=AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/LunarNavigator/UI/Navigator.uss");
            app.config=config;app.sites=sites;app.Clock=clock;app.Director=director;app.UI=ui;app.World=world;
            director.view=camera;director.config=config;director.terrain=terrain;director.selected=sites[0];input.app=app;
            world.sunlight=light;world.earth=earth.transform;world.sun=sun.transform;world.stars=stars;world.terrain=terrain;world.globe=globe;
            RenderSettings.fog=false;RenderSettings.skybox=null;
            EditorSceneManager.SaveScene(scene,"Assets/Scenes/LunarNavigator_Main.unity");
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/LunarNavigator_Main.unity",true)};
            AssetDatabase.SaveAssets();Debug.Log("LUNAR_BUILD_COMPLETE: Main scene saved; HDRP retained, no packages changed.");
        }
        static GameObject CreateStars()
        {
            var rng=new System.Random(71);const int n=500;var vertices=new Vector3[n*3];var colors=new Color[n*3];var triangles=new int[n*3];
            for(int i=0;i<n;i++)
            {
                var normal=LunarCoordinates.Direction(rng.NextDouble()*180-90,rng.NextDouble()*360-180);var c=normal*18000;var right=Vector3.Cross(normal,Vector3.up).normalized;var up=Vector3.Cross(right,normal);float size=1.3f+(float)rng.NextDouble()*2.2f;
                vertices[i*3]=c-right*size-up*size;vertices[i*3+1]=c+up*size;vertices[i*3+2]=c+right*size-up*size;
                for(int j=0;j<3;j++){triangles[i*3+j]=i*3+j;colors[i*3+j]=Color.white;}
            }
            var m=new Mesh{name="Sparse star triangles"};m.vertices=vertices;m.colors=colors;m.triangles=triangles;m.RecalculateBounds();
            var mat=Unlit("Quiet starlight",new Color(.5f,.6f,.7f));mat.SetFloat("_CullMode",0);
            return MeshObject("Sparse stellar background",Asset(m,"Stars.asset"),mat);
        }
    }
}
