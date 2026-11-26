using System;
using System.Collections.Generic;
using UltimatePenaltyCup.Core;

namespace UltimatePenaltyCup.SaveSystem
{
    [Serializable]
    public class SaveData
    {
        public int coins = 0;
        public int totalPoints = 0;
        public int highScore = 0;

        public List<int> unlockedRounds = new List<int> { 1 };
        public List<BallType> unlockedBalls = new List<BallType> { BallType.Classic };
        public BallType equippedBall = BallType.Classic;

        public List<StadiumType> unlockedStadiums = new List<StadiumType> { StadiumType.Cantera };
        public StadiumType selectedStadium = StadiumType.Cantera;

        public List<string> unlockedAchievements = new List<string>();

        // Statistics
        public int totalGoalsScored = 0;
        public int totalCornerGoals = 0;
        public int bestGoalStreak = 0;
        public int totalMatchesPlayed = 0;
        public int totalRoundsWon = 0;
        public int finalRoundBestScore = 0;

        // Audio & Display settings (with backward-compatible defaults)
        public float masterVolume = 1.0f;
        public float musicVolume = 0.8f;
        public float sfxVolume = 0.9f;
        public float crowdVolume = 0.85f;
        public float uiVolume = 0.90f;
        public int resolutionIndex = -1;
        public bool isFullscreen = true;
        public int qualityLevel = 2;
        public float aimSensitivity = 1.0f;
        public bool useCustomStadiumOverride = false;
        public List<string> redeemedCodes = new List<string>();

        public SaveData()
        {
            unlockedRounds = new List<int> { 1 };
            unlockedBalls = new List<BallType> { BallType.Classic };
            equippedBall = BallType.Classic;
            unlockedStadiums = new List<StadiumType> { StadiumType.Cantera };
            selectedStadium = StadiumType.Cantera;
            unlockedAchievements = new List<string>();
            redeemedCodes = new List<string>();
        }
    }
}
