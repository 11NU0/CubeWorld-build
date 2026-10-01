using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CubeWorldBuild
{
    public static class CWBuildScript
    {
        private const string ScenePath = "Assets/Main.unity";
        private const string ProductName = "CubeWorld";

        [MenuItem("CubeWorld/Build/Windows x64")]
        public static void BuildWindows64()
        {
            Build(BuildTarget.StandaloneWindows64, ResolveOutputDir("Windows64"), "CubeWorld.exe");
        }

        [MenuItem("CubeWorld/Build/WebGL")]
        public static void BuildWebGL()
        {
            Build(BuildTarget.WebGL, ResolveOutputDir("WebGL"), "");
        }

        [MenuItem("CubeWorld/Build/All (Windows x64 + WebGL)")]
        public static void BuildAll()
        {
            BuildWindows64();
            BuildWebGL();
        }

        private static string ResolveOutputDir(string platformFolder)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.Combine(projectRoot, "Build", platformFolder);
        }

        private static void Build(BuildTarget target, string outputDir, string exeName)
        {
            Directory.CreateDirectory(outputDir);

            string locationPath = string.IsNullOrEmpty(exeName)
                ? outputDir
                : Path.Combine(outputDir, exeName);

            string[] scenes = ResolveScenes();
            if (scenes.Length == 0)
            {
                throw new Exception("No enabled scene found in build settings; cannot build.");
            }

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = locationPath,
                target = target,
                targetGroup = BuildPipeline.GetBuildTargetGroup(target),
                options = BuildOptions.None
            };

            Debug.Log("[CWBuild] Building " + target + " -> " + locationPath);
            Debug.Log("[CWBuild] Scenes: " + string.Join(", ", scenes));

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            Debug.Log(string.Format(
                "[CWBuild] RESULT {0} target={1} size={2} time={3}s errors={4} warnings={5}",
                summary.result, summary.platform, summary.totalSize,
                summary.totalTime.TotalSeconds, summary.totalErrors, summary.totalWarnings));

            if (summary.result != BuildResult.Succeeded)
            {
                foreach (var step in report.steps)
                {
                    foreach (var msg in step.messages)
                    {
                        if (msg.type == LogType.Error || msg.type == LogType.Exception)
                        {
                            Debug.LogError("[CWBuild] " + msg.content);
                        }
                    }
                }
                throw new Exception("Build failed for " + target + ": " + summary.result);
            }

            Debug.Log("[CWBuild] Output: " + locationPath);
        }

        private static string[] ResolveScenes()
        {
            var enabled = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .Where(p => !string.IsNullOrEmpty(p) && File.Exists(p))
                .ToArray();

            if (enabled.Length > 0)
            {
                return enabled;
            }

            Debug.LogWarning("[CWBuild] Build settings had no valid enabled scene, falling back to " + ScenePath);
            return new[] { ScenePath };
        }
    }
}
