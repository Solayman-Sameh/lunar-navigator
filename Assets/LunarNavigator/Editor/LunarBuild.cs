using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LunarNavigator.Editor
{
    public static class LunarBuild
    {
        [MenuItem("Lunar Navigator/Build Windows 64-bit")]
        public static string BuildWin64()
        {
            string buildDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", "Win64"));
            Directory.CreateDirectory(buildDir);
            string exePath = Path.Combine(buildDir, "LunarNavigator.exe");
            File.WriteAllText(Path.Combine(buildDir, "..", "build_progress.txt"), "Starting build at " + DateTime.UtcNow.ToString("O"));

            PlayerSettings.productName = "Lunar Navigator";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);

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
            File.WriteAllText(Path.Combine(buildDir, "..", "build_progress.txt"), "Completed: " + result);

            if (summary.result == BuildResult.Succeeded)
            {
                return result;
            }
            else
            {
                throw new Exception($"Build failed with {summary.totalErrors} error(s). See console log for details.");
            }
        }
    }
}
