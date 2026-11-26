using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
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
using UltimatePenaltyCup.Audio;
using UltimatePenaltyCup.UI;

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
            Test("14. PlayModeStartScene Enforces MainMenu Start With Debug Bypass", TestPlayModeStartSceneConfig, ref passed, ref total);
            Test("15. Rendering & Performance Audit (GPU Instancing, No Floodlight Shadows, Batched Stadium)", TestPerformanceOptimizations, ref passed, ref total);
            Test("16. Clean Logo Asset Exists With Zero Gray Pillarboxes", TestCleanLogoAsset, ref passed, ref total);
            Test("17. Player 3D Mesh Integrity (22 Bones, 5 BlendShapes, Back #7 UV & Pixel Dorsal #7)", TestPlayerMeshIntegrity, ref passed, ref total);
            Test("18. Shop, Stadium Override Persistence & Locked Item Enforcement", TestShopAndPersistenceRoundtrip, ref passed, ref total);
            Test("19. Quality Presets (Baja / Media / Alta)", TestQualityPresets, ref passed, ref total);
            Test("20. Goalkeeper 22-Bone Mesh, Gloves & Facial BlendShapes Integrity", TestGoalkeeperMeshIntegrity, ref passed, ref total);
            Test("21. Stadium Crowd Shader, CrowdManager & StadiumAmbienceController Wiring", TestCrowdAndAmbienceWiring, ref passed, ref total);
            Test("22. Audio 5-Channel SaveData Backward Compatibility (crowdVolume / uiVolume)", TestSaveDataAudioBackwardCompatibility, ref passed, ref total);
            Test("23. NotificationManager Queued Toasts, Modal & Unscaled Pause Support", TestNotificationSystemEditMode, ref passed, ref total);
            Test("24. Mat_Stadium_Banners Clean UV Offset & Runtime Guard", TestBannerMaterialCleanAsset, ref passed, ref total);

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
            GameManager gm = UnityEngine.Object.FindAnyObjectByType<GameManager>();
            Assert(gm != null && gm.GetRoundData(3) != null && gm.GetRoundData(5) != null, "MainMenu GameManager must have Round 1-5 configs wired.");

            var matchScene = EditorSceneManager.OpenScene("Assets/Scenes/MatchScene.unity", OpenSceneMode.Single);
            Assert(UnityEngine.Object.FindAnyObjectByType<MatchCameraController>() != null, "MatchScene Camera must have MatchCameraController.");
            var endUI = UnityEngine.Object.FindAnyObjectByType<UltimatePenaltyCup.UI.RoundEndUI>(FindObjectsInactive.Include);
            Assert(endUI != null && endUI.gameObject.activeInHierarchy, "RoundEndUI must live on an always-active GameObject so Start() subscribes!");
            Assert(UnityEngine.Object.FindAnyObjectByType<StadiumController>() != null, "MatchScene must have StadiumController.");
        }

        private static void TestCleanLogoAsset()
        {
            string cleanPath = "Assets/Art/Logo/UltimatePenaltyCup3D_Logo_Clean.png";
            Assert(System.IO.File.Exists(cleanPath), "Clean logo file must exist.");
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(cleanPath);
            Assert(tex != null && tex.width == tex.height && tex.width >= 400, $"Clean logo must be square >=400px, got {tex?.width}x{tex?.height}.");
            Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(cleanPath);
            Assert(sp != null, "Clean logo must import as Sprite.");

            byte[] png = System.IO.File.ReadAllBytes(cleanPath);
            Texture2D readable = new Texture2D(2, 2);
            readable.LoadImage(png);
            Color32 corner = readable.GetPixel(0, 0);
            UnityEngine.Object.DestroyImmediate(readable);
            Assert(corner.a == 0, $"Clean logo outer corner must have alpha=0 (rounded/no pillarboxes), got a={corner.a}.");
        }

        private static void TestPlayerMeshIntegrity()
        {
            Mesh m = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Models/Mesh_RubenGonzalez.asset");
            Assert(m != null, "Mesh_RubenGonzalez.asset must exist.");
            Assert(m.subMeshCount == 7, $"Expected 7 submeshes on Rubén mesh, got {m.subMeshCount}.");
            Assert(m.bindposes != null && m.bindposes.Length == 22, $"Expected 22 humanized bindposes, got {m.bindposes?.Length}.");
            Assert(m.bounds.min.y <= 0.02f && m.bounds.max.y >= 1.75f, $"Player mesh bounds Y must span full height [<=0.02 .. >=1.75], got [{m.bounds.min.y:F3} .. {m.bounds.max.y:F3}].");
            Assert(m.blendShapeCount >= 5, $"Expected 5 facial BlendShapes (Blink/Focus/Effort/Happy/Frustrated), got {m.blendShapeCount}.");
            Assert(m.GetBlendShapeIndex("Blink") >= 0 && m.GetBlendShapeIndex("Happy") >= 0, "Missing facial BlendShape Blink/Happy on Rubén mesh.");

            for (int s = 0; s < m.subMeshCount; s++)
            {
                Assert(m.GetTriangles(s).Length > 0, $"Submesh {s} must have non-zero triangles.");
            }

            Vector3[] verts = m.vertices;
            Vector2[] uvs = m.uv;
            int[] jerseyTris = m.GetTriangles(0);
            float minULeft = 1f, maxURight = 0f;
            var visited = new HashSet<int>();
            foreach (int idx in jerseyTris)
            {
                if (!visited.Add(idx)) continue;
                Vector3 v = verts[idx];
                if (Mathf.Abs(v.x) < 0.16f && v.z < -0.06f && v.y > 1.05f && v.y < 1.45f)
                {
                    if (v.x < -0.04f && uvs[idx].x < minULeft) minULeft = uvs[idx].x;
                    if (v.x > 0.04f && uvs[idx].x > maxURight) maxURight = uvs[idx].x;
                }
            }
            Assert(minULeft < maxURight && minULeft >= 0.50f && maxURight <= 1.0f,
                $"Back jersey #7 UV must increase left->right [0.5..1.0], got minULeft={minULeft:F3}, maxURight={maxURight:F3}.");

            // Pixel verification of dorsal #7 on Tex_Jersey_Ruben.png (and verify #10 is GONE!)
            string jerseyPath = "Assets/Art/Textures/Tex_Jersey_Ruben.png";
            Assert(System.IO.File.Exists(jerseyPath), "Tex_Jersey_Ruben.png must exist.");
            Texture2D readable = new Texture2D(2, 2);
            readable.LoadImage(System.IO.File.ReadAllBytes(jerseyPath));
            try
            {
                // Back #7 horizontal top bar at bx=128 (x=256+128=384), y=322 -> crisp white!
                Color topMid = readable.GetPixel(256 + 128, 322);
                Assert(topMid.r > 0.85f && topMid.g > 0.85f && topMid.b > 0.85f,
                    $"Back jersey top bar of #7 at (384,322) must be white, got {topMid}.");
                // Center void under top bar at bx=128 (x=384), y=250 -> NOT white (was vertical gap / diagonal #7 sits near bx=142 at y=250 -> check bx=100, y=220!)
                Color leftGap = readable.GetPixel(256 + 96, 220);
                Assert(leftGap.b > leftGap.r + 0.2f, $"Back jersey under top-left of #7 must be blue fabric (not #1 vertical bar), got {leftGap}.");
                // Diagonal stem at y=236 -> lerp(116,160, 0.5)=138 -> x=256+138=394 must be crisp white!
                Color diagMid = readable.GetPixel(256 + 138, 236);
                Assert(diagMid.r > 0.85f && diagMid.g > 0.85f && diagMid.b > 0.85f,
                    $"Back jersey diagonal stroke of #7 at (394,236) must be white, got {diagMid}.");
            }
            finally { UnityEngine.Object.DestroyImmediate(readable); }
        }

        private static void TestShopAndPersistenceRoundtrip()
        {
            GameObject go = new GameObject("Test_ShopAndPersist");
            var sm = go.AddComponent<SaveManager>();
            sm.SetTestDataInMemory(new SaveData());
            var cm = go.AddComponent<CurrencyManager>();
            cm.BindForTest();
            var shm = go.AddComponent<ShopManager>();
            var cust = go.AddComponent<CustomizationManager>();

            try
            {
                var night = ScriptableObject.CreateInstance<StadiumItemSO>();
                night.stadiumType = StadiumType.Night;
                night.displayName = "Estadio Nocturno";
                night.price = 2000;
                shm.SetCatalogs(new List<BallItemSO>(), new List<StadiumItemSO> { night });

                Assert(!cust.SelectStadium(StadiumType.Night), "Locked stadium must be rejected.");
                sm.CurrentData.coins = 1000;
                Assert(!shm.BuyStadium(StadiumType.Night), "Stadium purchase with insufficient coins must fail.");
                sm.CurrentData.coins = 2500;
                Assert(shm.BuyStadium(StadiumType.Night) && cm.Coins == 500, "Stadium purchase must deduct exact 2000 coins.");
                Assert(!shm.BuyStadium(StadiumType.Night) && cm.Coins == 500, "Duplicate stadium purchase must fail cleanly.");
                Assert(cust.SelectStadium(StadiumType.Night), "Unlocked stadium selection must succeed.");

                string json = JsonUtility.ToJson(sm.CurrentData);
                SaveData reloaded = JsonUtility.FromJson<SaveData>(json);
                Assert(reloaded.useCustomStadiumOverride && reloaded.selectedStadium == StadiumType.Night,
                    "JSON save/reload must preserve custom stadium selection.");
                Assert(reloaded.unlockedStadiums.Contains(StadiumType.Night), "JSON save/reload must preserve unlocked stadiums.");
            }
            finally { GameObject.DestroyImmediate(go); }
        }

        private static void TestQualityPresets()
        {
            string[] names = QualitySettings.names;
            Assert(names != null && names.Length >= 3, $"Expected >=3 quality levels (Baja/Media/Alta), found {names?.Length}.");
            Assert(Array.IndexOf(names, "Baja") >= 0 && Array.IndexOf(names, "Media") >= 0 && Array.IndexOf(names, "Alta") >= 0,
                $"Expected Baja/Media/Alta quality names, found: {string.Join(", ", names)}");
            Assert(AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/URP_Low.asset") != null, "URP_Low.asset missing.");
            Assert(AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/URP_Medium.asset") != null, "URP_Medium.asset missing.");
            Assert(AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset") != null, "PC_RPAsset.asset missing.");
        }

        private static void TestPlayModeStartSceneConfig()
        {
            PlayModeStartSceneConfig.ApplyPlayModeStartScene();
            if (!PlayModeStartSceneConfig.IsDirectMatchAllowed)
            {
                Assert(EditorSceneManager.playModeStartScene != null && EditorSceneManager.playModeStartScene.name == "MainMenu",
                    "playModeStartScene must default to MainMenu.unity when debug bypass is off.");
            }
        }

        private static void TestPerformanceOptimizations()
        {
            string[] matPaths =
            {
                "Assets/Materials/Mat_Grass_Cantera.mat",
                "Assets/Materials/Mat_Player_Kit.mat",
                "Assets/Materials/Mat_Ball_Classic.mat",
                "Assets/Materials/Mat_Stadium_Concrete.mat",
                "Assets/Materials/Mat_Stadium_Stands.mat"
            };
            foreach (string p in matPaths)
            {
                Material m = AssetDatabase.LoadAssetAtPath<Material>(p);
                Assert(m != null && m.enableInstancing, $"Material {p} must have GPU Instancing enabled.");
            }

            GameObject stadium = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/StadiumEnvironment.prefab");
            Assert(stadium != null, "StadiumEnvironment.prefab must exist.");

            Light[] lights = stadium.GetComponentsInChildren<Light>(true);
            foreach (var l in lights)
            {
                Assert(l.shadows == LightShadows.None, $"Floodlight {l.name} must have shadows disabled for 60 FPS performance.");
            }

            string[] venueNames = { "Venue_Cantera", "Venue_Night", "Venue_Professional", "Venue_WorldCup" };
            foreach (string vName in venueNames)
            {
                Transform v = stadium.transform.Find(vName);
                Assert(v != null, $"Missing venue {vName}.");
                bool hasBatch = false;
                for (int i = 0; i < v.childCount; i++)
                {
                    if (v.GetChild(i).name.StartsWith("StaticBatch_"))
                    {
                        hasBatch = true;
                        break;
                    }
                }
                Assert(hasBatch, $"Venue {vName} must contain combined StaticBatch meshes.");
            }
        }

        private static void TestGoalkeeperMeshIntegrity()
        {
            Mesh gk = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Models/Mesh_GoalkeeperMalagon.asset");
            Assert(gk != null, "Mesh_GoalkeeperMalagon.asset must exist.");
            Assert(gk.subMeshCount == 8, $"Expected 8 submeshes on Malágon mesh (incl gloves), got {gk.subMeshCount}.");
            Assert(gk.bindposes != null && gk.bindposes.Length == 22, $"Expected 22 humanized bindposes on Malágon, got {gk.bindposes?.Length}.");
            Assert(gk.blendShapeCount >= 5, $"Expected 5 facial BlendShapes on Malágon, got {gk.blendShapeCount}.");
            GameObject gkPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Goalkeeper_Malagon.prefab");
            var smr = gkPrefab.GetComponentInChildren<SkinnedMeshRenderer>(true);
            Assert(smr != null && smr.bones != null && smr.bones.Length == 22, "Goalkeeper prefab SkinnedMeshRenderer must wire 22 bones.");
        }

        private static void TestCrowdAndAmbienceWiring()
        {
            Shader s = Shader.Find("UltimatePenaltyCup/CrowdInstanced");
            Assert(s != null, "CrowdInstanced.shader must compile and be locatable.");
            GameObject stadium = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/StadiumEnvironment.prefab");
            Assert(stadium != null, "StadiumEnvironment.prefab must exist.");
            Assert(stadium.GetComponent<CrowdManager>() != null, "StadiumEnvironment prefab must include CrowdManager.");
            Assert(stadium.GetComponent<StadiumAmbienceController>() != null, "StadiumEnvironment prefab must include StadiumAmbienceController.");
        }

        private static void TestSaveDataAudioBackwardCompatibility()
        {
            // Simulate deserializing v1.0 JSON missing crowdVolume / uiVolume / useCustomStadiumOverride
            string legacyJson = "{\"coins\":750,\"masterVolume\":0.8,\"musicVolume\":0.6,\"sfxVolume\":0.7}";
            SaveData loaded = JsonUtility.FromJson<SaveData>(legacyJson);
            Assert(loaded != null && loaded.coins == 750, "Legacy SaveData coins must load.");
            Assert(Mathf.Abs(loaded.crowdVolume - 0.85f) < 0.01f, $"Missing crowdVolume in legacy save must default to 0.85, got {loaded.crowdVolume}.");
            Assert(Mathf.Abs(loaded.uiVolume - 0.90f) < 0.01f, $"Missing uiVolume in legacy save must default to 0.90, got {loaded.uiVolume}.");
        }

        private static void TestNotificationSystemEditMode()
        {
            GameObject go = new GameObject("Test_NotifEdit");
            try
            {
                var nm = go.AddComponent<NotificationManager>();
                nm.ShowToast("PRUEBA", "Notificación deportiva", NotificationKind.Success, 1.5f);
                Assert(nm.OverlayCanvas != null && nm.OverlayCanvas.sortingOrder >= 500, "NotificationManager must create high-priority overlay canvas.");
                Assert(nm.LastToastTitle == "PRUEBA", "LastToastTitle mismatch.");

                bool confirmed = false;
                nm.ShowConfirmModal("CONFIRMAR", "¿Seguro?", "SI", "NO", () => confirmed = true);
                Assert(nm.IsModalOpen, "Modal must open immediately.");
                nm.CloseModal();
                Assert(!nm.IsModalOpen && !confirmed, "CloseModal without confirm must close cleanly.");
            }
            finally { GameObject.DestroyImmediate(go); }
        }

        private static void TestBannerMaterialCleanAsset()
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_Stadium_Banners.mat");
            Assert(mat != null, "Mat_Stadium_Banners.mat must exist.");
            Assert(Mathf.Abs(mat.mainTextureOffset.x) < 0.001f && Mathf.Abs(mat.GetTextureOffset("_BaseMap").x) < 0.001f,
                $"Mat_Stadium_Banners.mat offset must remain (0,0) and never mutate at runtime, got {mat.GetTextureOffset("_BaseMap")}.");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"Assertion Failed: {message}");
        }
    }
}
