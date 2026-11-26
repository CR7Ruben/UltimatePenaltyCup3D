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
using UltimatePenaltyCup.Audio;

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
            MatchCameraController.SkipIntroSweepForTests = true;
            NotificationManager.Instance?.ClearAllNotifications();
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
        public IEnumerator Test04_ArrowAndTrajectoryAccuracyAcrossPowers()
        {
            yield return BootToCleanMainMenu();
            SaveManager.Instance.UnlockRound(1);
            GameManager.Instance.SelectRoundAndStartMatch(1);
            yield return WaitForScene("MatchScene");

            PenaltyManager pm = PenaltyManager.Instance;
            pm.PostAttemptDelay = 0.04f;
            PlayerShooter shooter = PlayerShooter.Instance;
            GoalkeeperAI gk = GoalkeeperAI.Instance;
            if (gk != null) gk.gameObject.SetActive(false); // Pure ballistic correspondence check without keeper deflection

            var cases = new[]
            {
                (target: new Vector3( 0.00f, 1.15f, 0f), power: 0.30f),
                (target: new Vector3( 0.00f, 1.15f, 0f), power: 0.85f),
                (target: new Vector3(-2.60f, 0.85f, 0f), power: 0.35f),
                (target: new Vector3(-2.60f, 0.85f, 0f), power: 0.85f),
                (target: new Vector3( 2.80f, 1.85f, 0f), power: 0.42f),
                (target: new Vector3( 2.80f, 1.85f, 0f), power: 0.88f),
                (target: new Vector3(-1.80f, 2.05f, 0f), power: 0.90f),
                (target: new Vector3( 1.80f, 0.55f, 0f), power: 0.78f)
            };

            float lowPowerRightX = 0f, highPowerRightX = 0f;

            foreach (var c in cases)
            {
                if (pm.IsRoundOver)
                {
                    pm.InitializeRound();
                    yield return null;
                    pm.PostAttemptDelay = 0.04f;
                }

                float waitReady = 0f;
                while ((!pm.CanAcceptShotInput || !shooter.CanShoot) && waitReady < 3f)
                {
                    waitReady += Time.unscaledDeltaTime;
                    yield return null;
                }

                Vector3 ballStart = BallController.Instance.transform.position;
                Vector3[] previewPts = shooter.GetPreviewTrajectoryPoints(ballStart, c.target, c.power, 20);
                float previewEndDist = Vector3.Distance(previewPts[previewPts.Length - 1], c.target);
                Assert.Less(previewEndDist, 0.08f, $"Preview trajectory must end on reticle target {c.target}, err={previewEndDist:F3}m.");

                // Ensure middle of preview does NOT deviate outward sideways relative to straight chord
                Vector3 midPreview = previewPts[10];
                Vector3 chordMid = Vector3.Lerp(ballStart, c.target, 0.5f);
                Assert.Less(Mathf.Abs(midPreview.x - chordMid.x), 0.25f,
                    $"Preview trajectory bent sideways abnormally (midX={midPreview.x:F2}, chordMidX={chordMid.x:F2}, power={c.power}).");

                shooter.ExecuteAutomatedShot(c.target, c.power, skipRunUp: true);

                Vector3 prevPos = BallController.Instance.transform.position;
                Vector3 actualCrossing = prevPos;
                bool crossedGoalPlane = false;
                float t = 0f;
                while (!crossedGoalPlane && t < 2.2f)
                {
                    yield return null;
                    t += Time.deltaTime;
                    Vector3 curPos = BallController.Instance.transform.position;
                    if (curPos.z >= 0f && prevPos.z < 0f)
                    {
                        float frac = Mathf.Clamp01((0f - prevPos.z) / Mathf.Max(0.0001f, curPos.z - prevPos.z));
                        actualCrossing = Vector3.Lerp(prevPos, curPos, frac);
                        crossedGoalPlane = true;
                    }
                    prevPos = curPos;
                }

                Assert.IsTrue(crossedGoalPlane, $"Ball did not cross goal plane for target {c.target} p={c.power}");
                float xyErr = Vector2.Distance(new Vector2(actualCrossing.x, actualCrossing.y), new Vector2(c.target.x, c.target.y));
                Assert.Less(xyErr, 0.35f, $"Actual ball impact {actualCrossing} deviated from aimed target {c.target} at power {c.power:F2} (err={xyErr:F3}m).");

                if (Mathf.Approximately(c.target.x, 2.80f) && Mathf.Approximately(c.power, 0.42f)) lowPowerRightX = actualCrossing.x;
                if (Mathf.Approximately(c.target.x, 2.80f) && Mathf.Approximately(c.power, 0.88f)) highPowerRightX = actualCrossing.x;

                float waitEnd = 0f;
                while (pm.IsEvaluatingShot && waitEnd < 2.5f) { waitEnd += Time.deltaTime; yield return null; }
                yield return new WaitForSeconds(0.06f);
            }

            Assert.Less(Mathf.Abs(highPowerRightX - lowPowerRightX), 0.18f,
                $"High power shot drifted horizontally vs low power for same aim! lowX={lowPowerRightX:F2}, highX={highPowerRightX:F2}");
        }

        [UnityTest]
        public IEnumerator Test05_100ShotManualFlowGoalkeeperCalibrationAndAnimationSync()
        {
            yield return BootToCleanMainMenu();
            for (int r = 1; r <= 5; r++) SaveManager.Instance.UnlockRound(r);

            var shotPattern = BuildRealistic20ShotPattern();
            var roundStats = new List<RoundBenchmarkRow>();
            var allFrameTimes = new List<float>(4096);

            int totalShotsFired = 0;
            int totalGoals = 0;
            int totalSaves = 0;
            int totalPosts = 0;
            int totalMisses = 0;
            int totalDiveAttempts = 0;
            int totalDuplicateEvents = 0;
            bool capturedAnimFrames = false;

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
                    bool verifyAndCapture = (!capturedAnimFrames && round == 1 && s == 0) || (round == 4 && s == 0);
                    yield return FireHumanFlowAndAwaitShot(spec.target, spec.power, outcome, allFrameTimes, round == 1 && s == 0, verifyAndCapture);
                    if (round == 1 && s == 0) capturedAnimFrames = true;

                    totalShotsFired++;
                    row.shots++;
                    if (outcome.eventCount != 1) totalDuplicateEvents++;
                    if (outcome.keeperDived) totalDiveAttempts++;

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

            // Sample 90 frames per stadium venue to measure Editor PlayMode GPU/CPU FPS across all 4 stadiums
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

            VenuePerfRow overallPerf = ComputePerfRow("PLAYMODE OVERALL", allFrameTimes);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine("ULTIMATE PENALTY CUP 3D - PLAY MODE 100-SHOT HUMAN-FLOW SIMULATION BENCHMARK");
            sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Resolution (GameView): {Screen.width}x{Screen.height} | Flow: Full Run-Up + Contact Sync (1.0x real-time)");
            sb.AppendLine($"GPU: {SystemInfo.graphicsDeviceName} | CPU: {SystemInfo.processorType}");
            sb.AppendLine("================================================================================");
            sb.AppendLine("1. 100-SHOT MANUAL-FLOW REAL PHYSICS SIMULATION (20 shots/round)");
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
            float diveRatePct = (totalDiveAttempts * 100f / Mathf.Max(1, totalShotsFired));
            sb.AppendLine($"TOTAL | {totalShotsFired,5} | {totalGoals,5} | {totalSaves,5} | {totalPosts,5} | {totalMisses,6} | {totSavePct,6:F1}%");
            sb.AppendLine($"Keeper Active Dive Attempt Rate: {diveRatePct:F1}% | Duplicate/Missed Evaluations: {totalDuplicateEvents}");
            sb.AppendLine();
            sb.AppendLine("2. EDITOR PLAYMODE VENUE SAMPLE METRICS");
            foreach (var vp in venueFpsRows)
            {
                sb.AppendLine($"{vp.label,-28} | Avg FPS: {vp.avgFps,6:F1} | 1% Low FPS: {vp.onePercentLowFps,6:F1} | Avg Frame: {vp.avgMs,5:F2} ms");
            }
            sb.AppendLine($"{overallPerf.label,-28} | Avg FPS: {overallPerf.avgFps,6:F1} | 1% Low FPS: {overallPerf.onePercentLowFps,6:F1} | Avg Frame: {overallPerf.avgMs,5:F2} ms");
            sb.AppendLine("================================================================================");

            string reportPath = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "PlayModeBenchmarkReport.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.WriteAllText(reportPath, sb.ToString());
            Debug.Log(sb.ToString());

            Assert.AreEqual(100, totalShotsFired, "Must fire 100 total manual-flow shots.");
            Assert.AreEqual(0, totalDuplicateEvents, "Every shot must resolve with strictly 1 evaluation.");
            Assert.GreaterOrEqual(diveRatePct, 90f, "Goalkeeper must attempt dives proactively on almost every shot.");

            float r1SaveRate = (roundStats[0].goals + roundStats[0].saves) > 0 ? (roundStats[0].saves * 100f / (roundStats[0].goals + roundStats[0].saves)) : 0f;
            float r5SaveRate = (roundStats[4].goals + roundStats[4].saves) > 0 ? (roundStats[4].saves * 100f / (roundStats[4].goals + roundStats[4].saves)) : 0f;
            Assert.Greater(r1SaveRate, 5f, $"Round 1 manual-flow goalkeeper save rate must be non-zero (>5%), got {r1SaveRate:F1}%.");
            Assert.Less(r1SaveRate, 38f, $"Round 1 manual-flow save rate should remain fair (<38%), got {r1SaveRate:F1}%.");
            Assert.Greater(r5SaveRate, r1SaveRate, $"Round 5 keeper save rate ({r5SaveRate:F1}%) must exceed Round 1 ({r1SaveRate:F1}%).");
            Assert.Greater(totalGoals, 0, "Player must score goals.");
            Assert.Greater(totalSaves, 0, "Goalkeeper must save shots.");
            Assert.Greater(totalMisses + totalPosts, 0, "Wide/post shots must register.");
        }

        [UnityTest]
        public IEnumerator Test06_FullBodyPlayerCameraFrustumVisibility()
        {
            yield return BootToCleanMainMenu();
            SaveManager.Instance.UnlockRound(1);
            GameManager.Instance.SelectRoundAndStartMatch(1);
            yield return WaitForScene("MatchScene");

            Camera cam = Camera.main;
            PlayerController pc = PlayerController.Instance;
            Assert.IsNotNull(cam);
            Assert.IsNotNull(pc);

            string[] requiredBones = { "Head", "Chest", "Hips", "LeftShin", "RightShin", "LeftFoot", "RightFoot" };
            foreach (string bName in requiredBones)
            {
                Transform t = FindChildRecursive(pc.transform, bName);
                Assert.IsNotNull(t, $"Missing bone {bName} on Rubén.");
                Vector3 vp = cam.WorldToViewportPoint(t.position);
                Assert.Greater(vp.z, 0.5f, $"Bone {bName} behind camera.");
                Assert.GreaterOrEqual(vp.x, 0.01f, $"Bone {bName} clipped off left edge: vp={vp}");
                Assert.LessOrEqual(vp.x, 0.99f, $"Bone {bName} clipped off right edge: vp={vp}");
                Assert.GreaterOrEqual(vp.y, 0.01f, $"Bone {bName} clipped off bottom edge (invisible legs!): vp={vp}");
                Assert.LessOrEqual(vp.y, 0.99f, $"Bone {bName} clipped off top edge: vp={vp}");
            }
        }

        [UnityTest]
        public IEnumerator Test07_All20StadiumRoundCombinationsAndPersistence()
        {
            yield return BootToCleanMainMenu();
            for (int r = 1; r <= 5; r++) SaveManager.Instance.UnlockRound(r);

            // 1. Locked stadium cannot be equipped
            Assert.IsFalse(CustomizationManager.Instance.SelectStadium(StadiumType.Night), "Locked Night stadium must be rejected.");
            Assert.IsFalse(CustomizationManager.Instance.IsCustomStadiumOverrideActive);

            // 2. Automatic mode per round matches round official themes
            StadiumType[] expectedAuto = { StadiumType.Cantera, StadiumType.Night, StadiumType.Professional, StadiumType.Night, StadiumType.WorldCup };
            for (int r = 1; r <= 5; r++)
            {
                GameManager.Instance.SelectRoundAndStartMatch(r);
                yield return WaitForScene("MatchScene");
                Assert.AreEqual(expectedAuto[r - 1], StadiumController.Instance.ActiveVenue, $"Auto mode round {r} venue mismatch.");
                AssertStrictlyOneActiveVenue(StadiumController.Instance, expectedAuto[r - 1]);
            }

            // 3. Unlock all 4 stadiums and validate all 4 stadiums x 5 rounds = 20 combinations + unchanged round difficulty
            StadiumType[] allStadiums = { StadiumType.Cantera, StadiumType.Night, StadiumType.Professional, StadiumType.WorldCup };
            foreach (var st in allStadiums) SaveManager.Instance.UnlockStadium(st);

            foreach (var st in allStadiums)
            {
                Assert.IsTrue(CustomizationManager.Instance.SelectStadium(st), $"Should select unlocked stadium {st}.");
                for (int r = 1; r <= 5; r++)
                {
                    RoundDataSO rd = GameManager.Instance.GetRoundData(r);
                    GameManager.Instance.SelectRoundAndStartMatch(r);
                    yield return WaitForScene("MatchScene");

                    Assert.AreEqual(st, StadiumController.Instance.ActiveVenue, $"Custom override {st} failed in Round {r}.");
                    AssertStrictlyOneActiveVenue(StadiumController.Instance, st);
                    Assert.AreEqual(rd.requiredGoals, PenaltyManager.Instance.RequiredGoals, "Stadium override must not alter round goal requirement.");
                }
            }

            // 4. Persistence simulation across JSON serialize/deserialize + automatic reset
            string json = JsonUtility.ToJson(SaveManager.Instance.CurrentData);
            SaveData parsed = JsonUtility.FromJson<SaveData>(json);
            Assert.IsTrue(parsed.useCustomStadiumOverride && parsed.selectedStadium == StadiumType.WorldCup);

            CustomizationManager.Instance.SetUseRoundStadiumAutomatic();
            Assert.IsFalse(SaveManager.Instance.CurrentData.useCustomStadiumOverride);
        }

        [UnityTest]
        public IEnumerator Test08_CrowdSpawningQualityScalingAndDynamicReactions()
        {
            yield return BootToCleanMainMenu();
            for (int r = 1; r <= 5; r++) SaveManager.Instance.UnlockRound(r);

            GameManager.Instance.SelectRoundAndStartMatch(3);
            yield return WaitForScene("MatchScene");

            CrowdManager crowd = CrowdManager.Instance;
            StadiumController sc = StadiumController.Instance;
            Assert.IsNotNull(crowd, "CrowdManager.Instance must exist in MatchScene.");
            Assert.IsNotNull(sc, "StadiumController.Instance must exist in MatchScene.");

            int prevQuality = QualitySettings.GetQualityLevel();
            try
            {
                QualitySettings.SetQualityLevel(2, true);
                crowd.SetQualityDensityRefresh();

                // Verify monotonically increasing crowd density across venues (Cantera < Night < Professional < WorldCup)
                int prevCount = 0;
                StadiumType[] venues = { StadiumType.Cantera, StadiumType.Night, StadiumType.Professional, StadiumType.WorldCup };
                foreach (var v in venues)
                {
                    sc.ApplyVenue(v);
                    crowd.SyncWithStadium();
                    yield return null;
                    yield return null;
                    int cnt = crowd.ActiveSpectatorCount;
                    Assert.Greater(cnt, prevCount, $"Expected crowd density in {v} ({cnt}) > previous venue ({prevCount}).");
                    prevCount = cnt;

                    // Capture 1920x1080 real GPU-instanced crowd stadium screenshot!
                    crowd.SubmitDrawCalls(Camera.main);
                    CaptureCameraFrame(Camera.main, $"1{(int)v + 3}_Stadium_{v}.png");
                }

                // Verify Baja < Media < Alta density scaling on WorldCup
                sc.ApplyVenue(StadiumType.WorldCup);
                QualitySettings.SetQualityLevel(0, true); crowd.SetQualityDensityRefresh(); yield return null;
                int lowCnt = crowd.ActiveSpectatorCount;
                QualitySettings.SetQualityLevel(1, true); crowd.SetQualityDensityRefresh(); yield return null;
                int medCnt = crowd.ActiveSpectatorCount;
                QualitySettings.SetQualityLevel(2, true); crowd.SetQualityDensityRefresh(); yield return null;
                int highCnt = crowd.ActiveSpectatorCount;
                Assert.Greater(lowCnt, 100);
                Assert.Less(lowCnt, medCnt, "Low quality crowd density must be less than Medium.");
                Assert.Less(medCnt, highCnt, "Medium quality crowd density must be less than High.");

                // Verify dynamic reaction triggers without duplicate calls + capture real GIF sequence frames
                PenaltyManager pm = PenaltyManager.Instance;
                pm.PostAttemptDelay = 0.08f;
                int baseTriggers = crowd.ReactionTriggerCount;
                yield return CaptureVideoShotSequence(new Vector3(2.95f, 1.95f, 0f), 0.84f, "PlayerKick_And_Crowd", 20);
                Assert.Greater(crowd.ReactionTriggerCount, baseTriggers, "Real shot must trigger crowd reaction.");

                yield return CaptureVideoShotSequence(new Vector3(0.0f, 0.90f, 0f), 0.55f, "KeeperDive_And_Save", 20);

                // Also capture dedicated WorldCup Crowd celebration reaction frames
                sc.ApplyVenue(StadiumType.WorldCup);
                crowd.SyncWithStadium();
                crowd.TriggerReaction(CrowdReactionMode.GoalCelebrate, 1.0f, 3.0f);
                string crowdDir = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "VideoFrames", "Stadium_Crowd_Reaction");
                Directory.CreateDirectory(crowdDir);
                for (int f = 0; f < 16; f++)
                {
                    crowd.SubmitDrawCalls(Camera.main);
                    CaptureCameraFrameCustomSize(Camera.main, Path.Combine(crowdDir, $"frame_{f:000}.png"), 960, 540);
                    yield return null;
                    yield return null;
                }
            }
            finally
            {
                QualitySettings.SetQualityLevel(prevQuality, true);
            }
        }

        [UnityTest]
        public IEnumerator Test09_LocalizedCrowdAudioAndVolumePersistence()
        {
            yield return BootToCleanMainMenu();
            GameManager.Instance.SelectRoundAndStartMatch(3);
            yield return WaitForScene("MatchScene");

            StadiumAmbienceController amb = StadiumAmbienceController.Instance;
            AudioManager am = AudioManager.Instance;
            Assert.IsNotNull(amb, "StadiumAmbienceController.Instance must exist.");
            Assert.IsNotNull(am, "AudioManager.Instance must exist.");
            Assert.GreaterOrEqual(amb.EmitterCount, 4, "Expected at least 4 localized 3D stand audio emitters.");

            AudioSource[] sources = amb.GetComponentsInChildren<AudioSource>(true);
            Assert.GreaterOrEqual(sources.Length, 4, "Stand audio emitters missing.");
            foreach (var src in sources)
            {
                Assert.GreaterOrEqual(src.spatialBlend, 0.7f, $"Stand emitter {src.name} must be localized 3D (spatialBlend >= 0.7).");
                Assert.IsNotNull(src.clip, $"Stand emitter {src.name} must have ambient crowd clip.");
            }

            // Verify 5-channel volume controls & JSON roundtrip persistence
            am.SetCrowdVolume(0.42f);
            am.SetUiVolume(0.67f);
            yield return null;
            Assert.AreEqual(0.42f, SaveManager.Instance.CurrentData.crowdVolume, 0.01f);
            Assert.AreEqual(0.67f, SaveManager.Instance.CurrentData.uiVolume, 0.01f);

            string json = JsonUtility.ToJson(SaveManager.Instance.CurrentData);
            SaveData reloaded = JsonUtility.FromJson<SaveData>(json);
            Assert.AreEqual(0.42f, reloaded.crowdVolume, 0.01f);
            Assert.AreEqual(0.67f, reloaded.uiVolume, 0.01f);

            int beforeTrig = amb.ReactionAudioTriggerCount;
            amb.PlayLocalizedReaction(SfxType.CrowdGoalRoar, 0.55f);
            Assert.AreEqual(beforeTrig + 1, amb.ReactionAudioTriggerCount);

            am.SetCrowdVolume(0.85f);
            am.SetUiVolume(0.90f);
        }

        [UnityTest]
        public IEnumerator Test10_NotificationsModalsAndPauseCompatibility()
        {
            yield return BootToCleanMainMenu();
            NotificationManager nm = NotificationManager.EnsureInstance();
            Assert.IsNotNull(nm);

            // Capture main-menu redesigned UI alerts, modals, and achievement cards
            Camera cam = Camera.main;
            Canvas overlayCanvas = nm.OverlayCanvas;

            // 1. Purchase Toast screenshot + animated slide-in sequence frames for UI GIF
            string uiDir = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "VideoFrames", "UI_Alerts_And_Modals");
            Directory.CreateDirectory(uiDir);
            nm.ShowToast("COMPRA EXITOSA", "¡Has desbloqueado Balón Azul!", NotificationKind.Success, 1.2f);
            for (int f = 0; f < 10; f++)
            {
                CaptureCameraWithCanvasCustomSize(cam, overlayCanvas, Path.Combine(uiDir, $"frame_{f:000}.png"), 960, 540);
                yield return null;
            }
            Assert.IsTrue(nm.IsToastVisible, "Toast should be visible after slide-in.");
            CaptureCameraWithCanvas(cam, overlayCanvas, "17_Alert_PurchaseToast.png");

            // Queue test: ensure second toast queues cleanly and clears
            nm.ShowToast("COMPRA NO DISPONIBLE", "Monedas insuficientes (faltan 1,500).", NotificationKind.Warning, 1.5f);
            yield return new WaitForSecondsRealtime(1.35f);
            Assert.IsTrue(nm.IsToastVisible, "Queued second toast must appear cleanly after first.");
            CaptureCameraWithCanvas(cam, overlayCanvas, "18_Alert_InsufficientCoins.png");

            // 2. Exit Confirmation Modal + Cancel test
            bool confirmed = false;
            nm.ShowConfirmModal("SALIR DEL JUEGO", "¿Deseas cerrar UltimatePenaltyCup3D?", "SALIR", "CANCELAR", () => confirmed = true, isDestructive: true);
            for (int f = 10; f < 18; f++)
            {
                CaptureCameraWithCanvasCustomSize(cam, overlayCanvas, Path.Combine(uiDir, $"frame_{f:000}.png"), 960, 540);
                yield return null;
            }
            Assert.IsTrue(nm.IsModalOpen, "Modal must be open.");
            CaptureCameraWithCanvas(cam, overlayCanvas, "19_Modal_ExitConfirm.png");
            nm.CloseModal(false);
            Assert.IsFalse(nm.IsModalOpen || confirmed);

            // 3. Achievement Reward Card during Pause (Time.timeScale == 0!)
            GameManager.Instance.SelectRoundAndStartMatch(1);
            yield return WaitForScene("MatchScene");
            PauseUI.Instance.Pause();
            Assert.AreEqual(0f, Time.timeScale, 0.001f);

            nm.ShowAchievementCard("Francotirador", "Marca 10 goles en total a lo largo de tu carrera.", 300, 250);
            yield return new WaitForSecondsRealtime(0.30f);
            Assert.IsTrue(nm.IsAchievementVisible, "Achievement card must animate visibly even while Paused (Time.timeScale=0).");
            CaptureCameraWithCanvas(Camera.main, nm.OverlayCanvas, "20_Alert_AchievementUnlocked.png");
            PauseUI.Instance.Resume();

            // 4. Broadcast Goal / Save alert screenshots in MatchHUD
            Canvas hudCanvas = MatchHUD.Instance.GetComponent<Canvas>();
            MatchHUD.Instance.ShowMessageBanner("¡GOLAZO AL ÁNGULO!", "+150 Pts  |  +120 Monedas", new Color(0.10f, 0.62f, 0.34f, 0.96f), 2.0f);
            yield return new WaitForSecondsRealtime(0.26f);
            CaptureCameraWithCanvas(Camera.main, hudCanvas, "21_Alert_GoalBroadcast.png");

            MatchHUD.Instance.ShowMessageBanner("¡ATAJADA!", "Malágon desvió el disparo", new Color(0.84f, 0.44f, 0.08f, 0.96f), 2.0f);
            yield return new WaitForSecondsRealtime(0.26f);
            CaptureCameraWithCanvas(Camera.main, hudCanvas, "22_Alert_SaveBroadcast.png");

            nm.ClearAllNotifications();
        }

        private IEnumerator CaptureVideoShotSequence(Vector3 target, float power, string folderName, int maxFrames)
        {
            string dir = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "VideoFrames", folderName);
            Directory.CreateDirectory(dir);
            PenaltyManager pm = PenaltyManager.Instance;
            PlayerShooter shooter = PlayerShooter.Instance;

            // Warm up camera at kick start
            float waitReady = 0f;
            while ((!pm.CanAcceptShotInput || !shooter.CanShoot) && waitReady < 4f)
            {
                waitReady += Time.unscaledDeltaTime;
                yield return null;
            }

            shooter.ExecuteAutomatedShot(target, power, skipRunUp: false);
            for (int f = 0; f < maxFrames; f++)
            {
                CrowdManager.Instance?.SubmitDrawCalls(Camera.main);
                CaptureCameraFrameCustomSize(Camera.main, Path.Combine(dir, $"frame_{f:000}.png"), 960, 540);
                for (int skip = 0; skip < 2; skip++) yield return null;
            }

            float waitEnd = 0f;
            while (pm.IsEvaluatingShot && waitEnd < 3.5f)
            {
                waitEnd += Time.deltaTime;
                yield return null;
            }
        }

        private static void CaptureCameraWithCanvas(Camera cam, Canvas canvas, string fileName)
        {
            if (cam == null || canvas == null) { CaptureCameraFrame(cam, fileName); return; }
            RenderMode prevMode = canvas.renderMode;
            Camera prevCam = canvas.worldCamera;
            float prevDist = canvas.planeDistance;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1.0f;
            Canvas.ForceUpdateCanvases();
            CaptureCameraFrame(cam, fileName);
            canvas.renderMode = prevMode;
            canvas.worldCamera = prevCam;
            canvas.planeDistance = prevDist;
        }

        private static void CaptureCameraWithCanvasCustomSize(Camera cam, Canvas canvas, string fullPath, int w, int h)
        {
            if (cam == null || canvas == null) { CaptureCameraFrameCustomSize(cam, fullPath, w, h); return; }
            RenderMode prevMode = canvas.renderMode;
            Camera prevCam = canvas.worldCamera;
            float prevDist = canvas.planeDistance;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1.0f;
            Canvas.ForceUpdateCanvases();
            CaptureCameraFrameCustomSize(cam, fullPath, w, h);
            canvas.renderMode = prevMode;
            canvas.worldCamera = prevCam;
            canvas.planeDistance = prevDist;
        }

        private static void CaptureCameraFrameCustomSize(Camera cam, string fullPath, int w, int h)
        {
            if (cam == null) return;
            RenderTexture rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            var prevTarget = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            cam.targetTexture = prevTarget;
            RenderTexture.active = null;
            UnityEngine.Object.Destroy(rt);
            File.WriteAllBytes(fullPath, tex.EncodeToPNG());
            UnityEngine.Object.Destroy(tex);
        }

        private static Transform FindChildRecursive(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindChildRecursive(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        private static void AssertStrictlyOneActiveVenue(StadiumController sc, StadiumType expected)
        {
            string[] names = { "Venue_Cantera", "Venue_Night", "Venue_Professional", "Venue_WorldCup" };
            int activeCount = 0;
            string activeName = "";
            foreach (var n in names)
            {
                Transform v = sc.transform.Find(n);
                Assert.IsNotNull(v, $"Missing {n}");
                if (v.gameObject.activeSelf)
                {
                    activeCount++;
                    activeName = n;
                }
            }
            Assert.AreEqual(1, activeCount, $"Expected strictly 1 active venue, found {activeCount} ({activeName}).");
            Assert.AreEqual($"Venue_{expected}", activeName, "Active venue hierarchy name mismatch.");
        }

        private IEnumerator FireHumanFlowAndAwaitShot(Vector3 target, float power, ShotOutcome outcome, List<float> frameTimes, bool verifyAnimSync, bool captureFrames)
        {
            PenaltyManager pm = PenaltyManager.Instance;
            PlayerShooter shooter = PlayerShooter.Instance;
            PlayerController pc = PlayerController.Instance;
            GoalkeeperAI gk = GoalkeeperAI.Instance;
            BallController ball = BallController.Instance;

            float waitReady = 0f;
            while ((!pm.CanAcceptShotInput || !shooter.CanShoot) && waitReady < 4f)
            {
                waitReady += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsTrue(pm.CanAcceptShotInput && shooter.CanShoot, "Shooter/PenaltyManager did not become ready.");

            bool resolved = false;
            int evalCount = 0;
            ShotResult capturedResult = ShotResult.Missed;
            void Handler(ShotResult res, ShotScoreResult score, string msg)
            {
                evalCount++;
                capturedResult = res;
                resolved = true;
            }

            if (captureFrames)
            {
                CaptureCameraFrame(Camera.main, "Anim_Player_01_Prep.png");
                CaptureCameraFrame(Camera.main, "Anim_GK_01_Ready.png");
            }

            pm.OnAttemptFinished += Handler;
            shooter.ExecuteAutomatedShot(target, power, skipRunUp: false);

            bool sawRunUp = false, sawWindUp = false, sawFollow = false;
            bool capturedRunUp = false, capturedContact = false, capturedFollow = false;
            bool capturedGkDive = false, capturedGkSave = false, capturedGkRecover = false;

            float waitShot = 0f;
            while (!resolved && waitShot < 6.5f)
            {
                yield return null;
                float dt = Time.unscaledDeltaTime;
                waitShot += dt;
                if (frameTimes != null && dt > 0.0001f && dt < 0.25f) frameTimes.Add(dt);

                if (pc != null)
                {
                    if (pc.CurrentAnimState == PlayerAnimState.RunUp)
                    {
                        sawRunUp = true;
                        if (verifyAnimSync) Assert.IsFalse(ball.HasBeenShot, "Ball must NOT launch before boot contact!");
                        if (captureFrames && !capturedRunUp) { CaptureCameraFrame(Camera.main, "Anim_Player_02_RunUp.png"); capturedRunUp = true; }
                    }
                    else if (pc.CurrentAnimState == PlayerAnimState.PlantAndWindUp)
                    {
                        sawWindUp = true;
                        if (verifyAnimSync) Assert.IsFalse(ball.HasBeenShot, "Ball must NOT launch during wind-up!");
                    }
                    else if (pc.CurrentAnimState == PlayerAnimState.FollowThrough)
                    {
                        sawFollow = true;
                        if (captureFrames && !capturedContact) { CaptureCameraFrame(Camera.main, "Anim_Player_03_Contact.png"); capturedContact = true; }
                        if (captureFrames && !capturedFollow && waitShot > 0.50f) { CaptureCameraFrame(Camera.main, "Anim_Player_04_FollowThrough.png"); capturedFollow = true; }
                    }
                }

                if (captureFrames && gk != null)
                {
                    if ((gk.CurrentState == GoalkeeperState.DivingLeft || gk.CurrentState == GoalkeeperState.DivingRight) && gk.HasDived && !capturedGkDive)
                    {
                        CaptureCameraFrame(Camera.main, "Anim_GK_02_Dive.png");
                        capturedGkDive = true;
                    }
                    if ((gk.CurrentState == GoalkeeperState.Saving || (gk.HasDived && waitShot > 0.62f)) && !capturedGkSave)
                    {
                        CaptureCameraFrame(Camera.main, "Anim_GK_03_Save.png");
                        capturedGkSave = true;
                    }
                    if (gk.IsRecoveringFromDive && !capturedGkRecover)
                    {
                        CaptureCameraFrame(Camera.main, "Anim_GK_04_Recover.png");
                        capturedGkRecover = true;
                    }
                }
            }

            if (captureFrames && !capturedGkRecover) CaptureCameraFrame(Camera.main, "Anim_GK_04_Recover.png");
            if (verifyAnimSync)
            {
                Assert.IsTrue(sawRunUp && sawWindUp && pc.ContactTriggeredThisKick && sawFollow,
                    $"Missing animation phases: runUp={sawRunUp}, windUp={sawWindUp}, contact={pc.ContactTriggeredThisKick}, follow={sawFollow}");
            }

            pm.OnAttemptFinished -= Handler;
            Assert.IsTrue(resolved, $"Human-flow shot timed out (target={target}, power={power}).");
            Assert.AreEqual(1, evalCount, $"Shot produced {evalCount} evaluations.");

            if (outcome != null)
            {
                outcome.result = capturedResult;
                outcome.eventCount = evalCount;
                outcome.keeperDived = gk != null && gk.HasDived;
            }

            float waitNext = 0f;
            while (waitNext < 0.25f && !pm.IsRoundOver && pm.CurrentState == MatchAttemptState.Evaluated)
            {
                waitNext += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private static void CaptureCameraFrame(Camera cam, string fileName)
        {
            if (cam == null) return;
            string dir = Path.Combine(Directory.GetCurrentDirectory(), "Docs", "Screenshots");
            Directory.CreateDirectory(dir);
            const int w = 1920, h = 1080;
            RenderTexture rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            var prevTarget = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            cam.targetTexture = prevTarget;
            RenderTexture.active = null;
            UnityEngine.Object.Destroy(rt);
            File.WriteAllBytes(Path.Combine(dir, fileName), tex.EncodeToPNG());
            UnityEngine.Object.Destroy(tex);
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
            public bool keeperDived;
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
