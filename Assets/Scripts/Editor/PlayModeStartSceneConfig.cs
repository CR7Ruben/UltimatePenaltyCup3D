using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UltimatePenaltyCup.Editor
{
    /// <summary>
    /// Ensures Play Mode always starts from MainMenu.unity (build index 0),
    /// while preserving an explicit debug menu toggle for direct MatchScene testing
    /// and avoiding interference with Unity Test Runner InitTestScene.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeStartSceneConfig
    {
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string PrefAllowDirectMatch = "UltimatePenaltyCup_AllowDirectMatchScene";
        private const string MenuAllowDirect = "Ultimate Penalty Cup/Debug/Allow Direct MatchScene Play";

        static PlayModeStartSceneConfig()
        {
            ApplyPlayModeStartScene();
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public static bool IsDirectMatchAllowed => EditorPrefs.GetBool(PrefAllowDirectMatch, false);

        private static bool IsRunningCommandLineTests =>
            Environment.CommandLine.IndexOf("-runTests", StringComparison.OrdinalIgnoreCase) >= 0;

        public static void ApplyPlayModeStartScene()
        {
            if (IsRunningCommandLineTests || IsDirectMatchAllowed)
            {
                EditorSceneManager.playModeStartScene = null;
            }
            else
            {
                SceneAsset mainMenu = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath);
                if (mainMenu != null)
                {
                    EditorSceneManager.playModeStartScene = mainMenu;
                }
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                string activePath = EditorSceneManager.GetActiveScene().path;
                if (IsRunningCommandLineTests || (!string.IsNullOrEmpty(activePath) && activePath.Contains("InitTestScene")))
                {
                    EditorSceneManager.playModeStartScene = null;
                }
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                ApplyPlayModeStartScene();
            }
        }

        [MenuItem(MenuAllowDirect, false, 200)]
        public static void ToggleAllowDirectMatchScene()
        {
            bool nextValue = !IsDirectMatchAllowed;
            EditorPrefs.SetBool(PrefAllowDirectMatch, nextValue);
            ApplyPlayModeStartScene();
            Debug.Log(nextValue
                ? "[PlayModeStartSceneConfig] Debug mode ENABLED: Editor Play starts in the currently open scene."
                : "[PlayModeStartSceneConfig] Standard mode ENABLED: Editor Play ALWAYS starts in MainMenu.unity.");
        }

        [MenuItem(MenuAllowDirect, true, 200)]
        public static bool ValidateToggleAllowDirectMatchScene()
        {
            Menu.SetChecked(MenuAllowDirect, IsDirectMatchAllowed);
            return true;
        }
    }
}
