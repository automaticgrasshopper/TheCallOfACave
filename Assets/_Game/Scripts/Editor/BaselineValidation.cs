using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TCC.EditorTools
{
    /// <summary>Repeatable Day 1 environment check and development-player build entry.</summary>
    public static class BaselineValidation
    {
        private const string ExpectedUnityVersion = "2022.3.44f1";
        private const string MainScene = "Assets/_Game/Scenes/Main.unity";

        [MenuItem("TCC/Validation/Build Baseline Smoke Player")]
        public static void BuildFromMenu() => Build(false);

        public static void BuildBatch() => Build(true);

        private static void Build(bool exitWhenDone)
        {
            try
            {
                ValidateEnvironment();
                string output = ReadArgument("-tccBaselineOutput");
                if (string.IsNullOrWhiteSpace(output))
                    output = Path.GetFullPath("Builds/BaselineSmoke/The Call of the Cave.app");

                Directory.CreateDirectory(Path.GetDirectoryName(output));
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { MainScene },
                    locationPathName = output,
                    target = BuildTarget.StandaloneOSX,
                    options = BuildOptions.Development
                });

                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException($"Player build failed: {report.summary.result}");

                Debug.Log($"[TCC Baseline] Development player built: {output}");
                if (exitWhenDone) EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (exitWhenDone) EditorApplication.Exit(1);
                else throw;
            }
        }

        private static void ValidateEnvironment()
        {
            if (Application.unityVersion != ExpectedUnityVersion)
                throw new InvalidOperationException(
                    $"Expected Unity {ExpectedUnityVersion}, found {Application.unityVersion}.");

            if (!BuildPipeline.IsBuildTargetSupported(
                    BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
                throw new InvalidOperationException("Windows x64 build support is not installed.");

            var enabledScenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).ToArray();
            if (enabledScenes.Length != 1 || enabledScenes[0].path != MainScene)
                throw new InvalidOperationException(
                    $"Build settings must contain only the enabled main scene: {MainScene}.");

            if (!File.Exists(MainScene))
                throw new FileNotFoundException("Main scene is missing.", MainScene);

            Debug.Log("[TCC Baseline] Environment verified: Unity 2022.3.44f1, licensed batch startup, " +
                "Windows x64 support present, Main.unity is the sole enabled scene.");
        }

        private static string ReadArgument(string key)
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, key);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}
