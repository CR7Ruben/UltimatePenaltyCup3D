#pragma warning disable 0649
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UltimatePenaltyCup.Core;
using UltimatePenaltyCup.Shop;
using UltimatePenaltyCup.SaveSystem;
using UltimatePenaltyCup.Economy;
using UltimatePenaltyCup.Customization;
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
            SwitchTab(true);
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
                ShopManager.Instance.OnBallPurchased += (ball) => { RefreshAllCards(); ShowFeedback($"¡Has comprado {ball.displayName}!", true); };
                ShopManager.Instance.OnStadiumPurchased += (stadium) => { RefreshAllCards(); ShowFeedback($"¡Has comprado {stadium.displayName}!", true); };
                ShopManager.Instance.OnPurchaseFailed += (err) => ShowFeedback(err, false);
            }

            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.OnCoinsChanged += (c, d) => UpdateCoins();
            }
        }

        public void SwitchTab(bool showBalls)
        {
            AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick);
            if (panelBallsList != null) panelBallsList.SetActive(showBalls);
            if (panelStadiumsList != null) panelStadiumsList.SetActive(!showBalls);
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
            // Refreshes UI cards for balls and stadiums
            var ballCards = ballsContainer != null ? ballsContainer.GetComponentsInChildren<ShopItemCardUI>(true) : Array.Empty<ShopItemCardUI>();
            foreach (var card in ballCards)
            {
                card.Refresh();
            }

            var stadiumCards = stadiumsContainer != null ? stadiumsContainer.GetComponentsInChildren<ShopItemCardUI>(true) : Array.Empty<ShopItemCardUI>();
            foreach (var card in stadiumCards)
            {
                card.Refresh();
            }
        }

        public void ShowFeedback(string message, bool success)
        {
            if (txtFeedback != null)
            {
                txtFeedback.gameObject.SetActive(true);
                txtFeedback.text = message;
                txtFeedback.color = success ? Color.green : new Color(1f, 0.35f, 0.35f);
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
