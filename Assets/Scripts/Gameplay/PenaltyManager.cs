#pragma warning disable 0649
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UltimatePenaltyCup.Core;
using UltimatePenaltyCup.Economy;
using UltimatePenaltyCup.Levels;
using UltimatePenaltyCup.Player;
using UltimatePenaltyCup.Goalkeeper;
using UltimatePenaltyCup.SaveSystem;
using UltimatePenaltyCup.Achievements;
using UltimatePenaltyCup.Audio;

namespace UltimatePenaltyCup.Gameplay
{
    public struct RoundEndSummary
    {
        public bool isVictory;
        public bool isGrandFinal;
        public int roundNumber;
        public string roundName;
        public int goalsScored;
        public int requiredGoals;
        public int attemptsUsed;
        public int maxAttempts;
        public int matchScore;
        public int matchCoins;
        public int victoryBonusCoins;
        public int victoryBonusPoints;
        public bool isNewRecord;
        public string[] unlockedAchievements;
    }

    public class PenaltyManager : MonoBehaviour
    {
        public static PenaltyManager Instance { get; private set; }

        [Header("Rules")]
        [SerializeField] private int maxAttempts = 5;
        [SerializeField] private int requiredGoals = 3;

        [Header("Positions")]
        [SerializeField] private Vector3 ballSpotPosition = new Vector3(0f, 0.11f, -11f);

        public int CurrentAttempt { get; private set; } = 1;
        public int AttemptsUsed { get; private set; } = 0;
        public int GoalsScored { get; private set; } = 0;
        public int RequiredGoals => requiredGoals;
        public int MaxAttempts => maxAttempts;

        public MatchAttemptState CurrentState { get; private set; } = MatchAttemptState.Aiming;
        public bool IsEvaluatingShot { get; private set; } = false;
        public bool IsRoundOver { get; private set; } = false;
        public bool CanAcceptShotInput => !IsRoundOver && !IsEvaluatingShot && CurrentState == MatchAttemptState.Aiming && Time.timeScale > 0f;

        /// <summary>
        /// Configurable resolution delays (used by automated Play Mode test benchmarks without altering physics).
        /// </summary>
        public float PostAttemptDelay { get; set; } = 1.60f;
        public float SaveConfirmDelay { get; set; } = 0.22f;
        public float ShotTimeoutDuration { get; set; } = 2.50f;

        private bool hasEvaluatedCurrentShot = false;
        private bool hasFinishedRound = false;
        private bool lastShotWasHighPower = false;
        private bool lastShotHitPost = false;
        private Coroutine shotTimeoutCoroutine;
        private Coroutine saveConfirmCoroutine;
        private Coroutine postAttemptCoroutine;
        private readonly WaitForSeconds waitPollStep = new WaitForSeconds(0.08f);
        private readonly List<string> matchUnlockedAchievements = new List<string>();

        public event Action<int, int> OnAttemptStarted; // (attempt, max)
        public event Action<ShotResult, ShotScoreResult, string> OnAttemptFinished; // result, scoreInfo, message
        public event Action<int, int, int> OnStatsUpdated; // (goals, attemptsUsed, score)
        public event Action<bool, int, int, int> OnRoundEnded; // (isVictory, goalsScored, requiredGoals, finalScore)
        public event Action<RoundEndSummary> OnRoundEndedDetailed;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (AchievementManager.Instance != null)
            {
                AchievementManager.Instance.OnAchievementUnlocked += HandleMatchAchievementUnlocked;
            }
            InitializeRound();
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            if (GoalDetector.Instance != null)
            {
                GoalDetector.Instance.OnGoalScored -= HandleGoalDetected;
            }
            if (AchievementManager.Instance != null)
            {
                AchievementManager.Instance.OnAchievementUnlocked -= HandleMatchAchievementUnlocked;
            }
        }

        private void HandleMatchAchievementUnlocked(AchievementSO ach)
        {
            if (ach != null && !matchUnlockedAchievements.Contains(ach.title))
            {
                matchUnlockedAchievements.Add(ach.title);
            }
        }

        public void InitializeRound()
        {
            StopAllCoroutines();
            matchUnlockedAchievements.Clear();

            RoundDataSO data = GameManager.Instance != null ? GameManager.Instance.CurrentRoundData : null;
            if (data == null && RoundManager.Instance != null)
            {
                int rNum = GameManager.Instance != null ? GameManager.Instance.CurrentSelectedRoundNumber : 1;
                data = RoundManager.Instance.GetRound(rNum);
            }

            if (data != null)
            {
                requiredGoals = data.requiredGoals;
                maxAttempts = data.maxAttempts;
                GoalkeeperAI.Instance?.Configure(data);
            }
            else
            {
                requiredGoals = 3;
                maxAttempts = 5;
            }
            StadiumController.Instance?.ApplyForRound(data);

            CurrentAttempt = 1;
            AttemptsUsed = 0;
            GoalsScored = 0;
            hasEvaluatedCurrentShot = false;
            hasFinishedRound = false;
            IsRoundOver = false;
            IsEvaluatingShot = false;
            CurrentState = MatchAttemptState.Aiming;

            if (SaveManager.Instance != null && SaveManager.Instance.CurrentData != null)
            {
                SaveManager.Instance.CurrentData.totalMatchesPlayed++;
                SaveManager.Instance.Save(flushToDisk: false);
            }

            ScoreManager.Instance?.ResetMatchScore();
            OnStatsUpdated?.Invoke(GoalsScored, AttemptsUsed, 0);

            if (GoalDetector.Instance != null)
            {
                GoalDetector.Instance.OnGoalScored -= HandleGoalDetected;
                GoalDetector.Instance.OnGoalScored += HandleGoalDetected;
            }

            AudioManager.Instance?.PlayBgm(BgmType.M02_Match);
            AudioManager.Instance?.PlaySfx(SfxType.Whistle);

            StartAttempt();
        }

        public void StartAttempt()
        {
            if (IsRoundOver || hasFinishedRound) return;

            hasEvaluatedCurrentShot = false;
            IsEvaluatingShot = false;
            lastShotHitPost = false;
            CurrentState = MatchAttemptState.Aiming;

            GoalNetRipple.Instance?.ResetNets();
            BallController.Instance?.ResetBall(ballSpotPosition);
            PlayerController.Instance?.ResetPosition();
            PlayerShooter.Instance?.ResetShooter();
            GoalkeeperAI.Instance?.ResetPosition();

            OnAttemptStarted?.Invoke(CurrentAttempt, maxAttempts);
            OnStatsUpdated?.Invoke(GoalsScored, AttemptsUsed, ScoreManager.Instance != null ? ScoreManager.Instance.CurrentMatchScore : 0);
        }

        public void SetChargingState()
        {
            if (CanAcceptShotInput)
            {
                CurrentState = MatchAttemptState.Charging;
            }
        }

        public void OnShotKickingStarted()
        {
            CurrentState = MatchAttemptState.Kicking;
        }

        public void OnShotFired(Vector3 target, float power, bool isHighPower)
        {
            if (IsRoundOver) return;

            lastShotWasHighPower = isHighPower;
            IsEvaluatingShot = true;
            hasEvaluatedCurrentShot = false;
            CurrentState = MatchAttemptState.BallInFlight;

            if (shotTimeoutCoroutine != null) StopCoroutine(shotTimeoutCoroutine);
            shotTimeoutCoroutine = StartCoroutine(ShotTimeoutRoutine());
        }

        public void NotifyPostHit()
        {
            if (IsEvaluatingShot && !hasEvaluatedCurrentShot)
            {
                lastShotHitPost = true;
            }
        }

        private IEnumerator ShotTimeoutRoutine()
        {
            float elapsed = 0f;
            while (elapsed < ShotTimeoutDuration)
            {
                yield return waitPollStep;
                elapsed += 0.08f;

                if (hasEvaluatedCurrentShot || !IsEvaluatingShot) yield break;

                BallController ball = BallController.Instance;
                if (elapsed > 0.60f && ball != null)
                {
                    Vector3 pos = ball.transform.position;
                    bool outOfBounds = pos.z > 3.5f || Mathf.Abs(pos.x) > 6.5f || pos.y > 5.8f;
                    bool bouncedBackOffPost = ball.HitPost && pos.z < -2.2f && ball.Velocity.z < -0.5f;
                    bool stalledOnPitch = elapsed > 1.30f && ball.Velocity.sqrMagnitude < 0.35f;

                    if (outOfBounds || bouncedBackOffPost || stalledOnPitch)
                    {
                        break;
                    }
                }
            }

            if (!hasEvaluatedCurrentShot && IsEvaluatingShot)
            {
                if (lastShotHitPost)
                {
                    EvaluateShot(ShotResult.PostHit, false, false, "¡AL POSTE!");
                }
                else
                {
                    AudioManager.Instance?.PlaySfx(SfxType.Miss);
                    EvaluateShot(ShotResult.Missed, false, false, "¡FUERA!");
                }
            }
        }

        private void HandleGoalDetected(bool isCornerOrPrecision, Vector3 entryPoint)
        {
            if (hasEvaluatedCurrentShot || !IsEvaluatingShot) return;

            string msg = isCornerOrPrecision
                ? "¡GOLAZO AL ÁNGULO!"
                : (lastShotWasHighPower ? "¡GOL IMPARABLE!" : "¡GOOOOL!");

            EvaluateShot(ShotResult.GoalNormal, isCornerOrPrecision, lastShotWasHighPower, msg);
        }

        public void OnGoalkeeperSavedBall()
        {
            if (hasEvaluatedCurrentShot || !IsEvaluatingShot) return;

            if (saveConfirmCoroutine != null) StopCoroutine(saveConfirmCoroutine);
            saveConfirmCoroutine = StartCoroutine(CheckSaveConfirmationRoutine());
        }

        private IEnumerator CheckSaveConfirmationRoutine()
        {
            yield return new WaitForSeconds(SaveConfirmDelay);
            if (!hasEvaluatedCurrentShot && IsEvaluatingShot)
            {
                EvaluateShot(ShotResult.Saved, false, false, "¡ATAJADA!");
            }
        }

        private void EvaluateShot(ShotResult result, bool isCorner, bool isHighPower, string message)
        {
            if (hasEvaluatedCurrentShot) return;
            hasEvaluatedCurrentShot = true;
            IsEvaluatingShot = false;
            CurrentState = MatchAttemptState.Evaluated;

            if (shotTimeoutCoroutine != null)
            {
                StopCoroutine(shotTimeoutCoroutine);
                shotTimeoutCoroutine = null;
            }
            if (saveConfirmCoroutine != null)
            {
                StopCoroutine(saveConfirmCoroutine);
                saveConfirmCoroutine = null;
            }
            if (postAttemptCoroutine != null)
            {
                StopCoroutine(postAttemptCoroutine);
                postAttemptCoroutine = null;
            }

            PlayerShooter.Instance?.SetInputLocked(true);
            PlayerController.Instance?.SetControlEnabled(false);

            AttemptsUsed = CurrentAttempt;
            ShotScoreResult scoreResult = new ShotScoreResult();

            if (result == ShotResult.GoalNormal)
            {
                GoalsScored++;
                scoreResult = ScoreManager.Instance != null
                    ? ScoreManager.Instance.ProcessGoal(isCorner, isHighPower)
                    : new ShotScoreResult();

                PlayerController.Instance?.PlayGoalCelebration();
                GoalkeeperAI.Instance?.PlayConcededReaction();

                if (SaveManager.Instance != null && SaveManager.Instance.CurrentData != null)
                {
                    SaveManager.Instance.CurrentData.totalGoalsScored++;
                    if (isCorner) SaveManager.Instance.CurrentData.totalCornerGoals++;
                    if (ScoreManager.Instance != null && ScoreManager.Instance.CurrentStreak > SaveManager.Instance.CurrentData.bestGoalStreak)
                    {
                        SaveManager.Instance.CurrentData.bestGoalStreak = ScoreManager.Instance.CurrentStreak;
                    }
                    SaveManager.Instance.Save(flushToDisk: false);
                }

                AchievementManager.Instance?.CheckGoalScored(
                    ScoreManager.Instance?.CurrentStreak ?? 1,
                    SaveManager.Instance?.CurrentData?.totalGoalsScored ?? 1);
            }
            else
            {
                ScoreManager.Instance?.ProcessMissOrSave();
                scoreResult.shotType = result;
                scoreResult.rewardDescription = message;

                if (result == ShotResult.Saved)
                {
                    GoalkeeperAI.Instance?.PlaySaveCelebration();
                    PlayerController.Instance?.PlayMissReaction();
                }
                else
                {
                    PlayerController.Instance?.PlayMissReaction();
                }
            }

            ShotResult displayType = result == ShotResult.GoalNormal ? scoreResult.shotType : result;
            OnAttemptFinished?.Invoke(displayType, scoreResult, message);
            OnStatsUpdated?.Invoke(GoalsScored, AttemptsUsed, ScoreManager.Instance != null ? ScoreManager.Instance.CurrentMatchScore : 0);

            postAttemptCoroutine = StartCoroutine(PostAttemptDelayRoutine());
        }

        private IEnumerator PostAttemptDelayRoutine()
        {
            yield return new WaitForSeconds(PostAttemptDelay);

            int remainingAttempts = maxAttempts - AttemptsUsed;
            bool goalTargetReached = GoalsScored >= requiredGoals;
            bool mathematicallyImpossible = (GoalsScored + remainingAttempts) < requiredGoals;

            if (goalTargetReached)
            {
                FinishRound(true);
            }
            else if (mathematicallyImpossible || AttemptsUsed >= maxAttempts)
            {
                FinishRound(GoalsScored >= requiredGoals);
            }
            else
            {
                CurrentAttempt++;
                CurrentState = MatchAttemptState.Resetting;
                StartAttempt();
            }
        }

        private void FinishRound(bool isVictory)
        {
            if (hasFinishedRound) return;
            hasFinishedRound = true;
            IsRoundOver = true;
            CurrentState = MatchAttemptState.Evaluated;

            PlayerShooter.Instance?.SetInputLocked(true);
            PlayerController.Instance?.SetControlEnabled(false);

            RoundDataSO data = GameManager.Instance != null ? GameManager.Instance.CurrentRoundData : null;
            int roundNum = GameManager.Instance != null ? GameManager.Instance.CurrentSelectedRoundNumber : 1;
            string roundName = data != null ? data.roundName : $"Ronda {roundNum}";

            int totalScore = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentMatchScore : 0;
            int bonusPts = 0;
            int bonusCoins = 0;

            if (isVictory)
            {
                bonusPts = data != null ? data.winPoints : 500;
                bonusCoins = data != null ? data.winCoins : 300;

                ScoreManager.Instance?.AwardRoundVictory(bonusPts, bonusCoins);
                totalScore = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentMatchScore : totalScore;

                int nextRound = roundNum + 1;
                if (nextRound <= 5)
                {
                    SaveManager.Instance?.UnlockRound(nextRound);
                }

                if (SaveManager.Instance != null && SaveManager.Instance.CurrentData != null)
                {
                    SaveManager.Instance.CurrentData.totalRoundsWon++;
                    if (roundNum == 5 && totalScore > SaveManager.Instance.CurrentData.finalRoundBestScore)
                    {
                        SaveManager.Instance.CurrentData.finalRoundBestScore = totalScore;
                    }
                    SaveManager.Instance.Save();
                }

                AchievementManager.Instance?.CheckRoundCompleted(roundNum, totalScore, GoalsScored, AttemptsUsed, requiredGoals);
                AchievementManager.Instance?.CheckCoins(CurrencyManager.Instance?.Coins ?? 0);
            }

            int previousBest = SaveManager.Instance?.CurrentData != null ? SaveManager.Instance.CurrentData.highScore : 0;
            bool isNewRecord = totalScore > previousBest && totalScore > 0;
            SaveManager.Instance?.UpdateHighScore(totalScore);

            int totalMatchCoins = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentMatchCoins : 0;

            RoundEndSummary summary = new RoundEndSummary
            {
                isVictory = isVictory,
                isGrandFinal = isVictory && roundNum == 5,
                roundNumber = roundNum,
                roundName = roundName,
                goalsScored = GoalsScored,
                requiredGoals = requiredGoals,
                attemptsUsed = AttemptsUsed,
                maxAttempts = maxAttempts,
                matchScore = totalScore,
                matchCoins = totalMatchCoins,
                victoryBonusCoins = bonusCoins,
                victoryBonusPoints = bonusPts,
                isNewRecord = isNewRecord,
                unlockedAchievements = matchUnlockedAchievements.ToArray()
            };

            OnRoundEnded?.Invoke(isVictory, GoalsScored, requiredGoals, totalScore);
            OnRoundEndedDetailed?.Invoke(summary);
        }
    }
}
