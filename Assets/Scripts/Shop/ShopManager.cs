using System;
using System.Collections.Generic;
using UnityEngine;
using UltimatePenaltyCup.Core;
using UltimatePenaltyCup.Economy;
using UltimatePenaltyCup.SaveSystem;
using UltimatePenaltyCup.Audio;

namespace UltimatePenaltyCup.Shop
{
    public class ShopManager : MonoBehaviour
    {
        public static ShopManager Instance { get; private set; }

        [Header("Catalog Assets")]
        [SerializeField] private List<BallItemSO> ballCatalog = new List<BallItemSO>();
        [SerializeField] private List<StadiumItemSO> stadiumCatalog = new List<StadiumItemSO>();

        public event Action<BallItemSO> OnBallPurchased;
        public event Action<StadiumItemSO> OnStadiumPurchased;
        public event Action<string> OnPurchaseFailed;

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

        public void SetCatalogs(List<BallItemSO> balls, List<StadiumItemSO> stadiums)
        {
            ballCatalog = balls;
            stadiumCatalog = stadiums;
        }

        public List<BallItemSO> GetBallCatalog() => ballCatalog;
        public List<StadiumItemSO> GetStadiumCatalog() => stadiumCatalog;

        public BallItemSO GetBallItem(BallType type)
        {
            return ballCatalog.Find(b => b.ballType == type);
        }

        public StadiumItemSO GetStadiumItem(StadiumType type)
        {
            return stadiumCatalog.Find(s => s.stadiumType == type);
        }

        public bool BuyBall(BallType type)
        {
            if (SaveManager.Instance.IsBallUnlocked(type))
            {
                OnPurchaseFailed?.Invoke("Este balón ya ha sido adquirido.");
                return false;
            }

            BallItemSO item = GetBallItem(type);
            if (item == null)
            {
                OnPurchaseFailed?.Invoke("Artículo no encontrado.");
                return false;
            }

            if (!CurrencyManager.Instance.SpendCoins(item.price))
            {
                OnPurchaseFailed?.Invoke("Monedas insuficientes.");
                return false;
            }

            SaveManager.Instance.UnlockBall(type);
            AudioManager.Instance?.PlaySfx(SfxType.S04_Purchase);
            OnBallPurchased?.Invoke(item);
            Debug.Log($"[ShopManager] Balón comprado con éxito: {item.displayName}");
            return true;
        }

        public bool BuyStadium(StadiumType type)
        {
            if (SaveManager.Instance.IsStadiumUnlocked(type))
            {
                OnPurchaseFailed?.Invoke("Este estadio ya ha sido adquirido.");
                return false;
            }

            StadiumItemSO item = GetStadiumItem(type);
            if (item == null)
            {
                OnPurchaseFailed?.Invoke("Estadio no encontrado.");
                return false;
            }

            if (!CurrencyManager.Instance.SpendCoins(item.price))
            {
                OnPurchaseFailed?.Invoke("Monedas insuficientes.");
                return false;
            }

            SaveManager.Instance.UnlockStadium(type);
            AudioManager.Instance?.PlaySfx(SfxType.S04_Purchase);
            OnStadiumPurchased?.Invoke(item);
            Debug.Log($"[ShopManager] Estadio comprado con éxito: {item.displayName}");
            return true;
        }
    }
}
