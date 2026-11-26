using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UltimatePenaltyCup.Core;
using UltimatePenaltyCup.Gameplay;
using UltimatePenaltyCup.Levels;
using UltimatePenaltyCup.Shop;
using UltimatePenaltyCup.SaveSystem;
using UltimatePenaltyCup.Economy;
using UltimatePenaltyCup.UI;

namespace UltimatePenaltyCup.Editor
{
    /// <summary>
    /// Captures authentic 1920x1080 GPU-rendered PNG screenshots of every UI screen
    /// and 3D gameplay scene into Docs/Screenshots/ for real visual inspection.
    /// Must run WITHOUT -nographics.
    /// </summary>
    public static class VisualValidationCapture
    {
        private const string SHOT_DIR = "Docs/Screenshots";

        [MenuItem("Ultimate Penalty Cup/Visual Validation/Capture All Screenshots", false, 150)]
        public static void CaptureAllScreenshots()
        {
            if (!Directory.Exists(SHOT_DIR)) Directory.CreateDirectory(SHOT_DIR);

            CaptureMainMenuScreens();
            CaptureMatchScreens();

            AssetDatabase.Refresh();
            Debug.Log("[VisualValidationCapture] Captured all 1920x1080 verification screenshots into Docs/Screenshots/.");
        }

        private static void CaptureMainMenuScreens()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);

            // Bind temporary in-memory SaveData so previews/badges/cards show realistic state without touching disk saves
            var saveMgr = Object.FindFirstObjectByType<SaveManager>();
            var currMgr = Object.FindFirstObjectByType<CurrencyManager>();
            if (saveMgr != null)
            {
                var sampleData = new SaveData { coins = 1250, highScore = 1150, totalGoalsScored = 4, bestGoalStreak = 2 };
                sampleData.unlockedBalls.Add(BallType.Blue);
                sampleData.unlockedAchievements.Add("FIRST_GOAL");
                saveMgr.SetTestDataInMemory(sampleData);
            }
            if (currMgr != null) currMgr.BindForTest();

            Camera cam = Camera.main;
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            MainMenuUI menu = Object.FindFirstObjectByType<MainMenuUI>();
            menu.UpdateCurrencyUI();

            var pnlMain   = GetField<GameObject>(menu, "panelMainMenu");
            var pnlRounds = GetField<GameObject>(menu, "panelRoundSelect");
            var pnlShop   = GetField<GameObject>(menu, "panelShop");
            var pnlCustom = GetField<GameObject>(menu, "panelCustomization");
            var pnlAch    = GetField<GameObject>(menu, "panelAchievements");
            var pnlSet    = GetField<GameObject>(menu, "panelSettings");

            var shopUI   = pnlShop.GetComponent<ShopUI>();
            var customUI = pnlCustom.GetComponent<CustomizationUI>();

            // Populate RoundSelect preview cards for accurate screenshot
            var roundSelectUI = pnlRounds.GetComponent<RoundSelectUI>();
            if (roundSelectUI != null)
            {
                var cards = GetField<RoundSelectUI.RoundCard[]>(roundSelectUI, "roundCards");
                var gm = Object.FindFirstObjectByType<GameManager>();
                if (cards != null && gm != null)
                {
                    for (int i = 0; i < cards.Length; i++)
                    {
                        var r = gm.GetRoundData(cards[i].roundNumber);
                        if (r != null)
                        {
                            if (cards[i].txtTitle != null) cards[i].txtTitle.text = $"RONDA {r.roundNumber}\n{r.roundName.ToUpper()}";
                            if (cards[i].txtDifficulty != null) cards[i].txtDifficulty.text = $"IA: {r.goalkeeperTypeTitle}";
                            if (cards[i].txtDescription != null) cards[i].txtDescription.text = r.description;
                            if (cards[i].txtTarget != null) cards[i].txtTarget.text = $"Meta: {r.requiredGoals} / {r.maxAttempts} goles";
                            if (cards[i].txtRewards != null) cards[i].txtRewards.text = $"+{r.winCoins:N0} Monedas";
                        }
                        bool unlocked = cards[i].roundNumber == 1;
                        if (cards[i].lockOverlay != null) cards[i].lockOverlay.SetActive(!unlocked);
                        if (cards[i].btnPlay != null) cards[i].btnPlay.interactable = unlocked;
                    }
                }
            }

            void ShowOnly(GameObject target)
            {
                pnlMain.SetActive(pnlMain == target);
                pnlRounds.SetActive(pnlRounds == target);
                pnlShop.SetActive(pnlShop == target);
                pnlCustom.SetActive(pnlCustom == target);
                pnlAch.SetActive(pnlAch == target);
                pnlSet.SetActive(pnlSet == target);
            }

            ShowOnly(pnlMain);
            RenderWithCanvas(cam, canvas, "01_MainMenu.png");

            ShowOnly(pnlRounds);
            RenderWithCanvas(cam, canvas, "02_RoundSelect.png");

            ShowOnly(pnlShop);
            shopUI.SwitchTab(true, silent: true);
            shopUI.RefreshAllCards();
            RenderWithCanvas(cam, canvas, "03_Shop_Balls.png");

            shopUI.SwitchTab(false, silent: true);
            shopUI.RefreshAllCards();
            RenderWithCanvas(cam, canvas, "04_Shop_Stadiums.png");

            ShowOnly(pnlCustom);
            customUI.SwitchTab(true);
            customUI.RefreshAll();
            RenderWithCanvas(cam, canvas, "05_Customization.png");

            customUI.SwitchTab(false);
            customUI.RefreshAll();
            RenderWithCanvas(cam, canvas, "05b_Customization_Stadiums.png");

            ShowOnly(pnlAch);
            var achUI = pnlAch.GetComponent<AchievementsUI>();
            if (achUI != null)
            {
                var cards = GetField<AchievementsUI.AchievementCard[]>(achUI, "achievementCards");
                string[] sampleProg = { "1 / 1", "4 / 10 goles", "Mejor racha: 2 / 4", "Ronda 2 / 5", "1,250 / 10,000", "2 / 5 balones", "0 / 1,500" };
                float[] sampleFill = { 1f, 0.4f, 0.5f, 0.25f, 0.125f, 0.4f, 0f };
                if (cards != null)
                {
                    for (int i = 0; i < cards.Length && i < sampleProg.Length; i++)
                    {
                        if (cards[i].txtProgress != null) cards[i].txtProgress.text = sampleProg[i];
                        if (cards[i].imgProgressFill != null) cards[i].imgProgressFill.fillAmount = sampleFill[i];
                        bool u = i == 0;
                        if (cards[i].unlockedBadge != null) cards[i].unlockedBadge.SetActive(u);
                        if (cards[i].lockedBadge != null) cards[i].lockedBadge.SetActive(!u);
                    }
                }
            }
            RenderWithCanvas(cam, canvas, "06_Achievements.png");

            ShowOnly(pnlSet);
            RenderWithCanvas(cam, canvas, "07_Settings.png");
        }

        private static void CaptureMatchScreens()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MatchScene.unity", OpenSceneMode.Single);

            Camera cam = Camera.main;
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            StadiumController sc = Object.FindAnyObjectByType<StadiumController>();
            if (sc != null) sc.ApplyVenue(StadiumType.Professional);

            // Position reticle on goal & directional arrow from ball toward goal for clean gameplay capture
            GameObject reticleObj = GameObject.Find("AimReticle_Indicator");
            GameObject arrowObj = GameObject.Find("DirectionArrowRoot");
            GameObject trajObj = GameObject.Find("TrajectoryGuideline");
            if (reticleObj != null) reticleObj.transform.position = new Vector3(0f, 1.15f, -0.04f);
            if (arrowObj != null)
            {
                arrowObj.transform.position = new Vector3(0f, 0.135f, -11f);
                arrowObj.transform.LookAt(new Vector3(0f, 1.15f, 0f));
            }
            if (trajObj != null) trajObj.SetActive(false);

            // 08: Broadcast Penalty Camera Gameplay View + HUD
            var hud = canvas.GetComponent<MatchHUD>();
            var pwr = GetField<GameObject>(hud, "powerBarContainer");
            var banner = GetField<GameObject>(hud, "bannerResult");
            if (pwr != null) pwr.SetActive(true);
            if (banner != null) banner.SetActive(false);
            RenderWithCanvas(cam, canvas, "08_Match_Gameplay.png");
            if (pwr != null) pwr.SetActive(false);

            // 08b..08f: 360-degree anatomical close-up verification of Skinned Player & Goalkeeper
            canvas.gameObject.SetActive(false);
            if (reticleObj != null) reticleObj.SetActive(false);
            if (arrowObj != null) arrowObj.SetActive(false);
            Vector3 oldPos = cam.transform.position;
            Quaternion oldRot = cam.transform.rotation;

            Vector3 rubenCenter = new Vector3(-0.68f, 0.95f, -13.10f);
            cam.transform.position = rubenCenter + new Vector3(0.55f, 0.10f, 2.75f);
            cam.transform.LookAt(rubenCenter);
            RenderCameraOnly(cam, "08b_Ruben_Front.png");

            cam.transform.position = rubenCenter + new Vector3(-0.20f, 0.15f, -2.70f);
            cam.transform.LookAt(rubenCenter);
            RenderCameraOnly(cam, "08c_Ruben_Back.png");

            cam.transform.position = rubenCenter + new Vector3(-2.65f, 0.08f, 0.20f);
            cam.transform.LookAt(rubenCenter);
            RenderCameraOnly(cam, "08d_Ruben_Left.png");

            cam.transform.position = rubenCenter + new Vector3(2.65f, 0.08f, 0.20f);
            cam.transform.LookAt(rubenCenter);
            RenderCameraOnly(cam, "08e_Ruben_Right.png");

            cam.transform.position = new Vector3(1.35f, 1.20f, -3.4f);
            cam.transform.LookAt(new Vector3(0f, 0.98f, -0.45f));
            RenderCameraOnly(cam, "08f_Malagon_CloseUp.png");

            // Capture all 4 Stadium Venues cleanly
            cam.transform.position = new Vector3(0.58f, 1.88f, -16.85f);
            cam.transform.LookAt(new Vector3(-0.08f, 1.15f, 0f));
            if (sc != null)
            {
                sc.ApplyVenue(StadiumType.Cantera); RenderCameraOnly(cam, "13_Stadium_Cantera.png");
                sc.ApplyVenue(StadiumType.Night); RenderCameraOnly(cam, "14_Stadium_Night.png");
                sc.ApplyVenue(StadiumType.Professional); RenderCameraOnly(cam, "15_Stadium_Professional.png");
                sc.ApplyVenue(StadiumType.WorldCup); RenderCameraOnly(cam, "16_Stadium_WorldCup.png");
                sc.ApplyVenue(StadiumType.Professional);
            }

            cam.transform.position = oldPos;
            cam.transform.rotation = oldRot;
            canvas.gameObject.SetActive(true);

            // 09: Pause Modal
            var pauseUI = Object.FindFirstObjectByType<PauseUI>(FindObjectsInactive.Include);
            var pnlPause = GetField<GameObject>(pauseUI, "panelPause");
            pnlPause.SetActive(true);
            RenderWithCanvas(cam, canvas, "09_Match_Pause.png");
            pnlPause.SetActive(false);

            // 10, 11, 12: Victory, Defeat, Grand Final
            var endUI = Object.FindFirstObjectByType<RoundEndUI>(FindObjectsInactive.Include);
            endUI.ShowRoundEndDetailed(new RoundEndSummary
            {
                isVictory = true, isGrandFinal = false, roundNumber = 2, roundName = "Liga Local",
                goalsScored = 3, requiredGoals = 3, attemptsUsed = 4, maxAttempts = 5,
                matchScore = 1150, matchCoins = 925, victoryBonusCoins = 450, victoryBonusPoints = 500, isNewRecord = true
            });
            RenderWithCanvas(cam, canvas, "10_Match_Victory.png");

            endUI.ShowRoundEndDetailed(new RoundEndSummary
            {
                isVictory = false, isGrandFinal = false, roundNumber = 3, roundName = "Liga Profesional",
                goalsScored = 2, requiredGoals = 4, attemptsUsed = 4, maxAttempts = 5,
                matchScore = 300, matchCoins = 225, victoryBonusCoins = 0, victoryBonusPoints = 0, isNewRecord = false
            });
            RenderWithCanvas(cam, canvas, "11_Match_Defeat.png");

            if (sc != null) sc.ApplyVenue(StadiumType.WorldCup);
            endUI.ShowRoundEndDetailed(new RoundEndSummary
            {
                isVictory = true, isGrandFinal = true, roundNumber = 5, roundName = "Gran Final",
                goalsScored = 4, requiredGoals = 4, attemptsUsed = 4, maxAttempts = 5,
                matchScore = 2450, matchCoins = 2350, victoryBonusCoins = 1500, victoryBonusPoints = 1200, isNewRecord = true,
                unlockedAchievements = new[] { "Campeón", "Leyenda", "Racha Perfecta" }
            });
            RenderWithCanvas(cam, canvas, "12_Match_GrandFinal.png");
        }

        private static void RenderWithCanvas(Camera cam, Canvas canvas, string fileName)
        {
            RenderMode prevMode = canvas.renderMode;
            Camera prevCam = canvas.worldCamera;
            float prevDist = canvas.planeDistance;

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1.0f;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(canvas.GetComponent<RectTransform>());
            Canvas.ForceUpdateCanvases();

            RenderCameraOnly(cam, fileName);

            canvas.renderMode = prevMode;
            canvas.worldCamera = prevCam;
            canvas.planeDistance = prevDist;
        }

        private static void RenderCameraOnly(Camera cam, string fileName)
        {
            const int width = 1920;
            const int height = 1080;
            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render(); // Warm-up frame for URP materials
            cam.Render();

            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);

            string path = Path.Combine(SHOT_DIR, fileName).Replace('\\', '/');
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static T GetField<T>(object target, string fieldName) where T : class
        {
            if (target == null) return null;
            var f = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            return f != null ? f.GetValue(target) as T : null;
        }
    }
}
