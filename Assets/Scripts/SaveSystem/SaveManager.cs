using System;
using UnityEngine;
using UltimatePenaltyCup.Core;

namespace UltimatePenaltyCup.SaveSystem
{
    public class SaveManager : MonoBehaviour
    {
        private const string SAVE_KEY = "UltimatePenaltyCup_SaveData_v1";

        public static SaveManager Instance { get; private set; }

        public SaveData CurrentData { get; private set; }

        public event Action<SaveData> OnDataLoaded;
        public event Action<SaveData> OnDataSaved;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }

        public void Load()
        {
            try
            {
                if (PlayerPrefs.HasKey(SAVE_KEY))
                {
                    string json = PlayerPrefs.GetString(SAVE_KEY);
                    CurrentData = JsonUtility.FromJson<SaveData>(json);
                    if (CurrentData == null)
                    {
                        CurrentData = new SaveData();
                    }
                }
                else
                {
                    CurrentData = new SaveData();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveManager] Error loading save data: {ex.Message}. Initializing new save.");
                CurrentData = new SaveData();
            }

            ValidateDataIntegrity();
            OnDataLoaded?.Invoke(CurrentData);
        }

        public void Save()
        {
            if (CurrentData == null) return;

            try
            {
                string json = JsonUtility.ToJson(CurrentData, true);
                PlayerPrefs.SetString(SAVE_KEY, json);
                PlayerPrefs.Save();
                OnDataSaved?.Invoke(CurrentData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Error saving data: {ex.Message}");
            }
        }

        private void ValidateDataIntegrity()
        {
            if (CurrentData.unlockedRounds == null || CurrentData.unlockedRounds.Count == 0)
            {
                CurrentData.unlockedRounds = new System.Collections.Generic.List<int> { 1 };
            }

            if (!CurrentData.unlockedRounds.Contains(1))
            {
                CurrentData.unlockedRounds.Add(1);
            }

            if (CurrentData.unlockedBalls == null || CurrentData.unlockedBalls.Count == 0)
            {
                CurrentData.unlockedBalls = new System.Collections.Generic.List<BallType> { BallType.Classic };
            }

            if (!CurrentData.unlockedBalls.Contains(BallType.Classic))
            {
                CurrentData.unlockedBalls.Add(BallType.Classic);
            }

            if (CurrentData.unlockedStadiums == null || CurrentData.unlockedStadiums.Count == 0)
            {
                CurrentData.unlockedStadiums = new System.Collections.Generic.List<StadiumType> { StadiumType.Cantera };
            }

            if (!CurrentData.unlockedStadiums.Contains(StadiumType.Cantera))
            {
                CurrentData.unlockedStadiums.Add(StadiumType.Cantera);
            }

            if (CurrentData.unlockedAchievements == null)
            {
                CurrentData.unlockedAchievements = new System.Collections.Generic.List<string>();
            }

            if (CurrentData.coins < 0) CurrentData.coins = 0;
            if (CurrentData.totalPoints < 0) CurrentData.totalPoints = 0;
        }

        public bool UnlockRound(int roundNumber)
        {
            if (!CurrentData.unlockedRounds.Contains(roundNumber))
            {
                CurrentData.unlockedRounds.Add(roundNumber);
                Save();
                return true;
            }
            return false;
        }

        public bool IsRoundUnlocked(int roundNumber)
        {
            return CurrentData != null && CurrentData.unlockedRounds.Contains(roundNumber);
        }

        public bool UnlockBall(BallType ball)
        {
            if (!CurrentData.unlockedBalls.Contains(ball))
            {
                CurrentData.unlockedBalls.Add(ball);
                Save();
                return true;
            }
            return false;
        }

        public bool IsBallUnlocked(BallType ball)
        {
            return CurrentData != null && CurrentData.unlockedBalls.Contains(ball);
        }

        public void EquipBall(BallType ball)
        {
            if (IsBallUnlocked(ball))
            {
                CurrentData.equippedBall = ball;
                Save();
            }
        }

        public bool UnlockStadium(StadiumType stadium)
        {
            if (!CurrentData.unlockedStadiums.Contains(stadium))
            {
                CurrentData.unlockedStadiums.Add(stadium);
                Save();
                return true;
            }
            return false;
        }

        public bool IsStadiumUnlocked(StadiumType stadium)
        {
            return CurrentData != null && CurrentData.unlockedStadiums.Contains(stadium);
        }

        public void SelectStadium(StadiumType stadium)
        {
            if (IsStadiumUnlocked(stadium))
            {
                CurrentData.selectedStadium = stadium;
                Save();
            }
        }

        public void UpdateHighScore(int matchScore)
        {
            if (CurrentData != null && matchScore > CurrentData.highScore)
            {
                CurrentData.highScore = matchScore;
                Save();
            }
        }

        public void ResetAllData()
        {
            PlayerPrefs.DeleteKey(SAVE_KEY);
            CurrentData = new SaveData();
            Save();
            Debug.Log("[SaveManager] All save data has been reset to defaults.");
        }
    }
}
