using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UltimatePenaltyCup.SaveSystem;
using UltimatePenaltyCup.Levels;

namespace UltimatePenaltyCup.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public const string SCENE_MAIN_MENU = "MainMenu";
        public const string SCENE_MATCH = "MatchScene";

        [Header("Configuration")]
        [SerializeField] private RoundDataSO[] roundConfigs;

        public int CurrentSelectedRoundNumber { get; private set; } = 1;
        public RoundDataSO CurrentRoundData => GetRoundData(CurrentSelectedRoundNumber);
        public bool RequestOpenAchievementsOnMenu { get; set; } = false;

        public event Action<int> OnRoundSelected;

        private int perfFrameCount = 0;
        private float perfAccumulatedTime = 0f;
        private bool perfLoggedForCurrentScene = false;
        private string perfSceneName = "";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 1;
            EnsureCoreManagers();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Time.timeScale = 1f;
            DeduplicateSceneSingletons();

            perfSceneName = scene.name;
            perfFrameCount = 0;
            perfAccumulatedTime = 0f;
            perfLoggedForCurrentScene = false;

            if (scene.name == SCENE_MAIN_MENU)
            {
                GameStateManager.Instance?.ChangeState(GameState.MainMenu);
            }
        }

        private void Update()
        {
            if (perfLoggedForCurrentScene) return;
            float dt = Time.unscaledDeltaTime;
            if (dt > 0.00001f && dt < 0.25f)
            {
                perfFrameCount++;
                perfAccumulatedTime += dt;
                if (perfFrameCount >= 120)
                {
                    perfLoggedForCurrentScene = true;
                    float avgDt = perfAccumulatedTime / perfFrameCount;
                    float fps = 1f / Mathf.Max(0.00001f, avgDt);
                    float avgMs = avgDt * 1000f;
                    string qualName = QualitySettings.names != null && QualitySettings.GetQualityLevel() < QualitySettings.names.Length
                        ? QualitySettings.names[QualitySettings.GetQualityLevel()]
                        : QualitySettings.GetQualityLevel().ToString();
                    Debug.Log($"[Performance] Scene={perfSceneName} Res={Screen.width}x{Screen.height} AvgFPS={fps:F1} AvgFrameMs={avgMs:F2} Quality={qualName}");
                }
            }
        }

        private void DeduplicateSceneSingletons()
        {
            EventSystem[] eventSystems = FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (eventSystems.Length > 1)
            {
                EventSystem keep = null;
                for (int i = 0; i < eventSystems.Length; i++)
                {
                    if (eventSystems[i].gameObject.activeInHierarchy)
                    {
                        keep = eventSystems[i];
                        break;
                    }
                }
                if (keep == null) keep = eventSystems[0];

                for (int i = 0; i < eventSystems.Length; i++)
                {
                    if (eventSystems[i] != keep)
                    {
                        Destroy(eventSystems[i].gameObject);
                    }
                }
            }

            AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (listeners.Length > 1)
            {
                AudioListener keepListener = Camera.main != null ? Camera.main.GetComponent<AudioListener>() : null;
                if (keepListener == null) keepListener = listeners[0];

                for (int i = 0; i < listeners.Length; i++)
                {
                    if (listeners[i] != keepListener)
                    {
                        listeners[i].enabled = false;
                    }
                }
            }
        }

        private void EnsureCoreManagers()
        {
            if (GetComponent<SaveManager>() == null) gameObject.AddComponent<SaveManager>();
            if (GetComponent<GameStateManager>() == null) gameObject.AddComponent<GameStateManager>();
            if (GetComponent<RoundManager>() == null) gameObject.AddComponent<RoundManager>();
            if (GetComponent<Economy.CurrencyManager>() == null) gameObject.AddComponent<Economy.CurrencyManager>();
            if (GetComponent<Economy.ScoreManager>() == null) gameObject.AddComponent<Economy.ScoreManager>();
            if (GetComponent<Audio.AudioManager>() == null) gameObject.AddComponent<Audio.AudioManager>();
            if (GetComponent<Shop.ShopManager>() == null) gameObject.AddComponent<Shop.ShopManager>();
            if (GetComponent<Customization.CustomizationManager>() == null) gameObject.AddComponent<Customization.CustomizationManager>();
            if (GetComponent<Achievements.AchievementManager>() == null) gameObject.AddComponent<Achievements.AchievementManager>();
        }

        public void SetRoundConfigs(RoundDataSO[] configs)
        {
            roundConfigs = configs;
        }

        public RoundDataSO GetRoundData(int roundNumber)
        {
            if (roundConfigs != null && roundConfigs.Length > 0)
            {
                foreach (var r in roundConfigs)
                {
                    if (r != null && r.roundNumber == roundNumber)
                    {
                        return r;
                    }
                }
            }

            if (RoundManager.Instance != null)
            {
                RoundDataSO fallback = RoundManager.Instance.GetRound(roundNumber);
                if (fallback != null) return fallback;
            }

            return null;
        }

        public void SelectRoundAndStartMatch(int roundNumber)
        {
            CurrentSelectedRoundNumber = Mathf.Clamp(roundNumber, 1, 5);
            Time.timeScale = 1f;
            OnRoundSelected?.Invoke(CurrentSelectedRoundNumber);
            LoadMatchScene();
        }

        public void LoadMatchScene()
        {
            GameStateManager.Instance?.ChangeState(GameState.Match);
            SceneManager.LoadScene(SCENE_MATCH);
        }

        public void LoadMainMenuScene()
        {
            Time.timeScale = 1f;
            GameStateManager.Instance?.ChangeState(GameState.MainMenu);
            SceneManager.LoadScene(SCENE_MAIN_MENU);
        }

        public void QuitGame()
        {
            Debug.Log("[GameManager] Quitting application...");
            SaveManager.Instance?.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
