using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
namespace LunarNavigator.Editor
{
    public static class LunarPolish
    {
        [MenuItem("Lunar Navigator/Apply Visual Refinements")]
        public static string Apply()
        {
            const string root="Assets/LunarNavigator/Generated/";
            AssetDatabase.ImportAsset(root+"NASA_LROC_4K.tif",ImportAssetOptions.ForceSynchronousImport);
            var importer=AssetImporter.GetAtPath(root+"NASA_LROC_4K.tif") as TextureImporter;
            if(importer){importer.maxTextureSize=4096;importer.anisoLevel=8;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.sRGBTexture=true;importer.SaveAndReimport();}
            var app=Object.FindAnyObjectByType<LunarApplication>();
            var heightPath=root+"NASA_LOLA_16.tif";
            if(System.IO.File.Exists(heightPath))
            {
                AssetDatabase.ImportAsset(heightPath,ImportAssetOptions.ForceSynchronousImport);
                var hi=(TextureImporter)AssetImporter.GetAtPath(heightPath);hi.sRGBTexture=false;hi.isReadable=true;hi.textureType=TextureImporterType.Default;hi.maxTextureSize=4096;hi.textureCompression=TextureImporterCompression.Uncompressed;hi.SaveAndReimport();
                var heights=AssetDatabase.LoadAssetAtPath<Texture2D>(heightPath);
                var mesh=app.World.globe.GetComponent<MeshFilter>().sharedMesh;var verts=mesh.vertices;var uv=mesh.uv;
                for(int i=0;i<verts.Length;i++)verts[i]=verts[i].normalized*(app.config.radius+(heights.GetPixelBilinear(uv[i].x,uv[i].y).r-.5f)*14);
                mesh.vertices=verts;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
                hi.textureType=TextureImporterType.NormalMap;hi.convertToNormalmap=true;hi.heightmapScale=.009f;hi.isReadable=false;hi.textureCompression=TextureImporterCompression.CompressedHQ;hi.SaveAndReimport();
                var mat=app.World.globe.GetComponent<MeshRenderer>().sharedMaterial;mat.SetTexture("_NormalMap",AssetDatabase.LoadAssetAtPath<Texture2D>(heightPath));mat.EnableKeyword("_NORMALMAP");mat.EnableKeyword("_NORMALMAP_TANGENT_SPACE");EditorUtility.SetDirty(mat);
            }
            var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(root+"NASA_LROC_4K.tif");
            app.config.globalStart=4000;
            if(tex)
            {
                app.config.moonTexture=tex;
                app.config.imagerySource="NASA / GSFC / LROC · 4K color mosaic";
                var material=app.World.globe.GetComponent<MeshRenderer>().sharedMaterial;
                material.SetTexture("_BaseColorMap",tex);
                material.SetColor("_BaseColor",new Color(.8f,.8f,.8f));
                // Synthetic relief remains illustrative and deliberately restrained over real imagery.
                material.SetFloat("_NormalScale",.28f);EditorUtility.SetDirty(material);
            }
            app.Director.yaw=-48;app.Director.pitch=-25;app.Director.distance=4000;
            app.Director.surfaceYaw=45;app.Director.surfacePitch=3;
            foreach(var t in new[]{app.World.earth,app.World.sun}){t.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;EditorUtility.SetDirty(t.GetComponent<MeshRenderer>());}
            var terrainMesh=app.World.terrain.GetComponent<MeshFilter>().sharedMesh;
            if(!terrainMesh.name.Contains("horizon8"))
            {
                var points=terrainMesh.vertices;for(int i=0;i<points.Length;i++)points[i].y=LunarTerrain.Height(points[i].x,points[i].z,app.config.radius);
                terrainMesh.vertices=points;terrainMesh.RecalculateNormals();terrainMesh.RecalculateTangents();terrainMesh.RecalculateBounds();terrainMesh.name+=" / horizon8";EditorUtility.SetDirty(terrainMesh);
                app.World.terrain.GetComponent<MeshCollider>().sharedMesh=null;app.World.terrain.GetComponent<MeshCollider>().sharedMesh=terrainMesh;
                var rocks=app.World.terrain.transform.GetChild(0).GetComponent<MeshFilter>().sharedMesh;var rp=rocks.vertices;
                for(int i=0;i<rp.Length;i++)rp[i].y+=(rp[i].x*rp[i].x+rp[i].z*rp[i].z)/(2*app.config.radius)*.875f;
                rocks.vertices=rp;rocks.RecalculateBounds();rocks.name+=" / horizon8";EditorUtility.SetDirty(rocks);
            }
            app.Director.view.transform.position=LunarCoordinates.Direction(-25,-48)*4000;
            app.Director.view.transform.LookAt(Vector3.down*90,Vector3.up);
            var star=app.World.stars.GetComponent<MeshRenderer>().sharedMaterial;
            star.SetColor("_EmissiveColor",new Color(.25f,.28f,.32f));star.SetFloat("_EmissiveExposureWeight",0);star.SetFloat("_DoubleSidedEnable",1);star.SetFloat("_CullMode",0);star.SetFloat("_OpaqueCullMode",0);EditorUtility.SetDirty(star);
            var volume=Object.FindAnyObjectByType<Volume>();
            var profile=volume.sharedProfile;
            if(!profile.TryGet<Exposure>(out var exposure)){exposure=profile.Add<Exposure>(true);AssetDatabase.AddObjectToAsset(exposure,profile);}
            exposure.mode.Override(ExposureMode.Fixed);exposure.fixedExposure.Override(12.7f);EditorUtility.SetDirty(exposure);
            if(!profile.TryGet<Tonemapping>(out var tone)){tone=profile.Add<Tonemapping>(true);AssetDatabase.AddObjectToAsset(tone,profile);}
            tone.mode.Override(TonemappingMode.ACES);EditorUtility.SetDirty(tone);
            if(!profile.TryGet<Bloom>(out var bloom)){bloom=profile.Add<Bloom>(true);AssetDatabase.AddObjectToAsset(bloom,profile);}
            bloom.intensity.Override(.015f);EditorUtility.SetDirty(bloom);
            if(!profile.TryGet<HDShadowSettings>(out var shadows)){shadows=profile.Add<HDShadowSettings>(true);AssetDatabase.AddObjectToAsset(shadows,profile);}
            shadows.maxShadowDistance.Override(2500);EditorUtility.SetDirty(shadows);
            var ground=AssetDatabase.LoadAssetAtPath<Material>(root+"Rough lunar soil.mat");ground.SetFloat("_NormalScale",.17f);ground.SetTextureScale("_BaseColorMap",new Vector2(2,2));ground.SetColor("_BaseColor",new Color(.75f,.74f,.72f));EditorUtility.SetDirty(ground);
            EditorUtility.SetDirty(volume.sharedProfile);EditorUtility.SetDirty(app.config);EditorUtility.SetDirty(app.Director);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(app.gameObject.scene);EditorSceneManager.SaveOpenScenes();
            return tex?"NASA 4096 color integrated; scene refined and saved":"Refinements saved; NASA map not imported";
        }
    }
}
