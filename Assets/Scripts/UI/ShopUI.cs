#pragma warning disable 0649
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UltimatePenaltyCup.Shop;
using UltimatePenaltyCup.Economy;
using UltimatePenaltyCup.Audio;

namespace UltimatePenaltyCup.UI
{
    public class ShopUI : MonoBehaviour
    {
        [Header("Tabs")]
        [SerializeField] private Button btnTabBalls;
        [SerializeField] private Button btnTabStadiums;
        [SerializeField] private GameObject panelBallsList;
        [SerializeField] private GameObject panelStadiumsList;

        [Header("Currency & Feedback")]
        [SerializeField] private TMP_Text txtCoins;
        [SerializeField] private TMP_Text txtFeedback;
        [SerializeField] private Button btnBack;
        [SerializeField] private MainMenuUI mainMenuUI;

        [Header("Card Container")]
        [SerializeField] private Transform ballsContainer;
        [SerializeField] private Transform stadiumsContainer;

        private void OnEnable()
        {
            UpdateCoins();
            SwitchTab(true, silent: true);
            RefreshAllCards();
        }

        private void Start()
        {
            if (btnTabBalls != null) btnTabBalls.onClick.AddListener(() => SwitchTab(true));
            if (btnTabStadiums != null) btnTabStadiums.onClick.AddListener(() => SwitchTab(false));

            if (btnBack != null)
            {
                btnBack.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick);
                    mainMenuUI?.ReturnToMainMenu();
                });
            }

            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OnBallPurchased += HandleBallPurchased;
                ShopManager.Instance.OnStadiumPurchased += HandleStadiumPurchased;
                ShopManager.Instance.OnPurchaseFailed += HandlePurchaseFailed;
            }

            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.OnCoinsChanged += HandleCoinsChanged;
            }
        }

        private void OnDestroy()
        {
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OnBallPurchased -= HandleBallPurchased;
                ShopManager.Instance.OnStadiumPurchased -= HandleStadiumPurchased;
                ShopManager.Instance.OnPurchaseFailed -= HandlePurchaseFailed;
            }
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.OnCoinsChanged -= HandleCoinsChanged;
            }
        }

        private void HandleBallPurchased(BallItemSO ball)
        {
            RefreshAllCards();
            string msg = $"¡Has desbloqueado {ball.displayName}!";
            ShowFeedback(msg, true);
            NotificationManager.EnsureInstance().ShowToast("COMPRA EXITOSA", msg, NotificationKind.Success);
        }

        private void HandleStadiumPurchased(StadiumItemSO stadium)
        {
            RefreshAllCards();
            string msg = $"¡Has desbloqueado {stadium.displayName}!";
            ShowFeedback(msg, true);
            NotificationManager.EnsureInstance().ShowToast("ESTADIO DESBLOQUEADO", msg, NotificationKind.Reward);
        }

        private void HandlePurchaseFailed(string err)
        {
            ShowFeedback(err, false);
            NotificationManager.EnsureInstance().ShowToast("COMPRA NO DISPONIBLE", err, NotificationKind.Warning);
        }
        private void HandleCoinsChanged(int c, int d) => UpdateCoins();

        public void SwitchTab(bool showBalls, bool silent = false)
        {
            if (!silent) AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick);
            if (panelBallsList != null) panelBallsList.SetActive(showBalls);
            if (panelStadiumsList != null) panelStadiumsList.SetActive(!showBalls);

            if (btnTabBalls != null)
                btnTabBalls.GetComponent<Image>().color = showBalls ? new Color(0.08f, 0.49f, 1.0f) : new Color(0.10f, 0.17f, 0.30f);
            if (btnTabStadiums != null)
                btnTabStadiums.GetComponent<Image>().color = !showBalls ? new Color(0.08f, 0.49f, 1.0f) : new Color(0.10f, 0.17f, 0.30f);

            RefreshAllCards();
        }

        public void UpdateCoins()
        {
            if (txtCoins != null)
            {
                int coins = CurrencyManager.Instance != null ? CurrencyManager.Instance.Coins : 0;
                txtCoins.text = $"{coins:N0}";
            }
        }

        public void RefreshAllCards()
        {
            UpdateCoins();
            var ballCards = ballsContainer != null ? ballsContainer.GetComponentsInChildren<ShopItemCardUI>(true) : Array.Empty<ShopItemCardUI>();
            foreach (var card in ballCards) card.Refresh();

            var stadiumCards = stadiumsContainer != null ? stadiumsContainer.GetComponentsInChildren<ShopItemCardUI>(true) : Array.Empty<ShopItemCardUI>();
            foreach (var card in stadiumCards) card.Refresh();
        }

        public void ShowFeedback(string message, bool success)
        {
            if (txtFeedback != null)
            {
                txtFeedback.gameObject.SetActive(true);
                txtFeedback.text = message;
                txtFeedback.color = success ? new Color(0.15f, 0.80f, 0.45f) : new Color(0.90f, 0.28f, 0.30f);
                CancelInvoke(nameof(HideFeedback));
                Invoke(nameof(HideFeedback), 2.5f);
            }
        }

        private void HideFeedback()
        {
            if (txtFeedback != null) txtFeedback.gameObject.SetActive(false);
        }
    }
}
