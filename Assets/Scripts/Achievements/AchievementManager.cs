using System;
using System.Collections.Generic;
using UnityEngine;
using UltimatePenaltyCup.Core;
using UltimatePenaltyCup.SaveSystem;
using UltimatePenaltyCup.Economy;
using UltimatePenaltyCup.Audio;

namespace UltimatePenaltyCup.Achievements
{
    public class AchievementManager : MonoBehaviour
    {
        public static AchievementManager Instance { get; private set; }

        public const string ID_FIRST_GOAL = "FIRST_GOAL";
        public const string ID_SNIPER_10 = "SNIPER_10";
        public const string ID_PERFECT_STREAK = "PERFECT_STREAK";
        public const string ID_CHAMPION = "CHAMPION";
        public const string ID_MILLIONAIRE = "MILLIONAIRE";
        public const string ID_COLLECTOR = "COLLECTOR";
        public const string ID_LEGEND = "LEGEND";

        public const int LEGEND_SCORE_THRESHOLD = 1500;

        [SerializeField] private List<AchievementSO> achievements = new List<AchievementSO>();

        public event Action<AchievementSO> OnAchievementUnlocked;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetAchievements(List<AchievementSO> list)
        {
            achievements = list;
        }

        public List<AchievementSO> GetAllAchievements() => achievements;

        public AchievementSO GetAchievement(string id)
        {
            return achievements.Find(a => a.id == id);
        }

        public bool IsUnlocked(string id)
        {
            return SaveManager.Instance != null &&
                   SaveManager.Instance.CurrentData != null &&
                   SaveManager.Instance.CurrentData.unlockedAchievements.Contains(id);
        }

        public bool UnlockAchievement(string id)
        {
            if (SaveManager.Instance == null || SaveManager.Instance.CurrentData == null) return false;

            if (IsUnlocked(id)) return false;

            SaveManager.Instance.CurrentData.unlockedAchievements.Add(id);
            AchievementSO so = GetAchievement(id);

            if (so != null)
            {
                if (so.rewardCoins > 0) CurrencyManager.Instance?.AddCoins(so.rewardCoins);
                if (so.rewardPoints > 0) CurrencyManager.Instance?.AddPoints(so.rewardPoints);
            }

            SaveManager.Instance.Save();
            AudioManager.Instance?.PlaySfx(SfxType.S05_Unlock);
            OnAchievementUnlocked?.Invoke(so);
            Debug.Log($"[AchievementManager] ¡Logro Desbloqueado!: {id} - {so?.title}");
            return true;
        }

        public void CheckGoalScored(int currentStreak, int totalGoals)
        {
            if (totalGoals >= 1) UnlockAchievement(ID_FIRST_GOAL);
            if (totalGoals >= 10) UnlockAchievement(ID_SNIPER_10);
            if (currentStreak >= 5) UnlockAchievement(ID_PERFECT_STREAK);
        }

        public void CheckCoins(int totalCoins)
        {
            if (totalCoins >= 10000)
            {
                UnlockAchievement(ID_MILLIONAIRE);
            }
        }

        public void CheckBallsCollector()
        {
            if (SaveManager.Instance == null || SaveManager.Instance.CurrentData == null) return;
            var list = SaveManager.Instance.CurrentData.unlockedBalls;
            if (list.Contains(BallType.Classic) &&
                list.Contains(BallType.Blue) &&
                list.Contains(BallType.Fire) &&
                list.Contains(BallType.Rainbow) &&
                list.Contains(BallType.Gold))
            {
                UnlockAchievement(ID_COLLECTOR);
            }
        }

        public void CheckRoundCompleted(int roundNumber, int matchScore)
        {
            if (roundNumber == 5)
            {
                UnlockAchievement(ID_CHAMPION);
                if (matchScore >= LEGEND_SCORE_THRESHOLD)
                {
                    UnlockAchievement(ID_LEGEND);
                }
            }
        }
    }
}
