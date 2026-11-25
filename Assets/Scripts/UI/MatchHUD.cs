#pragma warning disable 0649
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UltimatePenaltyCup.Core;
using UltimatePenaltyCup.Levels;
using UltimatePenaltyCup.Gameplay;
using UltimatePenaltyCup.Economy;
using UltimatePenaltyCup.Player;
using UltimatePenaltyCup.Audio;

namespace UltimatePenaltyCup.UI
{
    public class MatchHUD : MonoBehaviour
    {
        public static MatchHUD Instance { get; private set; }

        [Header("Top Info Bar")]
        [SerializeField] private TMP_Text txtRoundTitle;
        [SerializeField] private TMP_Text txtGoalsTarget;
        [SerializeField] private TMP_Text txtAttemptCounter;
        [SerializeField] private TMP_Text txtScore;
        [SerializeField] private TMP_Text txtCoins;
        [SerializeField] private TMP_Text txtStreak;

        [Header("Power Bar")]
        [SerializeField] private GameObject powerBarContainer;
        [SerializeField] private Image imgPowerFill;
        [SerializeField] private Gradient powerGradient;

        [Header("Result Banner")]
        [SerializeField] private GameObject bannerResult;
        [SerializeField] private TMP_Text txtResultTitle;
        [SerializeField] private TMP_Text txtResultSub;
        [SerializeField] private Image imgBannerBg;

        [Header("Pause")]
        [SerializeField] private Button btnPause;
        [SerializeField] private PauseUI pauseUI;

        [Header("Shot Attempt Indicators")]
        [SerializeField] private Image[] imgShotIndicators; // 5 circles

        private static readonly Color ColorEmpty = new Color(0.3f, 0.3f, 0.35f, 0.9f);
        private static readonly Color ColorGoal  = new Color(0.15f, 0.80f, 0.3f, 1f);
        private static readonly Color ColorMiss  = new Color(0.85f, 0.2f, 0.2f, 1f);
        private static readonly Color ColorSaved = new Color(0.9f, 0.5f, 0.1f, 1f);

        private int currentAttemptIndex = 0;
        private Coroutine bannerCoroutine;

        private void Awake()
        {
            Instance = this;
            InitializeGradient();
        }

        private void InitializeGradient()
        {
            if (powerGradient == null || powerGradient.colorKeys.Length == 0)
            {
                powerGradient = new Gradient();
                var colorKeys = new GradientColorKey[3];
                colorKeys[0] = new GradientColorKey(new Color(0.2f, 0.9f, 0.3f), 0.0f); // Green
                colorKeys[1] = new GradientColorKey(new Color(1.0f, 0.85f, 0.0f), 0.6f); // Yellow
                colorKeys[2] = new GradientColorKey(new Color(1.0f, 0.2f, 0.2f), 1.0f); // Red

                var alphaKeys = new GradientAlphaKey[2];
                alphaKeys[0] = new GradientAlphaKey(1.0f, 0.0f);
                alphaKeys[1] = new GradientAlphaKey(1.0f, 1.0f);

                powerGradient.SetKeys(colorKeys, alphaKeys);
            }
        }

        private void Start()
        {
            if (btnPause != null)
            {
                btnPause.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlaySfx(SfxType.S06_ButtonClick);
                    pauseUI?.TogglePause();
                });
            }

            if (PlayerShooter.Instance != null)
            {
                PlayerShooter.Instance.OnPowerChanged += UpdatePowerBar;
            }

            if (PenaltyManager.Instance != null)
            {
                PenaltyManager.Instance.OnAttemptStarted += HandleAttemptStarted;
                PenaltyManager.Instance.OnAttemptFinished += HandleAttemptFinished;
                PenaltyManager.Instance.OnStatsUpdated += HandleStatsUpdated;
            }

            if (powerBarContainer != null) powerBarContainer.SetActive(false);
            if (bannerResult != null) bannerResult.SetActive(false);
            if (txtStreak != null) txtStreak.gameObject.SetActive(false);

            // Reset shot indicator circles to empty
            currentAttemptIndex = 0;
            if (imgShotIndicators != null)
                foreach (var img in imgShotIndicators)
                    if (img != null) img.color = ColorEmpty;

            ShowMessageBanner("¡Prepárate para disparar!", "Apunta con el mouse y mantén clic para cargar potencia", new Color(0.1f, 0.4f, 0.85f, 0.9f), 2.2f);
        }

        private void OnDestroy()
        {
            if (PlayerShooter.Instance != null)
            {
                PlayerShooter.Instance.OnPowerChanged -= UpdatePowerBar;
            }

            if (PenaltyManager.Instance != null)
            {
                PenaltyManager.Instance.OnAttemptStarted -= HandleAttemptStarted;
                PenaltyManager.Instance.OnAttemptFinished -= HandleAttemptFinished;
                PenaltyManager.Instance.OnStatsUpdated -= HandleStatsUpdated;
            }
        }

        private void HandleAttemptStarted(int attempt, int max)
        {
            if (txtAttemptCounter != null)
            {
                txtAttemptCounter.text = $"Intento {attempt} / {max}";
            }

            RoundDataSO data = GameManager.Instance != null ? GameManager.Instance.CurrentRoundData : null;
            if (txtRoundTitle != null)
            {
                txtRoundTitle.text = data != null ? $"Ronda {data.roundNumber}: {data.roundName.ToUpper()}" : "TIROS PENALES";
            }
        }

        private void HandleStatsUpdated(int goals, int attemptsUsed, int matchScore)
        {
            RoundDataSO data = GameManager.Instance != null ? GameManager.Instance.CurrentRoundData : null;
            int req = data != null ? data.requiredGoals : 3;

            if (txtGoalsTarget != null)
            {
                txtGoalsTarget.text = $"Goles: {goals} / {req}";
                txtGoalsTarget.color = goals >= req ? Color.green : Color.white;
            }

            if (txtScore != null)
            {
                txtScore.text = $"Puntos: {matchScore:N0}";
            }

            if (txtCoins != null)
            {
                int coins = CurrencyManager.Instance != null ? CurrencyManager.Instance.Coins : 0;
                txtCoins.text = $"Monedas: {coins:N0}";
            }

            int streak = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentStreak : 0;
            if (txtStreak != null)
            {
                if (streak > 1)
                {
                    txtStreak.gameObject.SetActive(true);
                    txtStreak.text = $"¡Racha x{streak}!";
                }
                else
                {
                    txtStreak.gameObject.SetActive(false);
                }
            }
        }

        private void HandleAttemptFinished(ShotResult result, ShotScoreResult scoreResult, string msg)
        {
            // Update shot indicator circle
            if (imgShotIndicators != null && currentAttemptIndex < imgShotIndicators.Length)
            {
                Color indicatorColor = result switch
                {
                    ShotResult.GoalNormal    => ColorGoal,
                    ShotResult.GoalCorner    => ColorGoal,
                    ShotResult.GoalHighPower => ColorGoal,
                    ShotResult.Saved         => ColorSaved,
                    _                        => ColorMiss
                };
                if (imgShotIndicators[currentAttemptIndex] != null)
                    imgShotIndicators[currentAttemptIndex].color = indicatorColor;
                currentAttemptIndex++;
            }
            Color bannerColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            string subText = "";

            switch (result)
            {
                case ShotResult.GoalNormal:
                case ShotResult.GoalCorner:
                case ShotResult.GoalHighPower:
                    bannerColor = new Color(0.1f, 0.65f, 0.25f, 0.95f);
                    subText = $"+{scoreResult.pointsEarned} Pts  |  +{scoreResult.coinsEarned} Monedas";
                    break;
                case ShotResult.Saved:
                    bannerColor = new Color(0.85f, 0.4f, 0.1f, 0.95f);
                    subText = "¡Gran estirada del arquero!";
                    break;
                case ShotResult.Missed:
                    bannerColor = new Color(0.8f, 0.15f, 0.15f, 0.95f);
                    subText = "El tiro se fue desviado";
                    break;
            }

            ShowMessageBanner(msg, subText, bannerColor, 2.0f);
        }

        public void UpdatePowerBar(float normalizedPower)
        {
            if (powerBarContainer == null || imgPowerFill == null) return;

            if (normalizedPower > 0.01f)
            {
                powerBarContainer.SetActive(true);
                imgPowerFill.fillAmount = normalizedPower;
                imgPowerFill.color = powerGradient.Evaluate(normalizedPower);
            }
            else
            {
                powerBarContainer.SetActive(false);
            }
        }

        public void ShowMessageBanner(string title, string sub, Color bgColor, float duration)
        {
            if (bannerResult == null) return;

            if (bannerCoroutine != null) StopCoroutine(bannerCoroutine);
            bannerCoroutine = StartCoroutine(BannerDisplayRoutine(title, sub, bgColor, duration));
        }

        private IEnumerator BannerDisplayRoutine(string title, string sub, Color bgColor, float duration)
        {
            bannerResult.SetActive(true);
            if (txtResultTitle != null) txtResultTitle.text = title;
            if (txtResultSub != null) txtResultSub.text = sub;
            if (imgBannerBg != null) imgBannerBg.color = bgColor;

            yield return new WaitForSeconds(duration);

            bannerResult.SetActive(false);
        }
    }
}
