using System;
using UnityEngine;
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

        public event Action<int> OnRoundSelected;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureCoreManagers();
        }

        private void EnsureCoreManagers()
        {
            if (GetComponent<SaveManager>() == null) gameObject.AddComponent<SaveManager>();
            if (GetComponent<GameStateManager>() == null) gameObject.AddComponent<GameStateManager>();
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
            return null;
        }

        public void SelectRoundAndStartMatch(int roundNumber)
        {
            CurrentSelectedRoundNumber = roundNumber;
            OnRoundSelected?.Invoke(roundNumber);
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
