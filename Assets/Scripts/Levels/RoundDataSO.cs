using UnityEngine;
using UltimatePenaltyCup.Core;

namespace UltimatePenaltyCup.Levels
{
    [CreateAssetMenu(fileName = "RoundData_", menuName = "UltimatePenaltyCup/RoundData")]
    public class RoundDataSO : ScriptableObject
    {
        [Header("General Info")]
        public int roundNumber = 1;
        public string roundName = "Cantera";
        [TextArea(2, 4)]
        public string description = "Tutorial / primera ronda. Introduce el movimiento, apuntado, potencia y disparo.";

        [Header("Rules")]
        public int requiredGoals = 3;
        public int maxAttempts = 5;

        [Header("Rewards")]
        public int winPoints = 500;
        public int winCoins = 300;

        [Header("Environment")]
        public StadiumType stadiumTheme = StadiumType.Cantera;

        [Header("Goalkeeper AI Tuning")]
        public string goalkeeperName = "Malágon";
        public string goalkeeperTypeTitle = "Portero Básico";
        [Range(0.05f, 0.6f)]
        public float reactionDelay = 0.38f;
        [Range(2f, 12f)]
        public float moveSpeed = 3.5f;
        [Range(3f, 15f)]
        public float diveSpeed = 4.0f;
        [Range(0f, 0.5f)]
        public float errorProbability = 0.35f;
    }
}
