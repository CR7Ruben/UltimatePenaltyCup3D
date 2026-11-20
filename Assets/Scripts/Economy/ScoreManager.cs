using System;
using UnityEngine;
using UltimatePenaltyCup.Core;

namespace UltimatePenaltyCup.Economy
{
    public struct ShotScoreResult
    {
        public ShotResult shotType;
        public int pointsEarned;
        public int coinsEarned;
        public int streakCount;
        public int streakBonusPoints;
        public int streakBonusCoins;
        public string rewardDescription;
    }

    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        public const int NORMAL_GOAL_POINTS = 100;
        public const int NORMAL_GOAL_COINS = 100;

        public const int CORNER_GOAL_POINTS = 200;
        public const int CORNER_GOAL_COINS = 150;

        public const int HIGH_POWER_GOAL_POINTS = 150;
        public const int HIGH_POWER_GOAL_COINS = 125;

        public const int STREAK_BONUS_POINTS_PER_STREAK = 50;
        public const int STREAK_BONUS_COINS_PER_STREAK = 25;

        public int CurrentMatchScore { get; private set; } = 0;
        public int CurrentMatchCoins { get; private set; } = 0;
        public int CurrentStreak { get; private set; } = 0;

        public event Action<ShotScoreResult> OnShotScored;
        public event Action<int> OnMatchScoreUpdated;

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

        public void ResetMatchScore()
        {
            CurrentMatchScore = 0;
            CurrentMatchCoins = 0;
            CurrentStreak = 0;
            OnMatchScoreUpdated?.Invoke(CurrentMatchScore);
        }

        /// <summary>
        /// Evaluates shot scoring with unambiguous tier priority to prevent duplicate rewards:
        /// 1. Corner / Precision Goal (Top tier: 200 pts, 150 coins)
        /// 2. High Power Goal (Mid tier: 150 pts, 125 coins)
        /// 3. Normal Goal (Base tier: 100 pts, 100 coins)
        /// Streak bonuses are calculated separately and added.
        /// </summary>
        public ShotScoreResult ProcessGoal(bool isCornerOrPrecision, bool isHighPower)
        {
            CurrentStreak++;

            ShotResult type;
            int basePoints;
            int baseCoins;
            string description;

            if (isCornerOrPrecision)
            {
                type = ShotResult.GoalCorner;
                basePoints = CORNER_GOAL_POINTS;
                baseCoins = CORNER_GOAL_COINS;
                description = "¡GOLAZO AL ÁNGULO! (Precisión)";
            }
            else if (isHighPower)
            {
                type = ShotResult.GoalHighPower;
                basePoints = HIGH_POWER_GOAL_POINTS;
                baseCoins = HIGH_POWER_GOAL_COINS;
                description = "¡POTENCIA MÁXIMA!";
            }
            else
            {
                type = ShotResult.GoalNormal;
                basePoints = NORMAL_GOAL_POINTS;
                baseCoins = NORMAL_GOAL_COINS;
                description = "¡GOOOOL!";
            }

            // Streak bonuses apply when streak is 2 or higher
            int streakPts = (CurrentStreak > 1) ? (CurrentStreak - 1) * STREAK_BONUS_POINTS_PER_STREAK : 0;
            int streakCoins = (CurrentStreak > 1) ? (CurrentStreak - 1) * STREAK_BONUS_COINS_PER_STREAK : 0;

            int totalPts = basePoints + streakPts;
            int totalCoins = baseCoins + streakCoins;

            CurrentMatchScore += totalPts;
            CurrentMatchCoins += totalCoins;

            CurrencyManager.Instance?.AddPoints(totalPts);
            CurrencyManager.Instance?.AddCoins(totalCoins);

            ShotScoreResult result = new ShotScoreResult
            {
                shotType = type,
                pointsEarned = totalPts,
                coinsEarned = totalCoins,
                streakCount = CurrentStreak,
                streakBonusPoints = streakPts,
                streakBonusCoins = streakCoins,
                rewardDescription = description
            };

            OnShotScored?.Invoke(result);
            OnMatchScoreUpdated?.Invoke(CurrentMatchScore);

            return result;
        }

        public void ProcessMissOrSave()
        {
            CurrentStreak = 0;
        }

        public void AwardRoundVictory(int bonusPoints, int bonusCoins)
        {
            CurrentMatchScore += bonusPoints;
            CurrentMatchCoins += bonusCoins;

            CurrencyManager.Instance?.AddPoints(bonusPoints);
            CurrencyManager.Instance?.AddCoins(bonusCoins);

            OnMatchScoreUpdated?.Invoke(CurrentMatchScore);
        }
    }
}
