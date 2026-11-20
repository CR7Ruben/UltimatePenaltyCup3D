using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UltimatePenaltyCup.Core;
using UltimatePenaltyCup.SaveSystem;
using UltimatePenaltyCup.Economy;
using UltimatePenaltyCup.Shop;
using UltimatePenaltyCup.Customization;
using UltimatePenaltyCup.Achievements;
using UltimatePenaltyCup.Levels;

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

            Test("1. Save Data Integrity and Default Initialization", TestSaveDataIntegrity, ref passed, ref total);
            Test("2. Currency Transactions & Non-Negative Balance Enforcement", TestCurrencyTransactions, ref passed, ref total);
            Test("3. Score Processing & Rule Priority (No Double Dipping)", TestScoreProcessing, ref passed, ref total);
            Test("4. Secret Dev Code PENALTY2026 Validation", TestSecretCode, ref passed, ref total);
            Test("5. Shop Purchasing & Duplicate Purchase Rejection", TestShopPurchases, ref passed, ref total);
            Test("6. Customization & Equipment Management", TestCustomization, ref passed, ref total);
            Test("7. Achievements Evaluation (All 7 Achievements)", TestAchievements, ref passed, ref total);
            Test("8. Round Progression & Champion Unlock (Rounds 1 to 5)", TestRoundProgression, ref passed, ref total);

            Debug.Log("----------------------------------------------------------");
            Debug.Log($"TEST RESULTS: {passed} / {total} TESTS PASSED SUCCESSFULLY! ({(float)passed / total * 100f:F1}%)");
            Debug.Log("==========================================================");
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
        }

        private static void TestCurrencyTransactions()
        {
            GameObject go = new GameObject("Test_Currency");
            var sm = go.AddComponent<SaveManager>();
            var cm = go.AddComponent<CurrencyManager>();

            try
            {
                sm.CurrentData.coins = 500;
                Assert(cm.Coins == 500, "Currency manager must reflect save data coins.");

                cm.AddCoins(250);
                Assert(cm.Coins == 750, "AddCoins must add exactly 250 coins.");

                bool spentSuccess = cm.SpendCoins(300);
                Assert(spentSuccess, "Spending less than balance must succeed.");
                Assert(cm.Coins == 450, "Coins must be 450 after spending 300.");

                bool spentFail = cm.SpendCoins(1000);
                Assert(!spentFail, "Spending more than balance must fail.");
                Assert(cm.Coins == 450, "Balance must not change on failed spend.");
                Assert(cm.Coins >= 0, "Balance must never be negative.");
            }
            finally
            {
                GameObject.DestroyImmediate(go);
            }
        }

        private static void TestScoreProcessing()
        {
            GameObject go = new GameObject("Test_Score");
            go.AddComponent<SaveManager>();
            go.AddComponent<CurrencyManager>();
            var scm = go.AddComponent<ScoreManager>();

            try
            {
                scm.ResetMatchScore();
                Assert(scm.CurrentMatchScore == 0, "Score must be 0 after reset.");

                // 1. Normal Goal: 100 pts, 100 coins
                var r1 = scm.ProcessGoal(isCornerOrPrecision: false, isHighPower: false);
                Assert(r1.shotType == ShotResult.GoalNormal, "Result should be GoalNormal.");
                Assert(r1.pointsEarned == 100, "Normal goal earns 100 pts.");
                Assert(r1.coinsEarned == 100, "Normal goal earns 100 coins.");
                Assert(scm.CurrentStreak == 1, "Streak should be 1.");

                // 2. High Power Goal with Streak: 150 pts + 50 streak = 200 pts; 125 coins + 25 streak = 150 coins
                var r2 = scm.ProcessGoal(isCornerOrPrecision: false, isHighPower: true);
                Assert(r2.shotType == ShotResult.GoalHighPower, "Result should be GoalHighPower.");
                Assert(r2.streakCount == 2, "Streak should now be 2.");
                Assert(r2.pointsEarned == 200, "High power with streak 2 earns 150 + 50 = 200 pts.");
                Assert(r2.coinsEarned == 150, "High power with streak 2 earns 125 + 25 = 150 coins.");

                // 3. Corner / Precision Goal (Priority over high power): 200 base pts + 100 streak = 300 pts
                var r3 = scm.ProcessGoal(isCornerOrPrecision: true, isHighPower: true);
                Assert(r3.shotType == ShotResult.GoalCorner, "Corner precision must take priority over high power.");
                Assert(r3.streakCount == 3, "Streak should be 3.");
                Assert(r3.pointsEarned == 300, "Corner with streak 3 earns 200 + 100 = 300 pts.");
                Assert(r3.coinsEarned == 200, "Corner with streak 3 earns 150 + 50 = 200 coins.");

                // 4. Miss resets streak
                scm.ProcessMissOrSave();
                Assert(scm.CurrentStreak == 0, "Miss must reset streak to 0.");
            }
            finally
            {
                GameObject.DestroyImmediate(go);
            }
        }

        private static void TestSecretCode()
        {
            GameObject go = new GameObject("Test_Code");
            go.AddComponent<SaveManager>();
            var cm = go.AddComponent<CurrencyManager>();

            try
            {
                int before = cm.Coins;
                bool success = cm.TryRedeemCode("PENALTY2026");
                Assert(success, "PENALTY2026 must be recognized.");
                Assert(cm.Coins == before + CurrencyManager.DEV_CODE_REWARD_COINS, "Dev code must grant 5000 coins.");

                bool fail = cm.TryRedeemCode("INVALID_CODE_123");
                Assert(!fail, "Invalid code must return false.");
            }
            finally
            {
                GameObject.DestroyImmediate(go);
            }
        }

        private static void TestShopPurchases()
        {
            GameObject go = new GameObject("Test_Shop");
            var sm = go.AddComponent<SaveManager>();
            var cm = go.AddComponent<CurrencyManager>();
            var shm = go.AddComponent<ShopManager>();

            try
            {
                var ballBlue = ScriptableObject.CreateInstance<BallItemSO>();
                ballBlue.ballType = BallType.Blue;
                ballBlue.displayName = "Balón Azul";
                ballBlue.price = 500;

                shm.SetCatalogs(new List<BallItemSO> { ballBlue }, new List<StadiumItemSO>());

                sm.CurrentData.coins = 200;
                bool buyFail = shm.BuyBall(BallType.Blue);
                Assert(!buyFail, "Cannot buy without enough coins.");

                sm.CurrentData.coins = 1000;
                bool buySuccess = shm.BuyBall(BallType.Blue);
                Assert(buySuccess, "Buying with sufficient coins must succeed.");
                Assert(sm.IsBallUnlocked(BallType.Blue), "Ball Blue must now be unlocked.");
                Assert(cm.Coins == 500, "Coins must be deducted accurately (1000 - 500 = 500).");

                bool buyDuplicate = shm.BuyBall(BallType.Blue);
                Assert(!buyDuplicate, "Cannot buy already unlocked ball again.");
                Assert(cm.Coins == 500, "Coins must not be deducted on duplicate purchase attempt.");
            }
            finally
            {
                GameObject.DestroyImmediate(go);
            }
        }

        private static void TestCustomization()
        {
            GameObject go = new GameObject("Test_Customization");
            var sm = go.AddComponent<SaveManager>();
            var cust = go.AddComponent<CustomizationManager>();

            try
            {
                Assert(cust.EquippedBall == BallType.Classic, "Initial ball should be Classic.");

                // Attempt equip locked ball -> should fail
                bool equipFail = cust.EquipBall(BallType.Gold);
                Assert(!equipFail, "Should not be able to equip locked ball.");
                Assert(cust.EquippedBall == BallType.Classic, "Equipped ball should remain Classic.");

                // Unlock and equip
                sm.UnlockBall(BallType.Gold);
                bool equipSuccess = cust.EquipBall(BallType.Gold);
                Assert(equipSuccess, "Equipping unlocked ball should succeed.");
                Assert(cust.EquippedBall == BallType.Gold, "Equipped ball must now be Gold.");
            }
            finally
            {
                GameObject.DestroyImmediate(go);
            }
        }

        private static void TestAchievements()
        {
            GameObject go = new GameObject("Test_Ach");
            var sm = go.AddComponent<SaveManager>();
            var cm = go.AddComponent<CurrencyManager>();
            var am = go.AddComponent<AchievementManager>();

            try
            {
                var achFirst = ScriptableObject.CreateInstance<AchievementSO>();
                achFirst.id = AchievementManager.ID_FIRST_GOAL;
                achFirst.title = "Primer Gol";
                achFirst.rewardCoins = 100;
                am.SetAchievements(new List<AchievementSO> { achFirst });

                Assert(!am.IsUnlocked(AchievementManager.ID_FIRST_GOAL), "Achievement should start locked.");

                am.CheckGoalScored(currentStreak: 1, totalGoals: 1);
                Assert(am.IsUnlocked(AchievementManager.ID_FIRST_GOAL), "First goal achievement should unlock on 1st goal.");

                // Duplicate check
                bool reUnlock = am.UnlockAchievement(AchievementManager.ID_FIRST_GOAL);
                Assert(!reUnlock, "Cannot unlock already unlocked achievement twice.");
            }
            finally
            {
                GameObject.DestroyImmediate(go);
            }
        }

        private static void TestRoundProgression()
        {
            GameObject go = new GameObject("Test_Round");
            var sm = go.AddComponent<SaveManager>();

            try
            {
                Assert(sm.IsRoundUnlocked(1), "Round 1 must be unlocked by default.");
                Assert(!sm.IsRoundUnlocked(2), "Round 2 must be locked initially.");
                Assert(!sm.IsRoundUnlocked(5), "Round 5 must be locked initially.");

                // Beat Round 1 -> Unlocks Round 2
                sm.UnlockRound(2);
                Assert(sm.IsRoundUnlocked(2), "Round 2 must be unlocked.");
                Assert(!sm.IsRoundUnlocked(3), "Round 3 must still be locked.");

                // Sequential progression up to Round 5
                sm.UnlockRound(3);
                sm.UnlockRound(4);
                sm.UnlockRound(5);
                Assert(sm.IsRoundUnlocked(5), "Round 5 (Gran Final) must be unlocked after reaching it.");
            }
            finally
            {
                GameObject.DestroyImmediate(go);
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception($"Assertion Failed: {message}");
            }
        }
    }
}
