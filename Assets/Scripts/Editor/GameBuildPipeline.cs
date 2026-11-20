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
                Debug.Log($">>> BUILD SUCCEEDED! Total Size: {summary.totalSize / (1024 * 1024):F2} MB <<<");
                Debug.Log($"Location: {Path.GetFullPath(buildPath)}");
                Debug.Log("==========================================================");
            }
            else
            {
                Debug.LogError($"BUILD FAILED with result: {summary.result}. Total Errors: {summary.totalErrors}");
            }
        }
    }
}
