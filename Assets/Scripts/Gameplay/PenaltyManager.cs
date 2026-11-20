#pragma warning disable 0649
using System;
using System.Collections;
using UnityEngine;
using UltimatePenaltyCup.Core;
using UltimatePenaltyCup.Economy;
using UltimatePenaltyCup.Levels;
using UltimatePenaltyCup.Player;
using UltimatePenaltyCup.Goalkeeper;
using UltimatePenaltyCup.SaveSystem;
using UltimatePenaltyCup.Achievements;

namespace UltimatePenaltyCup.Gameplay
{
    public class PenaltyManager : MonoBehaviour
    {
        public static PenaltyManager Instance { get; private set; }

        [Header("Rules")]
        [SerializeField] private int maxAttempts = 5;
        [SerializeField] private int requiredGoals = 3;

        [Header("Positions")]
        [SerializeField] private Vector3 ballSpotPosition = new Vector3(0f, 0.15f, -11f);

        public int CurrentAttempt { get; private set; } = 1;
        public int GoalsScored { get; private set; } = 0;
        public int RequiredGoals => requiredGoals;
        public int MaxAttempts => maxAttempts;

        public bool IsEvaluatingShot { get; private set; } = false;
        private bool hasEvaluatedCurrentShot = false;
        private bool lastShotWasHighPower = false;
        private Coroutine shotTimeoutCoroutine;

        public event Action<int, int> OnAttemptStarted; // (attempt, max)
        public event Action<ShotResult, ShotScoreResult, string> OnAttemptFinished; // result, scoreInfo, message
        public event Action<int, int, int> OnStatsUpdated; // (goals, attemptsUsed, score)
        public event Action<bool, int, int, int> OnRoundEnded; // (isVictory, goalsScored, requiredGoals, finalScore)

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            InitializeRound();
        }

        public void InitializeRound()
        {
            RoundDataSO data = GameManager.Instance != null ? GameManager.Instance.CurrentRoundData : null;
            if (data != null)
            {
                requiredGoals = data.requiredGoals;
                maxAttempts = data.maxAttempts;
                GoalkeeperAI.Instance?.Configure(data);
            }

            CurrentAttempt = 1;
            GoalsScored = 0;
            hasEvaluatedCurrentShot = false;
            IsEvaluatingShot = false;

            ScoreManager.Instance?.ResetMatchScore();
            OnStatsUpdated?.Invoke(GoalsScored, CurrentAttempt - 1, 0);

            if (GoalDetector.Instance != null)
            {
                GoalDetector.Instance.OnGoalScored -= HandleGoalDetected;
                GoalDetector.Instance.OnGoalScored += HandleGoalDetected;
            }

            StartAttempt();
        }

        private void OnDestroy()
        {
            if (GoalDetector.Instance != null)
            {
                GoalDetector.Instance.OnGoalScored -= HandleGoalDetected;
            }
        }

        public void StartAttempt()
        {
            hasEvaluatedCurrentShot = false;
            IsEvaluatingShot = false;

            BallController.Instance?.ResetBall(ballSpotPosition);
            PlayerController.Instance?.ResetPosition();
            PlayerShooter.Instance?.ResetShooter();
            GoalkeeperAI.Instance?.ResetPosition();

            OnAttemptStarted?.Invoke(CurrentAttempt, maxAttempts);
            OnStatsUpdated?.Invoke(GoalsScored, CurrentAttempt - 1, ScoreManager.Instance != null ? ScoreManager.Instance.CurrentMatchScore : 0);
        }

        public void OnShotFired(Vector3 target, float power, bool isHighPower)
        {
            lastShotWasHighPower = isHighPower;
            IsEvaluatingShot = true;
            hasEvaluatedCurrentShot = false;

            if (shotTimeoutCoroutine != null) StopCoroutine(shotTimeoutCoroutine);
            shotTimeoutCoroutine = StartCoroutine(ShotTimeoutRoutine());
        }

        private IEnumerator ShotTimeoutRoutine()
        {
            // Wait up to 3.2 seconds for shot to conclude
            yield return new WaitForSeconds(3.2f);

            if (!hasEvaluatedCurrentShot && IsEvaluatingShot)
            {
                // Ball stopped or flew out of bounds
                EvaluateShot(ShotResult.Missed, false, false, "¡Fallaste el disparo!");
            }
        }

        private void HandleGoalDetected(bool isCornerOrPrecision, Vector3 entryPoint)
        {
            if (hasEvaluatedCurrentShot || !IsEvaluatingShot) return;

            EvaluateShot(ShotResult.GoalNormal, isCornerOrPrecision, lastShotWasHighPower, "¡GOOOOL!");
        }

        public void OnGoalkeeperSavedBall()
        {
            if (hasEvaluatedCurrentShot || !IsEvaluatingShot) return;

            // Give a short delay to check if ball still deflects into goal
            StartCoroutine(CheckSaveConfirmationRoutine());
        }

        private IEnumerator CheckSaveConfirmationRoutine()
        {
            yield return new WaitForSeconds(0.45f);
            if (!hasEvaluatedCurrentShot && IsEvaluatingShot)
            {
                EvaluateShot(ShotResult.Saved, false, false, "¡El portero lo detuvo!");
            }
        }

        private void EvaluateShot(ShotResult result, bool isCorner, bool isHighPower, string message)
        {
            if (hasEvaluatedCurrentShot) return;
            hasEvaluatedCurrentShot = true;
            IsEvaluatingShot = false;

            if (shotTimeoutCoroutine != null)
            {
                StopCoroutine(shotTimeoutCoroutine);
                shotTimeoutCoroutine = null;
            }

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
                    if (ScoreManager.Instance != null && ScoreManager.Instance.CurrentStreak > SaveManager.Instance.CurrentData.bestGoalStreak)
                    {
                        SaveManager.Instance.CurrentData.bestGoalStreak = ScoreManager.Instance.CurrentStreak;
                    }
                    SaveManager.Instance.Save();
                }

                AchievementManager.Instance?.CheckGoalScored(ScoreManager.Instance?.CurrentStreak ?? 1, SaveManager.Instance?.CurrentData?.totalGoalsScored ?? 1);
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

            OnAttemptFinished?.Invoke(result, scoreResult, message);
            OnStatsUpdated?.Invoke(GoalsScored, CurrentAttempt, ScoreManager.Instance != null ? ScoreManager.Instance.CurrentMatchScore : 0);

            StartCoroutine(PostAttemptDelayRoutine());
        }

        private IEnumerator PostAttemptDelayRoutine()
        {
            yield return new WaitForSeconds(2.4f);

            if (CurrentAttempt >= maxAttempts)
            {
                FinishRound();
            }
            else
            {
                CurrentAttempt++;
                StartAttempt();
            }
        }

        private void FinishRound()
        {
            bool isVictory = GoalsScored >= requiredGoals;
            int totalScore = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentMatchScore : 0;
            int roundNum = GameManager.Instance != null ? GameManager.Instance.CurrentSelectedRoundNumber : 1;

            if (isVictory)
            {
                RoundDataSO data = GameManager.Instance != null ? GameManager.Instance.CurrentRoundData : null;
                int winPts = data != null ? data.winPoints : 500;
                int winCoins = data != null ? data.winCoins : 300;

                ScoreManager.Instance?.AwardRoundVictory(winPts, winCoins);
                totalScore = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentMatchScore : totalScore;

                // Unlock next round
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

                AchievementManager.Instance?.CheckRoundCompleted(roundNum, totalScore);
                AchievementManager.Instance?.CheckCoins(CurrencyManager.Instance?.Coins ?? 0);
            }

            OnRoundEnded?.Invoke(isVictory, GoalsScored, requiredGoals, totalScore);
        }
    }
}
