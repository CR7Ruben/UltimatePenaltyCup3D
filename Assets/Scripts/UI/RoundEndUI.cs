#pragma warning disable 0649
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UltimatePenaltyCup.Core;
using UltimatePenaltyCup.Gameplay;
using UltimatePenaltyCup.Audio;
using UltimatePenaltyCup.Economy;

namespace UltimatePenaltyCup.UI
{
    /// <summary>
    /// Lives on an ALWAYS-ACTIVE object on Canvas_MatchHUD so OnRoundEndedDetailed ALWAYS executes.
    /// Toggles panelEndContainer visible upon round completion.
    /// </summary>
    public class RoundEndUI : MonoBehaviour
    {
        [Header("Main Panels")]
        [SerializeField] private GameObject panelEndContainer;
        [SerializeField] private GameObject panelNormalResult;
        [SerializeField] private GameObject panelGrandChampionCelebration;

        [Header("Normal Result Elements")]
        [SerializeField] private TMP_Text txtStatusTitle;
        [SerializeField] private TMP_Text txtStatusSub;
        [SerializeField] private TMP_Text txtRoundInfo;
        [SerializeField] private TMP_Text txtGoals;
        [SerializeField] private TMP_Text txtAttempts;
        [SerializeField] private TMP_Text txtScore;
        [SerializeField] private TMP_Text txtCoinsGained;
        [SerializeField] private TMP_Text txtBonusDetails;
        [SerializeField] private GameObject badgeNewRecord;
        [SerializeField] private Image imgVictoryTrophy;

        [Header("Normal Buttons")]
        [SerializeField] private Button btnNextRound;
        [SerializeField] private Button btnRetry;
        [SerializeField] private TMP_Text txtBtnRetry;
        [SerializeField] private Button btnMainMenu;

        [Header("Grand Champion View (Round 5 Win)")]
        [SerializeField] private TMP_Text txtChampionTitle;
        [SerializeField] private TMP_Text txtChampionScore;
        [SerializeField] private TMP_Text txtChampionStats;
        [SerializeField] private TMP_Text txtChampionAchievements;
        [SerializeField] private Button btnChampionPlayAgain;
        [SerializeField] private Button btnChampionAchievements;
        [SerializeField] private Button btnChampionMainMenu;
        [SerializeField] private ParticleSystem confettiParticles;

        private void Awake()
        {
            if (panelEndContainer != null) panelEndContainer.SetActive(false);
            if (panelGrandChampionCelebration != null) panelGrandChampionCelebration.SetActive(false);
        }

        private void Start()
        {
            if (PenaltyManager.Instance != null)
            {
                PenaltyManager.Instance.OnRoundEndedDetailed += ShowRoundEndDetailed;
            }

            if (btnNextRound != null) btnNextRound.onClick.AddListener(OnNextRoundClicked);
            if (btnRetry != null) btnRetry.onClick.AddListener(OnRetryClicked);
            if (btnMainMenu != null) btnMainMenu.onClick.AddListener(OnMainMenuClicked);

            if (btnChampionPlayAgain != null) btnChampionPlayAgain.onClick.AddListener(OnPlayAgainFromRound1Clicked);
            if (btnChampionAchievements != null) btnChampionAchievements.onClick.AddListener(OnViewAchievementsClicked);
            if (btnChampionMainMenu != null) btnChampionMainMenu.onClick.AddListener(OnMainMenuClicked);
        }

        private void OnDestroy()
        {
            if (PenaltyManager.Instance != null)
            {
                PenaltyManager.Instance.OnRoundEndedDetailed -= ShowRoundEndDetailed;
            }
        }

        public void ShowRoundEndDetailed(RoundEndSummary summary)
        {
            if (panelEndContainer != null) panelEndContainer.SetActive(true);

            if (summary.isGrandFinal)
            {
                if (panelNormalResult != null) panelNormalResult.SetActive(false);
                if (panelGrandChampionCelebration != null) panelGrandChampionCelebration.SetActive(true);

                if (txtChampionTitle != null)
                    txtChampionTitle.text = "¡CAMPEÓN DE ULTIMATE PENALTY CUP 3D!";

                if (txtChampionScore != null)
                    txtChampionScore.text = $"Puntuación Final: {summary.matchScore:N0} pts  |  +{summary.matchCoins:N0} Monedas";

                if (txtChampionStats != null)
                {
                    txtChampionStats.text =
                        $"Goles en Final: {summary.goalsScored} en {summary.attemptsUsed} intentos  •  " +
                        $"Bono Campeonato: +{summary.victoryBonusCoins:N0} Monedas";
                }

                if (txtChampionAchievements != null)
                {
                    txtChampionAchievements.text = (summary.unlockedAchievements != null && summary.unlockedAchievements.Length > 0)
                        ? "Logros desbloqueados: " + string.Join(", ", summary.unlockedAchievements)
                        : "¡Has conquistado las 5 rondas del torneo!";
                }

                if (confettiParticles != null) confettiParticles.Play();
                AudioManager.Instance?.PlayBgm(BgmType.M03_Victory);
                return;
            }

            if (panelNormalResult != null) panelNormalResult.SetActive(true);
            if (panelGrandChampionCelebration != null) panelGrandChampionCelebration.SetActive(false);

            if (summary.isVictory)
            {
                if (txtStatusTitle != null)
                {
                    txtStatusTitle.text = "¡RONDA SUPERADA!";
                    txtStatusTitle.color = new Color(0.15f, 0.80f, 0.45f);
                }
                if (txtStatusSub != null) txtStatusSub.text = "¡Clasificación asegurada!";
                if (txtBtnRetry != null) txtBtnRetry.text = "REPETIR RONDA";
                AudioManager.Instance?.PlayBgm(BgmType.M03_Victory);
            }
            else
            {
                if (txtStatusTitle != null)
                {
                    txtStatusTitle.text = "RONDA NO SUPERADA";
                    txtStatusTitle.color = new Color(0.90f, 0.28f, 0.30f);
                }
                if (txtStatusSub != null) txtStatusSub.text = "No se alcanzó el objetivo de goles.";
                if (txtBtnRetry != null) txtBtnRetry.text = "REINTENTAR";
                AudioManager.Instance?.PlayBgm(BgmType.M04_Defeat);
            }

            if (txtRoundInfo != null) txtRoundInfo.text = $"Ronda {summary.roundNumber}: {summary.roundName}";
            if (txtGoals != null) txtGoals.text = $"Goles: {summary.goalsScored} / {summary.requiredGoals} requeridos";
            if (txtAttempts != null) txtAttempts.text = $"Intentos utilizados: {summary.attemptsUsed} de {summary.maxAttempts}";
            if (txtScore != null) txtScore.text = $"Puntuación: {summary.matchScore:N0} pts";
            if (txtCoinsGained != null) txtCoinsGained.text = $"Monedas ganadas: +{summary.matchCoins:N0}";
            if (gameObject.activeInHierarchy) StartCoroutine(AnimateSummaryCountersRoutine(summary.matchScore, summary.matchCoins));
            if (txtBonusDetails != null)
            {
                txtBonusDetails.text = summary.isVictory
                    ? $"Bono de ronda: +{summary.victoryBonusPoints} pts  |  +{summary.victoryBonusCoins} monedas"
                    : "Supera la meta para desbloquear la siguiente ronda";
            }

            if (badgeNewRecord != null) badgeNewRecord.SetActive(summary.isNewRecord);
            if (imgVictoryTrophy != null) imgVictoryTrophy.gameObject.SetActive(summary.isVictory);
            if (btnNextRound != null) btnNextRound.gameObject.SetActive(summary.isVictory && summary.roundNumber < 5);
        }

        private System.Collections.IEnumerator AnimateSummaryCountersRoutine(int targetScore, int targetCoins)
        {
            float elapsed = 0f;
            const float dur = 0.50f;
            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                float u = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / dur), 3f);
                if (txtScore != null) txtScore.text = $"Puntuación: {Mathf.RoundToInt(targetScore * u):N0} pts";
                if (txtCoinsGained != null) txtCoinsGained.text = $"Monedas ganadas: +{Mathf.RoundToInt(targetCoins * u):N0}";
                yield return null;
            }
            if (txtScore != null) txtScore.text = $"Puntuación: {targetScore:N0} pts";
            if (txtCoinsGained != null) txtCoinsGained.text = $"Monedas ganadas: +{targetCoins:N0}";
        }

        private void OnNextRoundClicked()
        {
            AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick);
            int currentRound = GameManager.Instance != null ? GameManager.Instance.CurrentSelectedRoundNumber : 1;
            int next = Mathf.Clamp(currentRound + 1, 1, 5);
            GameManager.Instance?.SelectRoundAndStartMatch(next);
        }

        private void OnRetryClicked()
        {
            AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick);
            int currentRound = GameManager.Instance != null ? GameManager.Instance.CurrentSelectedRoundNumber : 1;
            GameManager.Instance?.SelectRoundAndStartMatch(currentRound);
        }

        private void OnPlayAgainFromRound1Clicked()
        {
            AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick);
            GameManager.Instance?.SelectRoundAndStartMatch(1);
        }

        private void OnViewAchievementsClicked()
        {
            AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RequestOpenAchievementsOnMenu = true;
                GameManager.Instance.LoadMainMenuScene();
            }
        }

        private void OnMainMenuClicked()
        {
            AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick);
            GameManager.Instance?.LoadMainMenuScene();
        }
    }
}
