#pragma warning disable 0649
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

        [Header("Top Broadcast Scoreboard")]
        [SerializeField] private TMP_Text txtRoundTitle;
        [SerializeField] private TMP_Text txtGoalsTarget;
        [SerializeField] private TMP_Text txtAttemptCounter;
        [SerializeField] private TMP_Text txtScore;
        [SerializeField] private TMP_Text txtCoins;
        [SerializeField] private TMP_Text txtStreak;

        [Header("Power Bar")]
        [SerializeField] private GameObject powerBarContainer;
        [SerializeField] private Image imgPowerFill;
        [SerializeField] private TMP_Text txtPowerHint;
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

        private static readonly Color ColorEmpty = new Color(0.28f, 0.33f, 0.42f, 0.9f);
        private static readonly Color ColorGoal  = new Color(0.15f, 0.78f, 0.44f, 1f);
        private static readonly Color ColorMiss  = new Color(0.90f, 0.28f, 0.30f, 1f);
        private static readonly Color ColorSaved = new Color(0.96f, 0.56f, 0.12f, 1f);

        private int currentAttemptIndex = 0;
        private Coroutine bannerCoroutine;

        private void Awake()
        {
            Instance = this;
            NotificationManager.EnsureInstance();
            InitializeGradient();
        }

        private void InitializeGradient()
        {
            if (powerGradient == null || powerGradient.colorKeys.Length == 0)
            {
                powerGradient = new Gradient();
                var colorKeys = new GradientColorKey[4];
                colorKeys[0] = new GradientColorKey(new Color(0.15f, 0.78f, 0.44f), 0.0f);  // Green
                colorKeys[1] = new GradientColorKey(new Color(1.00f, 0.78f, 0.24f), 0.75f); // Gold Sweet Spot
                colorKeys[2] = new GradientColorKey(new Color(1.00f, 0.55f, 0.10f), 0.90f); // Orange
                colorKeys[3] = new GradientColorKey(new Color(0.90f, 0.28f, 0.30f), 1.0f);  // Red overpower

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
                PlayerShooter.Instance.OnPowerUpdated += UpdatePowerBarDetailed;
            }

            if (PenaltyManager.Instance != null)
            {
                PenaltyManager.Instance.OnAttemptStarted += HandleAttemptStarted;
                PenaltyManager.Instance.OnAttemptFinished += HandleAttemptFinished;
                PenaltyManager.Instance.OnStatsUpdated += HandleStatsUpdated;
                PenaltyManager.Instance.OnRoundEndedDetailed += HandleRoundEnded;
            }

            if (powerBarContainer != null) powerBarContainer.SetActive(false);
            if (bannerResult != null) bannerResult.SetActive(false);
            if (txtStreak != null) txtStreak.gameObject.SetActive(false);

            currentAttemptIndex = 0;
            if (imgShotIndicators != null)
            {
                foreach (var img in imgShotIndicators)
                    if (img != null) img.color = ColorEmpty;
            }

            RefreshHeaderFromRoundData();
            ShowMessageBanner("¡TU TURNO!", "WASD: Mover  |  Mouse: Apuntar  |  Clic: Cargar potencia", new Color(0.06f, 0.22f, 0.46f, 0.94f), 2.2f);
        }

        private void OnDestroy()
        {
            if (PlayerShooter.Instance != null)
            {
                PlayerShooter.Instance.OnPowerUpdated -= UpdatePowerBarDetailed;
            }

            if (PenaltyManager.Instance != null)
            {
                PenaltyManager.Instance.OnAttemptStarted -= HandleAttemptStarted;
                PenaltyManager.Instance.OnAttemptFinished -= HandleAttemptFinished;
                PenaltyManager.Instance.OnStatsUpdated -= HandleStatsUpdated;
                PenaltyManager.Instance.OnRoundEndedDetailed -= HandleRoundEnded;
            }
        }

        private RoundDataSO GetActiveRoundData()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentRoundData != null)
                return GameManager.Instance.CurrentRoundData;
            int rNum = GameManager.Instance != null ? GameManager.Instance.CurrentSelectedRoundNumber : 1;
            return RoundManager.Instance != null ? RoundManager.Instance.GetRound(rNum) : null;
        }

        private void RefreshHeaderFromRoundData()
        {
            RoundDataSO data = GetActiveRoundData();
            if (txtRoundTitle != null)
            {
                txtRoundTitle.text = data != null ? $"RONDA {data.roundNumber}: {data.roundName.ToUpper()}" : "RONDA 1";
            }
        }

        private void HandleAttemptStarted(int attempt, int max)
        {
            RefreshHeaderFromRoundData();
            if (txtAttemptCounter != null)
            {
                txtAttemptCounter.text = $"Penal {attempt} / {max}";
            }
            if (attempt == max && PenaltyManager.Instance != null && !PenaltyManager.Instance.IsRoundOver && attempt > 1)
            {
                ShowMessageBanner("¡ÚLTIMO PENAL!", "Disparo decisivo de la ronda", new Color(0.84f, 0.48f, 0.08f, 0.94f), 1.45f);
            }
        }

        private void HandleStatsUpdated(int goals, int attemptsUsed, int matchScore)
        {
            RoundDataSO data = GetActiveRoundData();
            int req = data != null ? data.requiredGoals : (PenaltyManager.Instance != null ? PenaltyManager.Instance.RequiredGoals : 3);

            if (txtGoalsTarget != null)
            {
                txtGoalsTarget.text = $"Goles: {goals} / {req}";
                txtGoalsTarget.color = goals >= req ? ColorGoal : Color.white;
            }

            if (txtScore != null)
            {
                txtScore.text = $"{matchScore:N0} pts";
            }

            if (txtCoins != null)
            {
                int coins = CurrencyManager.Instance != null ? CurrencyManager.Instance.Coins : 0;
                txtCoins.text = $"{coins:N0}";
            }

            int streak = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentStreak : 0;
            if (txtStreak != null)
            {
                if (streak >= 2)
                {
                    txtStreak.gameObject.SetActive(true);
                    txtStreak.text = $"RACHA x{streak}!";
                }
                else
                {
                    txtStreak.gameObject.SetActive(false);
                }
            }
        }

        private void HandleAttemptFinished(ShotResult result, ShotScoreResult scoreResult, string msg)
        {
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

            Color bannerColor = new Color(0.10f, 0.16f, 0.28f, 0.95f);
            string subText = "";

            switch (result)
            {
                case ShotResult.GoalNormal:
                case ShotResult.GoalCorner:
                case ShotResult.GoalHighPower:
                    bannerColor = new Color(0.10f, 0.62f, 0.34f, 0.96f);
                    subText = $"+{scoreResult.pointsEarned} Pts  |  +{scoreResult.coinsEarned} Monedas";
                    break;
                case ShotResult.Saved:
                    bannerColor = new Color(0.84f, 0.44f, 0.08f, 0.96f);
                    subText = "Malágon desvió el disparo";
                    break;
                case ShotResult.PostHit:
                    bannerColor = new Color(0.88f, 0.56f, 0.12f, 0.96f);
                    subText = "¡El balón se estrelló en el metal!";
                    break;
                case ShotResult.Missed:
                    bannerColor = new Color(0.76f, 0.18f, 0.20f, 0.96f);
                    subText = "El disparo salió desviado";
                    break;
            }

            ShowMessageBanner(msg, subText, bannerColor, 1.75f);
        }

        private void HandleRoundEnded(RoundEndSummary summary)
        {
            if (powerBarContainer != null) powerBarContainer.SetActive(false);
            if (bannerResult != null) bannerResult.SetActive(false);
        }

        public void UpdatePowerBar(float normalizedPower)
        {
            UpdatePowerBarDetailed(normalizedPower, false, false);
        }

        public void UpdatePowerBarDetailed(float normalizedPower, bool isPerfect, bool isOverpower)
        {
            if (powerBarContainer == null || imgPowerFill == null) return;

            if (normalizedPower > 0.01f)
            {
                powerBarContainer.SetActive(true);
                imgPowerFill.fillAmount = normalizedPower;
                imgPowerFill.color = powerGradient.Evaluate(normalizedPower);
                if (txtPowerHint != null)
                {
                    txtPowerHint.text = isOverpower ? "¡EXCESO DE POTENCIA!" : (isPerfect ? "¡ZONA PERFECTA!" : "POTENCIA DE TIRO");
                    txtPowerHint.color = isOverpower ? ColorMiss : (isPerfect ? new Color(1f, 0.80f, 0.22f) : Color.white);
                }
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

            CanvasGroup cg = bannerResult.GetComponent<CanvasGroup>();
            if (cg == null) cg = bannerResult.AddComponent<CanvasGroup>();
            RectTransform rt = bannerResult.GetComponent<RectTransform>();
            cg.blocksRaycasts = false;

            float enterT = 0f;
            while (enterT < 0.18f)
            {
                enterT += Time.unscaledDeltaTime;
                float u = 1f - Mathf.Pow(1f - Mathf.Clamp01(enterT / 0.18f), 3f);
                cg.alpha = u;
                float punch = Mathf.Lerp(0.84f, 1.04f, Mathf.Clamp01(u * 1.15f));
                if (u > 0.85f) punch = Mathf.Lerp(1.04f, 1.0f, (u - 0.85f) / 0.15f);
                if (rt != null) rt.localScale = new Vector3(punch, punch, 1f);
                yield return null;
            }
            cg.alpha = 1f;
            if (rt != null) rt.localScale = Vector3.one;

            yield return new WaitForSecondsRealtime(Mathf.Max(0.25f, duration - 0.30f));

            float exitT = 0f;
            while (exitT < 0.16f)
            {
                exitT += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(exitT / 0.16f);
                cg.alpha = 1f - u;
                if (rt != null) rt.localScale = Vector3.one * Mathf.Lerp(1f, 0.94f, u);
                yield return null;
            }

            bannerResult.SetActive(false);
        }
    }
}
