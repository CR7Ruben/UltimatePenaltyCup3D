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

            SyncToGameManager();
        }

        private void Start()
        {
            SyncToGameManager();
        }

        public void SetRounds(List<RoundDataSO> roundList)
        {
            rounds = roundList;
            SyncToGameManager();
        }

        private void SyncToGameManager()
        {
            if (GameManager.Instance != null && rounds != null && rounds.Count > 0)
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
