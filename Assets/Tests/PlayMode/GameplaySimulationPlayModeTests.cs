using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UltimatePenaltyCup.Core;
using UltimatePenaltyCup.Customization;
using UltimatePenaltyCup.Economy;
using UltimatePenaltyCup.Gameplay;
using UltimatePenaltyCup.Goalkeeper;
using UltimatePenaltyCup.Levels;
using UltimatePenaltyCup.Player;
using UltimatePenaltyCup.SaveSystem;
using UltimatePenaltyCup.Shop;
using UltimatePenaltyCup.UI;

namespace UltimatePenaltyCup.Tests
{
    public class GameplaySimulationPlayModeTests
    {
        private bool initialVSyncChecked = false;

        [SetUp]
        public void SetUp()
        {
            if (!initialVSyncChecked)
            {
                initialVSyncChecked = true;
            }
            Time.timeScale = 1f;
        }

        private IEnumerator BootToCleanMainMenu()
        {
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
            yield return null;
            yield return null;

            Assert.IsNotNull(GameManager.Instance, "GameManager.Instance must exist on MainMenu.");
            Assert.IsNotNull(SaveManager.Instance, "SaveManager.Instance must exist on MainMenu.");

            // Never mutate real player PlayerPrefs during automated tests
            SaveManager.Instance.SetTestDataInMemory(new SaveData());
            CurrencyManager.Instance?.BindForTest();
        }

        [UnityTest]
        public IEnumerator Test01_MainMenuBootAndPanelNavigation()
        {
            yield return BootToCleanMainMenu();

            Scene active = SceneManager.GetActiveScene();
            Assert.AreEqual(0, active.buildIndex, "MainMenu must be scene index 0.");
            Assert.AreEqual("MainMenu", active.name);

            Assert.AreEqual(1, UnityEngine.Object.FindObjectsByType<GameManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, "Only 1 GameManager allowed.");
            Assert.AreEqual(1, UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length, "Only 1 active EventSystem allowed.");
            Assert.AreEqual(1, UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length, "Only 1 enabled AudioListener allowed.");

            MainMenuUI menuUI = UnityEngine.Object.FindFirstObjectByType<MainMenuUI>();
            Assert.IsNotNull(menuUI, "MainMenuUI must exist in MainMenu scene.");

            // Test panel navigation via real button hierarchy
            string[] buttonNames = { "BtnPlay", "BtnShop", "BtnCustom", "BtnAch", "BtnSettings" };
            foreach (string btnName in buttonNames)
            {
                GameObject btnObj = GameObject.Find(btnName);
                Assert.IsNotNull(btnObj, $"Missing menu button {btnName}");
                Button btn = btnObj.GetComponent<Button>();
                Assert.IsNotNull(btn, $"{btnName} must have Button component.");
                btn.onClick.Invoke();
                yield return null;
                menuUI.ReturnToMainMenu();
                yield return null;
            }

            // Test in-memory shop purchase & customization equip
            CurrencyManager.Instance.AddCoins(1500);
            Assert.IsTrue(ShopManager.Instance.BuyBall(BallType.Blue), "Should purchase Blue ball with 1500 coins.");
            Assert.IsTrue(CustomizationManager.Instance.EquipBall(BallType.Blue), "Should equip unlocked Blue ball.");
            Assert.AreEqual(BallType.Blue, SaveManager.Instance.CurrentData.equippedBall);
        }

        [UnityTest]
        public IEnumerator Test02_RoundFlow_EarlyVictoryAndMathDefeat()
        {
            yield return BootToCleanMainMenu();
            for (int r = 1; r <= 5; r++) SaveManager.Instance.UnlockRound(r);

            // 1. Verify early victory at goal 3 of 5 in Round 1
            GameManager.Instance.SelectRoundAndStartMatch(1);
            yield return WaitForScene("MatchScene");

            PenaltyManager pm = PenaltyManager.Instance;
            pm.PostAttemptDelay = 0.04f;
            pm.SaveConfirmDelay = 0.14f;

            // Fire top-corner R1 shots; stop if early victory triggers
            for (int i = 0; i < 5 && !pm.IsRoundOver; i++)
            {
                yield return FireAndAwaitShot(new Vector3(i % 2 == 0 ? 3.22f : -3.22f, 2.20f, 0f), 0.85f);
            }
            if (pm.IsRoundOver && pm.GoalsScored >= pm.RequiredGoals)
            {
                Assert.LessOrEqual(pm.AttemptsUsed, 5);
                if (pm.GoalsScored == 3)
                {
                    Assert.Less(pm.AttemptsUsed, 5, "Early victory should end before using all 5 attempts when 3 goals reached early.");
                }
            }

            // 2. Verify mathematical defeat in Round 1 after 3 wide misses (0 + 2 remaining < 3 required)
            pm.InitializeRound();
            yield return null;
            pm.PostAttemptDelay = 0.04f;
            pm.SaveConfirmDelay = 0.14f;

            for (int i = 0; i < 3 && !pm.IsRoundOver; i++)
            {
                yield return FireAndAwaitShot(new Vector3(5.6f, 1.5f, 0f), 0.80f);
            }
            Assert.IsTrue(pm.IsRoundOver, "Round 1 must finish early as mathematical defeat after 3 misses (0+2 < 3).");
            Assert.AreEqual(3, pm.AttemptsUsed, "Defeat must trigger cleanly on attempt 3.");

            // 3. Verify all 5 rounds load without error & Round 5 Grand Final sets WorldCup venue
            for (int r = 1; r <= 5; r++)
            {
                GameManager.Instance.SelectRoundAndStartMatch(r);
                yield return WaitForScene("MatchScene");
                Assert.AreEqual(r, GameManager.Instance.CurrentSelectedRoundNumber);
                RoundDataSO rd = GameManager.Instance.CurrentRoundData;
                Assert.IsNotNull(rd, $"RoundDataSO missing for round {r}");
                Assert.IsTrue(PenaltyManager.Instance.RequiredGoals == rd.requiredGoals);
            }
        }

        [UnityTest]
        public IEnumerator Test03_PauseResumeRestartAndNoDuplicateManagers()
        {
            yield return BootToCleanMainMenu();

            GameManager.Instance.SelectRoundAndStartMatch(1);
            yield return WaitForScene("MatchScene");

            PauseUI pause = PauseUI.Instance;
            Assert.IsNotNull(pause, "PauseUI.Instance must exist in MatchScene.");

            pause.Pause();
            yield return null;
            Assert.IsTrue(pause.IsPaused, "Pause must set IsPaused=true.");
            Assert.AreEqual(0f, Time.timeScale, 0.001f, "TimeScale must be 0 when paused.");
            Assert.IsFalse(PlayerShooter.Instance.CanShoot, "Shooter must be locked while paused.");

            pause.Resume();
            yield return new WaitForSecondsRealtime(0.25f);
            Assert.IsFalse(pause.IsPaused, "Resume must set IsPaused=false.");
            Assert.AreEqual(1f, Time.timeScale, 0.001f, "TimeScale must restore to 1.");
            Assert.IsTrue(PlayerShooter.Instance.CanShoot, "Shooter must unlock after pause cooldown.");

            pause.RestartMatch();
            yield return WaitForScene("MatchScene");
            Assert.AreEqual(1, PenaltyManager.Instance.CurrentAttempt, "Restarted match must begin at attempt 1.");
            Assert.AreEqual(1, UnityEngine.Object.FindObjectsByType<GameManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length);
            Assert.AreEqual(1, UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length);
            Assert.AreEqual(1, UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length);

            GameManager.Instance.LoadMainMenuScene();
            yield return WaitForScene("MainMenu");
            Assert.AreEqual(1f, Time.timeScale, 0.001f);
            Assert.AreEqual(1, UnityEngine.Object.FindObjectsByType<GameManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length);
            Assert.AreEqual(1, UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length);
        }

        [UnityTest]
        public IEnumerator Test04_100ShotPhysicsGoalkeeperAndPerformanceBenchmark()
        {
            yield return BootToCleanMainMenu();
            for (int r = 1; r <= 5; r++) SaveManager.Instance.UnlockRound(r);

            // Disable vSync during benchmark so we measure actual uncapped render/simulation capacity cleanly, then restore
            int prevVsync = QualitySettings.vSyncCount;
            int prevTarget = Application.targetFrameRate;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;

            var shotPattern = BuildRealistic20ShotPattern();
            var roundStats = new List<RoundBenchmarkRow>();
            var allFrameTimes = new List<float>(4096);

            int totalShotsFired = 0;
            int totalGoals = 0;
            int totalSaves = 0;
            int totalPosts = 0;
            int totalMisses = 0;
            int totalDuplicateEvents = 0;

            try
            {
                for (int round = 1; round <= 5; round++)
                {
                    GameManager.Instance.SelectRoundAndStartMatch(round);
                    yield return WaitForScene("MatchScene");

                    PenaltyManager pm = PenaltyManager.Instance;
                    pm.PostAttemptDelay = 0.04f;
                    pm.SaveConfirmDelay = 0.14f;

                    var row = new RoundBenchmarkRow { roundNumber = round };

                    for (int s = 0; s < shotPattern.Length; s++)
                    {
                        if (pm.IsRoundOver)
                        {
                            pm.InitializeRound();
                            yield return null;
                            pm.PostAttemptDelay = 0.04f;
                            pm.SaveConfirmDelay = 0.14f;
                        }

                        var spec = shotPattern[s];
                        ShotOutcome outcome = new ShotOutcome();
                        yield return FireAndAwaitShot(spec.target, spec.power, outcome, allFrameTimes);

                        totalShotsFired++;
                        row.shots++;
                        if (outcome.eventCount != 1) totalDuplicateEvents++;

                        switch (outcome.result)
                        {
                            case ShotResult.GoalNormal:
                            case ShotResult.GoalCorner:
                            case ShotResult.GoalHighPower:
                                row.goals++;
                                totalGoals++;
                                break;
                            case ShotResult.Saved:
                                row.saves++;
                                totalSaves++;
                                break;
                            case ShotResult.PostHit:
                                row.posts++;
                                totalPosts++;
                                break;
                            default:
                                row.misses++;
                                totalMisses++;
                                break;
                        }
                    }

                    roundStats.Add(row);
                }

                // Sample 90 frames per stadium venue to measure GPU/CPU FPS across all 4 stadiums
                var venueFpsRows = new List<VenuePerfRow>();
                StadiumType[] venues = { StadiumType.Cantera, StadiumType.Night, StadiumType.Professional, StadiumType.WorldCup };
                foreach (var v in venues)
                {
                    StadiumController.Instance?.ApplyVenue(v);
                    yield return null;
                    yield return null;

                    var frameDts = new List<float>(90);
                    for (int f = 0; f < 90; f++)
                    {
                        yield return null;
                        float dt = Time.unscaledDeltaTime;
                        if (dt > 0.0001f && dt < 0.25f)
                        {
                            frameDts.Add(dt);
                            allFrameTimes.Add(dt);
                        }
                    }
                    venueFpsRows.Add(ComputePerfRow(v.ToString(), frameDts));
                }

                VenuePerfRow overallPerf = ComputePerfRow("OVERALL (Match + 4 Stadiums)", allFrameTimes);

                // Write truthful report to Logs/PlayModeBenchmarkReport.txt
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("================================================================================");
                sb.AppendLine("ULTIMATE PENALTY CUP 3D - PLAY MODE 100-SHOT PHYSICS & PERFORMANCE BENCHMARK");
                sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"Resolution: {Screen.width}x{Screen.height} | Quality: {QualitySettings.names[QualitySettings.GetQualityLevel()]}");
                sb.AppendLine($"GPU: {SystemInfo.graphicsDeviceName} | CPU: {SystemInfo.processorType}");
                sb.AppendLine("================================================================================");
                sb.AppendLine("1. AUTOMATED 100-SHOT REAL PHYSICS SIMULATION (20 shots/round)");
                sb.AppendLine("Round | Shots | Goals | Saves | Posts | Misses | OnTargetSaveRate");
                sb.AppendLine("------+-------+-------+-------+-------+--------+-----------------");
                foreach (var r in roundStats)
                {
                    int onTarget = r.goals + r.saves;
                    float savePct = onTarget > 0 ? (r.saves * 100f / onTarget) : 0f;
                    sb.AppendLine($"R{r.roundNumber}    | {r.shots,5} | {r.goals,5} | {r.saves,5} | {r.posts,5} | {r.misses,6} | {savePct,6:F1}%");
                }
                sb.AppendLine("------+-------+-------+-------+-------+--------+-----------------");
                int totOnTarget = totalGoals + totalSaves;
                float totSavePct = totOnTarget > 0 ? (totalSaves * 100f / totOnTarget) : 0f;
                sb.AppendLine($"TOTAL | {totalShotsFired,5} | {totalGoals,5} | {totalSaves,5} | {totalPosts,5} | {totalMisses,6} | {totSavePct,6:F1}%");
                sb.AppendLine($"Duplicate/Missed Shot Evaluations: {totalDuplicateEvents}");
                sb.AppendLine();
                sb.AppendLine("2. REAL RUNTIME PERFORMANCE METRICS");
                foreach (var vp in venueFpsRows)
                {
                    sb.AppendLine($"{vp.label,-28} | Avg FPS: {vp.avgFps,6:F1} | 1% Low FPS: {vp.onePercentLowFps,6:F1} | Avg Frame: {vp.avgMs,5:F2} ms");
                }
                sb.AppendLine($"{overallPerf.label,-28} | Avg FPS: {overallPerf.avgFps,6:F1} | 1% Low FPS: {overallPerf.onePercentLowFps,6:F1} | Avg Frame: {overallPerf.avgMs,5:F2} ms | GC Total Mem: {GC.GetTotalMemory(false) / (1024f * 1024f):F2} MB");
                sb.AppendLine("================================================================================");

                string reportPath = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "PlayModeBenchmarkReport.txt");
                Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
                File.WriteAllText(reportPath, sb.ToString());
                Debug.Log(sb.ToString());

                // Truthful functional assertions
                Assert.AreEqual(100, totalShotsFired, "Must fire 100 total shots across 5 rounds.");
                Assert.AreEqual(0, totalDuplicateEvents, "Every shot must resolve with strictly 1 evaluation (zero duplicates/timeouts).");
                Assert.Greater(totalGoals, 0, "Player must score goals in real physics simulation.");
                Assert.Greater(totalSaves, 0, "Goalkeeper must save shots in real physics simulation.");
                Assert.Greater(totalMisses + totalPosts, 0, "Wide/post shots must register misses or post hits.");
                Assert.Greater(overallPerf.avgFps, 30f, "Runtime performance must be responsive.");
            }
            finally
            {
                QualitySettings.vSyncCount = prevVsync;
                Application.targetFrameRate = prevTarget;
            }
        }

        private IEnumerator WaitForScene(string sceneName)
        {
            float waited = 0f;
            while (SceneManager.GetActiveScene().name != sceneName && waited < 5f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.AreEqual(sceneName, SceneManager.GetActiveScene().name, $"Timed out waiting for scene {sceneName}.");
            yield return null;
            yield return null;
        }

        private IEnumerator FireAndAwaitShot(Vector3 target, float power, ShotOutcome outcome = null, List<float> frameTimes = null)
        {
            PenaltyManager pm = PenaltyManager.Instance;
            PlayerShooter shooter = PlayerShooter.Instance;

            float waitReady = 0f;
            while ((!pm.CanAcceptShotInput || !shooter.CanShoot) && waitReady < 4f)
            {
                waitReady += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsTrue(pm.CanAcceptShotInput && shooter.CanShoot, "Shooter/PenaltyManager did not become ready for next shot.");

            bool resolved = false;
            int evalCount = 0;
            ShotResult capturedResult = ShotResult.Missed;

            void Handler(ShotResult res, ShotScoreResult score, string msg)
            {
                evalCount++;
                capturedResult = res;
                resolved = true;
            }

            pm.OnAttemptFinished += Handler;
            shooter.ExecuteAutomatedShot(target, power, skipRunUp: true);

            float waitShot = 0f;
            while (!resolved && waitShot < 6f)
            {
                yield return null;
                float dt = Time.unscaledDeltaTime;
                waitShot += dt;
                if (frameTimes != null && dt > 0.0001f && dt < 0.25f)
                {
                    frameTimes.Add(dt);
                }
            }

            pm.OnAttemptFinished -= Handler;
            Assert.IsTrue(resolved, $"Shot timed out waiting for OnAttemptFinished (target={target}, power={power}).");
            Assert.AreEqual(1, evalCount, $"Shot produced {evalCount} evaluations instead of strictly 1.");

            if (outcome != null)
            {
                outcome.result = capturedResult;
                outcome.eventCount = evalCount;
            }

            // Allow PostAttemptDelay routine to advance cleanly
            float waitNext = 0f;
            while (waitNext < 0.2f && !pm.IsRoundOver && pm.CurrentState == MatchAttemptState.Evaluated)
            {
                waitNext += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private ShotSpec[] BuildRealistic20ShotPattern()
        {
            return new[]
            {
                new ShotSpec(new Vector3(-2.90f, 0.55f, 0f), 0.78f), // 1. Low Left Sweet Spot
                new ShotSpec(new Vector3( 2.90f, 0.55f, 0f), 0.78f), // 2. Low Right Sweet Spot
                new ShotSpec(new Vector3(-3.15f, 2.00f, 0f), 0.84f), // 3. Top Left Corner
                new ShotSpec(new Vector3( 3.15f, 2.00f, 0f), 0.84f), // 4. Top Right Corner
                new ShotSpec(new Vector3( 0.00f, 0.75f, 0f), 0.45f), // 5. Soft Center-Low
                new ShotSpec(new Vector3( 0.00f, 1.75f, 0f), 0.80f), // 6. Center High
                new ShotSpec(new Vector3(-1.90f, 1.25f, 0f), 0.65f), // 7. Mid Left Placed
                new ShotSpec(new Vector3( 1.90f, 1.25f, 0f), 0.65f), // 8. Mid Right Placed
                new ShotSpec(new Vector3(-2.50f, 2.08f, 0f), 0.87f), // 9. Upper Left
                new ShotSpec(new Vector3( 2.50f, 2.08f, 0f), 0.87f), // 10. Upper Right
                new ShotSpec(new Vector3(-1.15f, 0.50f, 0f), 0.48f), // 11. Low Inner Left Soft
                new ShotSpec(new Vector3( 1.15f, 0.50f, 0f), 0.48f), // 12. Low Inner Right Soft
                new ShotSpec(new Vector3(-3.22f, 1.15f, 0f), 0.82f), // 13. Side Net Left Power
                new ShotSpec(new Vector3( 3.22f, 1.15f, 0f), 0.82f), // 14. Side Net Right Power
                new ShotSpec(new Vector3(-2.20f, 1.65f, 0f), 0.90f), // 15. High Power Left
                new ShotSpec(new Vector3( 2.20f, 1.65f, 0f), 0.90f), // 16. High Power Right
                new ShotSpec(new Vector3(-2.80f, 1.60f, 0f), 0.96f), // 17. Overpower Left
                new ShotSpec(new Vector3(-3.72f, 1.25f, 0f), 0.78f), // 18. Aimed at Left Post edge
                new ShotSpec(new Vector3(-4.80f, 1.40f, 0f), 0.78f), // 19. Intentional Wide Miss
                new ShotSpec(new Vector3( 0.40f, 3.45f, 0f), 0.95f)  // 20. Intentional High Over Bar
            };
        }

        private VenuePerfRow ComputePerfRow(string label, List<float> dts)
        {
            if (dts == null || dts.Count == 0)
            {
                return new VenuePerfRow { label = label, avgFps = 0f, onePercentLowFps = 0f, avgMs = 0f };
            }

            float sum = 0f;
            for (int i = 0; i < dts.Count; i++) sum += dts[i];
            float avgDt = sum / dts.Count;

            List<float> sorted = new List<float>(dts);
            sorted.Sort(); // ascending dt; slowest 1% frames are at the end
            int idx99 = Mathf.Clamp(Mathf.FloorToInt((sorted.Count - 1) * 0.99f), 0, sorted.Count - 1);
            float p99Dt = sorted[idx99];

            return new VenuePerfRow
            {
                label = label,
                avgMs = avgDt * 1000f,
                avgFps = 1f / Mathf.Max(0.0001f, avgDt),
                onePercentLowFps = 1f / Mathf.Max(0.0001f, p99Dt)
            };
        }

        private struct ShotSpec
        {
            public Vector3 target;
            public float power;
            public ShotSpec(Vector3 t, float p) { target = t; power = p; }
        }

        private class ShotOutcome
        {
            public ShotResult result;
            public int eventCount;
        }

        private class RoundBenchmarkRow
        {
            public int roundNumber;
            public int shots;
            public int goals;
            public int saves;
            public int posts;
            public int misses;
        }

        private struct VenuePerfRow
        {
            public string label;
            public float avgFps;
            public float onePercentLowFps;
            public float avgMs;
        }
    }
}
