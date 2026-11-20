#pragma warning disable 0649
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UltimatePenaltyCup.Core;
using UltimatePenaltyCup.Gameplay;
using UltimatePenaltyCup.Audio;
using UltimatePenaltyCup.Economy;

namespace UltimatePenaltyCup.UI
{
    public class RoundEndUI : MonoBehaviour
    {
        [Header("Main Panels")]
        [SerializeField] private GameObject panelEndContainer;
        [SerializeField] private GameObject panelNormalResult;
        [SerializeField] private GameObject panelGrandChampionCelebration;

        [Header("Normal Result Texts")]
        [SerializeField] private TMP_Text txtStatusTitle;
        [SerializeField] private TMP_Text txtStatusSub;
        [SerializeField] private TMP_Text txtGoals;
        [SerializeField] private TMP_Text txtScore;
        [SerializeField] private TMP_Text txtCoinsGained;

        [Header("Buttons")]
        [SerializeField] private Button btnNextRound;
        [SerializeField] private Button btnRetry;
        [SerializeField] private Button btnMainMenu;

        [Header("Grand Champion View (Round 5 Win)")]
        [SerializeField] private TMP_Text txtChampionTitle;
        [SerializeField] private TMP_Text txtChampionScore;
        [SerializeField] private Button btnChampionContinue;
        [SerializeField] private ParticleSystem confettiParticles;

        private void Start()
        {
            if (PenaltyManager.Instance != null)
            {
                PenaltyManager.Instance.OnRoundEnded += ShowRoundEnd;
            }

            if (btnNextRound != null) btnNextRound.onClick.AddListener(OnNextRoundClicked);
            if (btnRetry != null) btnRetry.onClick.AddListener(OnRetryClicked);
            if (btnMainMenu != null) btnMainMenu.onClick.AddListener(OnMainMenuClicked);
            if (btnChampionContinue != null) btnChampionContinue.onClick.AddListener(OnMainMenuClicked);

            if (panelEndContainer != null) panelEndContainer.SetActive(false);
            if (panelGrandChampionCelebration != null) panelGrandChampionCelebration.SetActive(false);
        }

        private void OnDestroy()
        {
            if (PenaltyManager.Instance != null)
            {
                PenaltyManager.Instance.OnRoundEnded -= ShowRoundEnd;
            }
        }

        public void ShowRoundEnd(bool isVictory, int goalsScored, int requiredGoals, int finalScore)
        {
            if (panelEndContainer != null) panelEndContainer.SetActive(true);

            int currentRound = GameManager.Instance != null ? GameManager.Instance.CurrentSelectedRoundNumber : 1;
            bool isGrandFinalWon = isVictory && currentRound == 5;

            if (isGrandFinalWon)
            {
                // Grand Champion Celebration
                if (panelNormalResult != null) panelNormalResult.SetActive(false);
                if (panelGrandChampionCelebration != null) panelGrandChampionCelebration.SetActive(true);

                if (txtChampionTitle != null)
                {
                    txtChampionTitle.text = "¡ERES EL CAMPEÓN DE ULTIMATE PENALTY CUP!";
                }

                if (txtChampionScore != null)
                {
                    txtChampionScore.text = $"Puntuación de Campeonato: {finalScore:N0} pts\n¡Felicidades Rubén González!";
                }

                if (confettiParticles != null)
                {
                    confettiParticles.Play();
                }

                AudioManager.Instance?.PlayBgm(BgmType.M03_Victory);
            }
            else
            {
                // Standard Victory or Defeat
                if (panelNormalResult != null) panelNormalResult.SetActive(true);
                if (panelGrandChampionCelebration != null) panelGrandChampionCelebration.SetActive(false);

                if (isVictory)
                {
                    if (txtStatusTitle != null)
                    {
                        txtStatusTitle.text = "¡RONDA SUPERADA!";
                        txtStatusTitle.color = new Color(0.2f, 0.9f, 0.35f);
                    }
                    if (txtStatusSub != null) txtStatusSub.text = "¡Excelente precisión y temple!";
                    AudioManager.Instance?.PlayBgm(BgmType.M03_Victory);
                }
                else
                {
                    if (txtStatusTitle != null)
                    {
                        txtStatusTitle.text = "NO FUE SUFICIENTE";
                        txtStatusTitle.color = new Color(0.95f, 0.3f, 0.25f);
                    }
                    if (txtStatusSub != null) txtStatusSub.text = "¡Inténtalo de nuevo para clasificar!";
                    AudioManager.Instance?.PlayBgm(BgmType.M04_Defeat);
                }

                if (txtGoals != null) txtGoals.text = $"Goles: {goalsScored} de 5 (Requeridos: {requiredGoals})";
                if (txtScore != null) txtScore.text = $"Puntuación: {finalScore:N0} pts";
                if (txtCoinsGained != null)
                {
                    int matchCoins = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentMatchCoins : 0;
                    txtCoinsGained.text = $"Monedas Ganadas: +{matchCoins:N0}";
                }

                if (btnNextRound != null)
                {
                    // Next round button only active on victory when not round 5
                    btnNextRound.gameObject.SetActive(isVictory && currentRound < 5);
                }
            }
        }

        private void OnNextRoundClicked()
        {
            AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick);
            int currentRound = GameManager.Instance != null ? GameManager.Instance.CurrentSelectedRoundNumber : 1;
            GameManager.Instance?.SelectRoundAndStartMatch(currentRound + 1);
        }

        private void OnRetryClicked()
        {
            AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick);
            int currentRound = GameManager.Instance != null ? GameManager.Instance.CurrentSelectedRoundNumber : 1;
            GameManager.Instance?.SelectRoundAndStartMatch(currentRound);
        }

        private void OnMainMenuClicked()
        {
            AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick);
            GameManager.Instance?.LoadMainMenuScene();
        }
    }
}
