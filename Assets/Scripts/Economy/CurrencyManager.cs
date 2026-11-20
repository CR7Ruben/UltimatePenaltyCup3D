using System;
using UnityEngine;
using UltimatePenaltyCup.SaveSystem;

namespace UltimatePenaltyCup.Economy
{
    public class CurrencyManager : MonoBehaviour
    {
        public static CurrencyManager Instance { get; private set; }

        public const string SECRET_DEV_CODE = "PENALTY2026";
        public const int DEV_CODE_REWARD_COINS = 5000;

        public event Action<int, int> OnCoinsChanged;     // (totalCoins, delta)
        public event Action<int, int> OnPointsChanged;    // (totalPoints, delta)
        public event Action<string, bool> OnCodeAttempt;  // (message, success)

        public int Coins => SaveManager.Instance != null && SaveManager.Instance.CurrentData != null
            ? SaveManager.Instance.CurrentData.coins
            : 0;

        public int TotalPoints => SaveManager.Instance != null && SaveManager.Instance.CurrentData != null
            ? SaveManager.Instance.CurrentData.totalPoints
            : 0;

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

        public void AddCoins(int amount)
        {
            if (amount <= 0 || SaveManager.Instance == null) return;

            SaveManager.Instance.CurrentData.coins += amount;
            SaveManager.Instance.Save();
            OnCoinsChanged?.Invoke(SaveManager.Instance.CurrentData.coins, amount);
        }

        public bool SpendCoins(int amount)
        {
            if (amount < 0 || SaveManager.Instance == null) return false;
            if (SaveManager.Instance.CurrentData.coins < amount) return false;

            SaveManager.Instance.CurrentData.coins -= amount;
            SaveManager.Instance.Save();
            OnCoinsChanged?.Invoke(SaveManager.Instance.CurrentData.coins, -amount);
            return true;
        }

        public void AddPoints(int amount)
        {
            if (amount <= 0 || SaveManager.Instance == null) return;

            SaveManager.Instance.CurrentData.totalPoints += amount;
            if (SaveManager.Instance.CurrentData.totalPoints > SaveManager.Instance.CurrentData.highScore)
            {
                SaveManager.Instance.CurrentData.highScore = SaveManager.Instance.CurrentData.totalPoints;
            }
            SaveManager.Instance.Save();
            OnPointsChanged?.Invoke(SaveManager.Instance.CurrentData.totalPoints, amount);
        }

        public bool HasEnoughCoins(int amount)
        {
            return Coins >= amount;
        }

        /// <summary>
        /// Code for testing/development as specified in GDD Section 17 & 20: PENALTY2026.
        /// Only accessible in Development Builds and Unity Editor.
        /// </summary>
        public bool TryRedeemCode(string inputCode)
        {
            if (string.IsNullOrWhiteSpace(inputCode))
            {
                OnCodeAttempt?.Invoke("Código inválido.", false);
                return false;
            }

            string sanitized = inputCode.Trim().ToUpperInvariant();

            // Development code check
            if (sanitized == SECRET_DEV_CODE)
            {
                if (Debug.isDebugBuild || Application.isEditor)
                {
                    AddCoins(DEV_CODE_REWARD_COINS);
                    string successMsg = $"¡Código secreto activado! +{DEV_CODE_REWARD_COINS} monedas recibidas.";
                    Debug.Log($"[CurrencyManager] {successMsg}");
                    OnCodeAttempt?.Invoke(successMsg, true);
                    return true;
                }
                else
                {
                    OnCodeAttempt?.Invoke("Este código no está disponible en la versión final.", false);
                    return false;
                }
            }

            OnCodeAttempt?.Invoke("Código no reconocido.", false);
            return false;
        }
    }
}
