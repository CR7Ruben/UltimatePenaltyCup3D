using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace UltimatePenaltyCup.Editor
{
    public static class GameBuildPipeline
    {
        private const string BUILD_OUTPUT_DIR = "Builds/Windows";
        private const string BUILD_EXE_NAME = "UltimatePenaltyCup3D.exe";

        [MenuItem("Ultimate Penalty Cup/Build Windows Standalone (x64)", false, 10)]
        public static void BuildWindowsStandalone()
        {
            Debug.Log("==========================================================");
            Debug.Log(">>> STARTING WINDOWS STANDALONE X64 BUILD PIPELINE <<<");
            Debug.Log("==========================================================");

            if (!Directory.Exists(BUILD_OUTPUT_DIR))
            {
                Directory.CreateDirectory(BUILD_OUTPUT_DIR);
            }

            string buildPath = Path.Combine(BUILD_OUTPUT_DIR, BUILD_EXE_NAME);

            // Enforce official product settings and build scenes order (MainMenu=0, MatchScene=1, NO SampleScene)
            PlayerSettings.companyName = "A&R Game Labs";
            PlayerSettings.productName = "UltimatePenaltyCup3D";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.resizableWindow = true;

            EditorBuildSettingsScene[] buildScenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/MatchScene.unity", true)
            };
            EditorBuildSettings.scenes = buildScenes;

            string[] scenes = new string[]
            {
                "Assets/Scenes/MainMenu.unity",
                "Assets/Scenes/MatchScene.unity"
            };

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = buildPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log("----------------------------------------------------------");
                Debug.Log($">>> BUILD SUCCEEDED! Total Size: {summary.totalSize / (1024f * 1024f):F2} MB | Warnings: {summary.totalWarnings} | Errors: {summary.totalErrors} <<<");
                Debug.Log($"Location: {Path.GetFullPath(buildPath)}");
                Debug.Log("==========================================================");
            }
            else
            {
                Debug.LogError($"BUILD FAILED with result: {summary.result}. Total Errors: {summary.totalErrors}");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
            }
        }
    }
}
