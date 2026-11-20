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
        public int bestGoalStreak = 0;
        public int totalMatchesPlayed = 0;
        public int totalRoundsWon = 0;
        public int finalRoundBestScore = 0;

        // Audio settings
        public float musicVolume = 0.8f;
        public float sfxVolume = 0.9f;

        public SaveData()
        {
            unlockedRounds = new List<int> { 1 };
            unlockedBalls = new List<BallType> { BallType.Classic };
            equippedBall = BallType.Classic;
            unlockedStadiums = new List<StadiumType> { StadiumType.Cantera };
            selectedStadium = StadiumType.Cantera;
            unlockedAchievements = new List<string>();
        }
    }
}
