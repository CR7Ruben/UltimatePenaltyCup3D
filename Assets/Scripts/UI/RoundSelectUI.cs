#pragma warning disable 0649
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UltimatePenaltyCup.Core;
using UltimatePenaltyCup.Levels;
using UltimatePenaltyCup.SaveSystem;
using UltimatePenaltyCup.Audio;

namespace UltimatePenaltyCup.UI
{
    public class RoundSelectUI : MonoBehaviour
    {
        [System.Serializable]
        public struct RoundCard
        {
            public int roundNumber;
            public GameObject cardObject;
            public TMP_Text txtTitle;
            public TMP_Text txtDescription;
            public TMP_Text txtTarget;
            public TMP_Text txtRewards;
            public GameObject lockOverlay;
            public Button btnPlay;
        }

        [Header("Round Cards (1 to 5)")]
        [SerializeField] private RoundCard[] roundCards;

        [Header("Navigation")]
        [SerializeField] private Button btnBack;
        [SerializeField] private MainMenuUI mainMenuUI;

        private void OnEnable()
        {
            RefreshCards();
        }

        private void Start()
        {
            if (btnBack != null)
            {
                btnBack.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick);
                    mainMenuUI?.ReturnToMainMenu();
                });
            }

            SetupCardButtons();
            RefreshCards();
        }

        private void SetupCardButtons()
        {
            if (roundCards == null) return;

            foreach (var card in roundCards)
            {
                int rNum = card.roundNumber;
                if (card.btnPlay != null)
                {
                    card.btnPlay.onClick.RemoveAllListeners();
                    card.btnPlay.onClick.AddListener(() => OnPlayRoundClicked(rNum));
                }
            }
        }

        public void RefreshCards()
        {
            if (roundCards == null) return;

            foreach (var card in roundCards)
            {
                bool isUnlocked = SaveManager.Instance != null && SaveManager.Instance.IsRoundUnlocked(card.roundNumber);
                RoundDataSO data = GameManager.Instance != null ? GameManager.Instance.GetRoundData(card.roundNumber) : null;

                if (card.lockOverlay != null)
                {
                    card.lockOverlay.SetActive(!isUnlocked);
                }

                if (card.btnPlay != null)
                {
                    card.btnPlay.interactable = isUnlocked;
                }

                if (data != null)
                {
                    if (card.txtTitle != null) card.txtTitle.text = $"Ronda {data.roundNumber}: {data.roundName}";
                    if (card.txtDescription != null) card.txtDescription.text = data.description;
                    if (card.txtTarget != null) card.txtTarget.text = $"Objetivo: {data.requiredGoals} de {data.maxAttempts} goles";
                    if (card.txtRewards != null) card.txtRewards.text = $"+{data.winCoins} Monedas | +{data.winPoints} Pts";
                }
            }
        }

        private void OnPlayRoundClicked(int roundNumber)
        {
            AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick);
            GameManager.Instance?.SelectRoundAndStartMatch(roundNumber);
        }
    }
}
