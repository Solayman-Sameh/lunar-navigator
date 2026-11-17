using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LunarNavigator.Editor
{
    public static class LunarBuild
    {
        public static void BuildRelease() { BuildWin64(); }
        [MenuItem("Lunar Navigator/Build Windows 64-bit")]
        public static string BuildWin64()
        {
            string buildDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", "Win64"));
            Directory.CreateDirectory(buildDir);
            string exePath = Path.Combine(buildDir, "LunarNavigator.exe");
            File.Delete(Path.Combine(buildDir, "source-hashes.txt"));

            PlayerSettings.productName = "Lunar Navigator";
            PlayerSettings.bundleVersion = "1.1.0";
            const string iconPath = "Assets/LunarNavigator/Resources/Branding/LunarNavigator.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(iconPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            var appIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
            if (appIcon == null) throw new InvalidOperationException("LunarNavigator application icon is missing.");
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { appIcon });
            var iconSizes = PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.Standalone);
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Standalone,
                iconSizes.Select(_ => appIcon).ToArray());
            PlayerSettings.SplashScreen.show = true;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.SplashScreen.backgroundColor = new Color(.025f, .04f, .065f);
            PlayerSettings.SplashScreen.logos = new[] {
                PlayerSettings.SplashScreenLogo.Create(2f, AssetDatabase.LoadAssetAtPath<Sprite>(iconPath))
            };
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);

            // Generated native meshes have no ModelImporter checkbox. Persist the
            // triangle bake setting explicitly; future Unity builds no longer infer it.
            var terrainMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/LunarNavigator/Generated/Terrain.asset");
            if (terrainMesh != null && !PhysicsEditorMeshExtensions.HasPreBakeCollisionMesh(terrainMesh, false))
            {
                PhysicsEditorMeshExtensions.SetPreBakeCollisionMesh(terrainMesh, false, true);
                EditorUtility.SetDirty(terrainMesh);
                AssetDatabase.SaveAssets();
            }

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/LunarNavigator_Main.unity" },
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            Debug.Log($"[LunarBuild] Starting StandaloneWindows64 build to: {exePath}");
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            string result = $"[LunarBuild] Result: {summary.result}, Time: {summary.totalTime.TotalSeconds:F1}s, Size: {summary.totalSize / (1024f * 1024f):F1} MB, Errors: {summary.totalErrors}, Warnings: {summary.totalWarnings}";
            Debug.Log(result);

            if (summary.result == BuildResult.Succeeded)
            {
                AssetDatabase.SaveAssets();
                string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                var files = new[] { "Assets", "Packages", "ProjectSettings" }
                    .SelectMany(dir => Directory.GetFiles(Path.Combine(root, dir), "*", SearchOption.AllDirectories))
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);
                using (var sha = SHA256.Create())
                using (var writer = new StreamWriter(Path.Combine(buildDir, "source-hashes.txt")))
                    foreach (string file in files)
                        using (var stream = File.OpenRead(file))
                            writer.WriteLine(BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "") + "|" +
                                file.Substring(root.Length + 1).Replace('\\', '/'));
                File.WriteAllText(Path.Combine(buildDir, "release-version.txt"), PlayerSettings.bundleVersion);
                return result;
            }
            else
            {
                throw new Exception($"Build failed with {summary.totalErrors} error(s). See console log for details.");
            }
        }
    }
}
