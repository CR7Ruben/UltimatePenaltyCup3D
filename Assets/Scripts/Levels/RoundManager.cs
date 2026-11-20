using System;
using System.Collections.Generic;
using UnityEngine;
using UltimatePenaltyCup.Core;
using UltimatePenaltyCup.SaveSystem;

namespace UltimatePenaltyCup.Levels
{
    public class RoundManager : MonoBehaviour
    {
        public static RoundManager Instance { get; private set; }

        [Header("Round Configurations (1 to 5)")]
        [SerializeField] private List<RoundDataSO> rounds = new List<RoundDataSO>();

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

        public void SetRounds(List<RoundDataSO> roundList)
        {
            rounds = roundList;
            if (GameManager.Instance != null && rounds != null)
            {
                GameManager.Instance.SetRoundConfigs(rounds.ToArray());
            }
        }

        public List<RoundDataSO> GetAllRounds() => rounds;

        public RoundDataSO GetRound(int roundNumber)
        {
            return rounds.Find(r => r.roundNumber == roundNumber);
        }

        public bool IsRoundUnlocked(int roundNumber)
        {
            return SaveManager.Instance != null && SaveManager.Instance.IsRoundUnlocked(roundNumber);
        }
    }
}
