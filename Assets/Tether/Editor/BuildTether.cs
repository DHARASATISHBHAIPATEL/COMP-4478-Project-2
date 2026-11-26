using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Tether.EditorTools
{
    /// <summary>
    /// Command-line Windows build:
    /// Unity.exe -batchmode -quit -projectPath . -executeMethod Tether.EditorTools.BuildTether.Build
    /// Output: Build/Tether.exe
    /// </summary>
    public static class BuildTether
    {
        const string ScenePath = "Assets/Tether/Tether.unity";

        [UnityEditor.MenuItem("Tether/Build Windows (Build/Tether.exe)")]
        public static void Build()
        {
            // In batch mode EditorApplication.delayCall never fires before -quit,
            // so run the first-import setup (scene + sprite material) directly.
            TetherSetup.Ensure(false);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Build", "Tether.exe"));
            Directory.CreateDirectory(Path.GetDirectoryName(output));

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary s = report.summary;
            Debug.Log($"[BuildTether] Result: {s.result}, errors: {s.totalErrors}, warnings: {s.totalWarnings}, size: {s.totalSize} bytes, output: {output}");

            if (Application.isBatchMode)
                EditorApplication.Exit(s.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
