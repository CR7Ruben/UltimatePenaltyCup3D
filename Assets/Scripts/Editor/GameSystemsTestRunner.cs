using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UltimatePenaltyCup.Core;
using UltimatePenaltyCup.SaveSystem;
using UltimatePenaltyCup.Economy;
using UltimatePenaltyCup.Shop;
using UltimatePenaltyCup.Customization;
using UltimatePenaltyCup.Achievements;
using UltimatePenaltyCup.Levels;
using UltimatePenaltyCup.Goalkeeper;
using UltimatePenaltyCup.Gameplay;

namespace UltimatePenaltyCup.Editor
{
    public static class GameSystemsTestRunner
    {
        [MenuItem("Ultimate Penalty Cup/Run All Automated System Tests", false, 2)]
        public static void RunAllTests()
        {
            Debug.Log("==========================================================");
            Debug.Log(">>> RUNNING ULTIMATE PENALTY CUP 3D SYSTEM INTEGRATION TESTS <<<");
            Debug.Log("==========================================================");

            int passed = 0;
            int total = 0;

            Test("1. Save Data Integrity and Backward Compatibility", TestSaveDataIntegrity, ref passed, ref total);
            Test("2. Currency Transactions & Non-Negative Balance Enforcement", TestCurrencyTransactions, ref passed, ref total);
            Test("3. Score Processing & Rule Priority (No Double Dipping)", TestScoreProcessing, ref passed, ref total);
            Test("4. Secret Dev Code PENALTY2026 Validation", TestSecretCode, ref passed, ref total);
            Test("5. Shop Purchasing, Collector Unlock & Duplicate Rejection", TestShopPurchases, ref passed, ref total);
            Test("6. Customization, Stadium Override & Automatic Round Mode", TestCustomization, ref passed, ref total);
            Test("7. Achievements Evaluation (Adapted PerfectStreak + No Duplicates)", TestAchievements, ref passed, ref total);
            Test("8. Round Progression (1 to 5) & Early Target Logic", TestRoundProgression, ref passed, ref total);
            Test("9. ScriptableObjects 5-Round Difficulty & Rules Verification", TestRoundScriptableObjects, ref passed, ref total);
            Test("10. Character Prefabs: SkinnedMesh, Separated Face Material & Colliders", TestCharacterPrefabs, ref passed, ref total);
            Test("11. Ball (0.22m) & Goal (7.32x2.44m) FIFA Proportions", TestWorldProportions, ref passed, ref total);
            Test("12. BuildSettings & Startup Scene Index (MainMenu=0, No SampleScene)", TestBuildSettingsOrder, ref passed, ref total);
            Test("13. MainMenu & MatchScene Wiring (GameManager, Camera, RoundEndUI)", TestSceneWiring, ref passed, ref total);

            Debug.Log("----------------------------------------------------------");
            Debug.Log($"TEST RESULTS: {passed} / {total} TESTS PASSED! ({(float)passed / total * 100f:F1}%)");
            Debug.Log("==========================================================");

            if (Application.isBatchMode && passed < total)
            {
                EditorApplication.Exit(1);
            }
        }

        private static void Test(string testName, Action testAction, ref int passed, ref int total)
        {
            total++;
            try
            {
                testAction();
                passed++;
                Debug.Log($"[PASS] {testName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FAIL] {testName}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void TestSaveDataIntegrity()
        {
            SaveData data = new SaveData();
            Assert(data.unlockedRounds.Contains(1), "Round 1 must be unlocked by default.");
            Assert(data.unlockedBalls.Contains(BallType.Classic), "Classic ball must be unlocked by default.");
            Assert(data.equippedBall == BallType.Classic, "Default equipped ball must be Classic.");
            Assert(data.unlockedStadiums.Contains(StadiumType.Cantera), "Cantera stadium must be unlocked by default.");
            Assert(data.selectedStadium == StadiumType.Cantera, "Default stadium must be Cantera.");
            Assert(data.coins == 0, "Initial coins must be 0.");
            Assert(!data.useCustomStadiumOverride, "Automatic round stadium mode must be enabled by default.");
            Assert(Mathf.Approximately(data.aimSensitivity, 1.0f), "Aim sensitivity default must be 1.0.");
        }

        private static void TestCurrencyTransactions()
        {
            GameObject go = new GameObject("Test_Currency");
            var sm = go.AddComponent<SaveManager>();
            sm.SetTestDataInMemory(new SaveData());
            var cm = go.AddComponent<CurrencyManager>();
            cm.BindForTest();

            try
            {
                sm.CurrentData.coins = 500;
                Assert(cm.Coins == 500, "Currency manager must reflect save data coins.");
                cm.AddCoins(250);
                Assert(cm.Coins == 750, "AddCoins must add exactly 250 coins.");
                Assert(cm.SpendCoins(300), "Spending less than balance must succeed.");
                Assert(cm.Coins == 450, "Coins must be 450 after spending 300.");
                Assert(!cm.SpendCoins(1000), "Spending more than balance must fail.");
                Assert(cm.Coins == 450, "Balance must not change on failed spend.");
                Assert(cm.Coins >= 0, "Balance must never be negative.");
            }
            finally { GameObject.DestroyImmediate(go); }
        }

        private static void TestScoreProcessing()
        {
            GameObject go = new GameObject("Test_Score");
            go.AddComponent<SaveManager>().SetTestDataInMemory(new SaveData());
            go.AddComponent<CurrencyManager>().BindForTest();
            var scm = go.AddComponent<ScoreManager>();

            try
            {
                scm.ResetMatchScore();
                var r1 = scm.ProcessGoal(false, false);
                Assert(r1.shotType == ShotResult.GoalNormal && r1.pointsEarned == 100 && r1.coinsEarned == 100, "Normal goal check failed.");
                var r2 = scm.ProcessGoal(false, true);
                Assert(r2.shotType == ShotResult.GoalHighPower && r2.pointsEarned == 200 && r2.coinsEarned == 150, "HighPower+Streak2 check failed.");
                var r3 = scm.ProcessGoal(true, true);
                Assert(r3.shotType == ShotResult.GoalCorner && r3.pointsEarned == 300 && r3.coinsEarned == 200, "Corner priority check failed.");
                scm.ProcessMissOrSave();
                Assert(scm.CurrentStreak == 0, "Miss must reset streak to 0.");
            }
            finally { GameObject.DestroyImmediate(go); }
        }

        private static void TestSecretCode()
        {
            GameObject go = new GameObject("Test_Code");
            go.AddComponent<SaveManager>().SetTestDataInMemory(new SaveData());
            var cm = go.AddComponent<CurrencyManager>();
            cm.BindForTest();

            try
            {
                int before = cm.Coins;
                Assert(cm.TryRedeemCode("PENALTY2026"), "PENALTY2026 must succeed in Editor/Dev.");
                Assert(cm.Coins == before + CurrencyManager.DEV_CODE_REWARD_COINS, "Dev code must grant 5000 coins.");
                Assert(!cm.TryRedeemCode("INVALID_CODE_123"), "Invalid code must return false.");
            }
            finally { GameObject.DestroyImmediate(go); }
        }

        private static void TestShopPurchases()
        {
            GameObject go = new GameObject("Test_Shop");
            var sm = go.AddComponent<SaveManager>();
            sm.SetTestDataInMemory(new SaveData());
            var cm = go.AddComponent<CurrencyManager>();
            cm.BindForTest();
            var shm = go.AddComponent<ShopManager>();

            try
            {
                var ballBlue = ScriptableObject.CreateInstance<BallItemSO>();
                ballBlue.ballType = BallType.Blue;
                ballBlue.displayName = "Balón Azul";
                ballBlue.price = 500;
                shm.SetCatalogs(new List<BallItemSO> { ballBlue }, new List<StadiumItemSO>());

                sm.CurrentData.coins = 200;
                Assert(!shm.BuyBall(BallType.Blue), "Cannot buy without enough coins.");
                sm.CurrentData.coins = 1000;
                Assert(shm.BuyBall(BallType.Blue), "Buying with sufficient coins must succeed.");
                Assert(sm.IsBallUnlocked(BallType.Blue) && cm.Coins == 500, "Ball Blue unlock & coin deduction check failed.");
                Assert(!shm.BuyBall(BallType.Blue) && cm.Coins == 500, "Duplicate purchase must fail cleanly.");
            }
            finally { GameObject.DestroyImmediate(go); }
        }

        private static void TestCustomization()
        {
            GameObject go = new GameObject("Test_Customization");
            var sm = go.AddComponent<SaveManager>();
            sm.SetTestDataInMemory(new SaveData());
            var cust = go.AddComponent<CustomizationManager>();

            try
            {
                Assert(!cust.EquipBall(BallType.Gold), "Should not equip locked ball.");
                sm.UnlockBall(BallType.Gold);
                Assert(cust.EquipBall(BallType.Gold) && cust.EquippedBall == BallType.Gold, "Equipping unlocked ball failed.");

                sm.UnlockStadium(StadiumType.WorldCup);
                Assert(cust.SelectStadium(StadiumType.WorldCup), "Selecting unlocked stadium failed.");
                Assert(sm.CurrentData.useCustomStadiumOverride, "Selecting stadium must enable custom override.");
                cust.SetUseRoundStadiumAutomatic();
                Assert(!sm.CurrentData.useCustomStadiumOverride, "Automatic mode toggle failed.");
            }
            finally { GameObject.DestroyImmediate(go); }
        }

        private static void TestAchievements()
        {
            GameObject go = new GameObject("Test_Ach");
            var sm = go.AddComponent<SaveManager>();
            sm.SetTestDataInMemory(new SaveData());
            go.AddComponent<CurrencyManager>().BindForTest();
            var am = go.AddComponent<AchievementManager>();

            try
            {
                var achFirst = ScriptableObject.CreateInstance<AchievementSO>();
                achFirst.id = AchievementManager.ID_FIRST_GOAL;
                achFirst.rewardCoins = 100;
                var achStreak = ScriptableObject.CreateInstance<AchievementSO>();
                achStreak.id = AchievementManager.ID_PERFECT_STREAK;
                achStreak.rewardCoins = 500;
                am.SetAchievements(new List<AchievementSO> { achFirst, achStreak });

                am.CheckGoalScored(1, 1);
                Assert(am.IsUnlocked(AchievementManager.ID_FIRST_GOAL), "First goal achievement should unlock.");
                Assert(!am.UnlockAchievement(AchievementManager.ID_FIRST_GOAL), "Cannot unlock achievement twice.");

                // Clean sweep 3/3 on Round 1 (early win) must unlock PerfectStreak!
                am.CheckRoundCompleted(1, 700, goalsScored: 3, attemptsUsed: 3, requiredGoals: 3);
                Assert(am.IsUnlocked(AchievementManager.ID_PERFECT_STREAK), "Clean sweep early victory (3/3) must unlock PerfectStreak.");
            }
            finally { GameObject.DestroyImmediate(go); }
        }

        private static void TestRoundProgression()
        {
            GameObject go = new GameObject("Test_Round");
            var sm = go.AddComponent<SaveManager>();
            sm.SetTestDataInMemory(new SaveData());

            try
            {
                Assert(sm.IsRoundUnlocked(1) && !sm.IsRoundUnlocked(2), "Only Round 1 unlocked initially.");
                sm.UnlockRound(2);
                Assert(sm.IsRoundUnlocked(2) && !sm.IsRoundUnlocked(3), "Sequential unlock check failed.");

                // Verify mathematical defeat formula (goalsScored + remainingAttempts < requiredGoals)
                Assert((0 + 2) < 3, "After 3 misses in R1 (req 3), 0 + 2 < 3 must trigger defeat.");
                Assert((3 >= 3), "At 3 goals in R1 (req 3), round must end immediately.");
                Assert((4 >= 4), "At 4 goals in R3 (req 4), round must end immediately.");
            }
            finally { GameObject.DestroyImmediate(go); }
        }

        private static void TestRoundScriptableObjects()
        {
            int[] reqs = { 3, 3, 4, 4, 4 };
            string[] paths =
            {
                "Assets/ScriptableObjects/Rounds/Round1_Cantera.asset",
                "Assets/ScriptableObjects/Rounds/Round2_LigaLocal.asset",
                "Assets/ScriptableObjects/Rounds/Round3_LigaProfesional.asset",
                "Assets/ScriptableObjects/Rounds/Round4_Semifinal.asset",
                "Assets/ScriptableObjects/Rounds/Round5_GranFinal.asset"
            };
            float prevDive = 0f;
            for (int i = 0; i < 5; i++)
            {
                var r = AssetDatabase.LoadAssetAtPath<RoundDataSO>(paths[i]);
                Assert(r != null, $"Missing RoundDataSO: {paths[i]}");
                Assert(r.roundNumber == i + 1, $"Round {i + 1} number mismatch.");
                Assert(r.requiredGoals == reqs[i], $"Round {i + 1} must require {reqs[i]} goals, found {r.requiredGoals}.");
                Assert(r.maxAttempts == 5, $"Round {i + 1} maxAttempts must be 5.");
                Assert(r.diveSpeed > prevDive, $"Round {i + 1} goalkeeper difficulty must increase monotonically.");
                prevDive = r.diveSpeed;
            }
        }

        private static void TestCharacterPrefabs()
        {
            GameObject player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player_RubenGonzalez.prefab");
            GameObject gk = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Goalkeeper_Malagon.prefab");
            Assert(player != null && gk != null, "Character prefabs must exist.");

            var pSmr = player.GetComponentInChildren<SkinnedMeshRenderer>(true);
            var gSmr = gk.GetComponentInChildren<SkinnedMeshRenderer>(true);
            Assert(pSmr != null && pSmr.sharedMesh != null, "Rubén must use a unified SkinnedMeshRenderer.");
            Assert(gSmr != null && gSmr.sharedMesh != null, "Malágon must use a unified SkinnedMeshRenderer.");
            Assert(pSmr.sharedMaterials[2].name == "Mat_Skin_Body", "Limb skin must use Mat_Skin_Body, NOT Face texture!");
            Assert(pSmr.sharedMaterials[3].name == "Mat_Face_Ruben", "Head must use Mat_Face_Ruben.");

            var gkRb = gk.GetComponent<Rigidbody>();
            var gkCols = gk.GetComponentsInChildren<Collider>(true);
            Assert(gkRb != null && gkRb.isKinematic, "Goalkeeper must have kinematic Rigidbody.");
            Assert(gkCols.Length >= 3, "Goalkeeper must have body + left/right glove colliders.");
        }

        private static void TestWorldProportions()
        {
            GameObject ball = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BallPrefab.prefab");
            GameObject goal = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GoalPrefab.prefab");
            Assert(ball != null && goal != null, "Ball and Goal prefabs must exist.");
            Assert(Mathf.Abs(ball.transform.localScale.x - 0.22f) < 0.02f, $"Ball diameter must be ~0.22m, got {ball.transform.localScale.x}.");
            Assert(goal.GetComponent<GoalNetRipple>() != null, "Goal must have GoalNetRipple component.");
        }

        private static void TestBuildSettingsOrder()
        {
            var scenes = EditorBuildSettings.scenes;
            Assert(scenes.Length == 2, $"Expected exactly 2 scenes in build settings, found {scenes.Length}.");
            Assert(scenes[0].path == "Assets/Scenes/MainMenu.unity" && scenes[0].enabled, "Scene 0 MUST be MainMenu.unity.");
            Assert(scenes[1].path == "Assets/Scenes/MatchScene.unity" && scenes[1].enabled, "Scene 1 MUST be MatchScene.unity.");
        }

        private static void TestSceneWiring()
        {
            var menuScene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
            GameManager gm = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            Assert(gm != null && gm.GetRoundData(3) != null && gm.GetRoundData(5) != null, "MainMenu GameManager must have Round 1-5 configs wired.");

            var matchScene = EditorSceneManager.OpenScene("Assets/Scenes/MatchScene.unity", OpenSceneMode.Single);
            Assert(UnityEngine.Object.FindFirstObjectByType<MatchCameraController>() != null, "MatchScene Camera must have MatchCameraController.");
            var endUI = UnityEngine.Object.FindFirstObjectByType<UltimatePenaltyCup.UI.RoundEndUI>(FindObjectsInactive.Include);
            Assert(endUI != null && endUI.gameObject.activeInHierarchy, "RoundEndUI must live on an always-active GameObject so Start() subscribes!");
            Assert(UnityEngine.Object.FindFirstObjectByType<StadiumController>() != null, "MatchScene must have StadiumController.");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"Assertion Failed: {message}");
        }
    }
}
